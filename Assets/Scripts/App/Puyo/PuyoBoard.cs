using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>NEXTぷよキューの色情報。</summary>
    public struct PuyoPairColors
    {
        public PuyoColor Main;
        public PuyoColor Sub;
    }

    /// <summary>
    /// ぷよぷよのゲームボードを管理するクラス。
    /// 6×12（表示）のグリッドデータ・ぷよの配置・スポーン・連鎖チェックを担当する。
    /// </summary>
    public sealed class PuyoBoard : MonoBehaviour
    {
        // ─── 定数 ───────────────────────────────────────────────
        public const int COLS              = 6;  // 横列数
        public const int ROWS              = 14; // 内部縦行数（上2段は非表示バッファ）
        public const int VISIBLE_ROWS      = 12; // 表示縦行数（本家準拠）
        public const int MaxOjamaPerTurn   = 30; // 1ターンに落下できるお邪魔ぷよの最大数（本家準拠）

        // ─── Inspector設定 ──────────────────────────────────────
        [Header("グリッド設定")]
        [SerializeField] private float _cellSize = 1f;

        [Header("Prefab参照")]
        [SerializeField] private PuyoPair  _pairPrefab;
        [SerializeField] private PuyoPiece _piecePrefab;

        /// <summary>
        /// 色スプライト配列。PuyoColor の値をインデックスとして使う。
        /// [0]=RED [1]=BLUE [2]=GREEN [3]=YELLOW [4]=PURPLE [5]=OJAMA
        /// </summary>
        [Header("スプライト (PuyoColor順に設定)")]
        [SerializeField] private Sprite[] _colorSprites;

        // ─── イベント（Observer） ────────────────────────────────
        /// <summary>
        /// 連鎖が完了したときに発火する（連鎖数を渡す）。
        /// パチンコ玉の発射数など外部ロジックをここで受け取る。
        /// </summary>
        /// <summary>第1引数：連鎖数、第2引数：消えたぷよ合計数</summary>
        public event Action<int, int>                        OnChainCompleted;
        public event Action<IReadOnlyList<PuyoPairColors>>? OnNextQueueChanged;

        /// <summary>ペアが着地してグリッドに固定されたとき発火する。</summary>
        public event Action? OnPairLocked;

        /// <summary>スポーン位置が埋まってゲームオーバーになったとき発火する。</summary>
        public event Action? OnGameOver;

        // ─── 内部状態 ────────────────────────────────────────────
        private PuyoPiece[,]          _grid;       // グリッドデータ本体
        private PuyoPair              _activePair; // 現在落下中のペア
        private Queue<PuyoPairColors> _nextQueue = new();
        private int                   _pendingOjama; // 次ターン開始前に降らせるお邪魔ぷよの予約数

        /// <summary>現在落下中のペア（GameMainController の入力転送に使う）</summary>
        public PuyoPair ActivePair => _activePair;

        /// <summary>NEXTキューの内容（[0]=NEXT, [1]=NEXT NEXT）</summary>
        public IReadOnlyList<PuyoPairColors> NextPairs => _nextQueue.ToList();

        /// <summary>色スプライト配列（NextPuyoDisplay の初期化に使う）</summary>
        public Sprite[] ColorSprites => _colorSprites;

        private int _colorCount; // 使用する色数（ステージで変わる）
        private CancellationTokenSource _cts;

        // ─── 初期化 ──────────────────────────────────────────────

        /// <summary>
        /// ゲーム開始時に呼ぶ。ステージ数に応じて使用色数を指定する。
        /// 例：ステージ1→4色、ステージ後半→5色（紫追加）
        /// </summary>
        public void Initialize(int colorCount = 4)
        {
            _colorCount   = Mathf.Clamp(colorCount, 2, (int)PuyoColor.OJAMA);
            _grid         = new PuyoPiece[COLS, ROWS];
            _pendingOjama = 0;
            _cts          = CancellationTokenSource.CreateLinkedTokenSource(
                              destroyCancellationToken);

            // Inspector 未アサイン時のフォールバック
            if (_pairPrefab  == null) _pairPrefab  = Resources.Load<PuyoPair> ("Prefabs/GameMain/Puyo/PuyoPair");
            if (_piecePrefab == null) _piecePrefab = Resources.Load<PuyoPiece>("Prefabs/GameMain/Puyo/PuyoPiece");

            // キューを3つ分初期化（落下中1 + NEXT + NEXT NEXT）
            _nextQueue.Clear();
            _nextQueue.Enqueue(RandomPairColors());
            _nextQueue.Enqueue(RandomPairColors());
            _nextQueue.Enqueue(RandomPairColors());

            SpawnNextPair();
        }

        private void OnDestroy() => _cts?.Cancel();

        /// <summary>
        /// シミュレーターモード用：ぷよの落下をすべて停止する。
        /// 再開はシーンリロード（GameMainController.RestartGame）で行う。
        /// </summary>
        public void Suspend()
        {
            _cts?.Cancel();
            if (_activePair != null)
            {
                Destroy(_activePair.gameObject);
                _activePair = null;
            }
        }

        // ─── 座標変換 ────────────────────────────────────────────

        public float CellSize => _cellSize;

        /// <summary>グリッド座標をワールド座標に変換する</summary>
        public Vector3 CellToWorld(Vector2Int cell) =>
            transform.position + new Vector3(cell.x * _cellSize, cell.y * _cellSize);

        // ─── グリッド操作 ────────────────────────────────────────

        /// <summary>
        /// 指定セルが空きかどうかを返す。
        /// 盤外（左右・下）は false、上空（ROWS以上）は true を返す。
        /// </summary>
        public bool IsEmpty(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0) return false;
            if (cell.y >= ROWS) return true; // スポーン時に上空を許容
            return _grid[cell.x, cell.y] == null;
        }

        // ─── ペアロック ──────────────────────────────────────────

        /// <summary>
        /// ペアが着地したときに PuyoPair から呼ばれる。
        /// ぷよをグリッドに配置し、連鎖チェックを開始する。
        /// </summary>
        public void LockPair(PuyoPiece main, PuyoPiece sub)
        {
            Place(main);
            Place(sub);
            _activePair = null;

            OnPairLocked?.Invoke();

            // 連鎖チェックを非同期で実行（完了後に次のペアをスポーン）
            ProcessChainAsync(_cts.Token).Forget();
        }

        /// <summary>ぷよをグリッドに書き込み、位置をセルに合わせる</summary>
        private void Place(PuyoPiece piece)
        {
            var cell = piece.Cell;
            if (cell.y < ROWS)
                _grid[cell.x, cell.y] = piece;

            piece.transform.position = CellToWorld(cell);
        }

        // ─── 連鎖処理 ────────────────────────────────────────────

        // BFS 探索用の4方向オフセット
        private static readonly Vector2Int[] _dirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        /// <summary>
        /// 連鎖チェック → 消去 → 落下 を繰り返す非同期処理。
        /// 連鎖がなくなったら次のペアをスポーンする。
        /// </summary>
        private async UniTaskVoid ProcessChainAsync(CancellationToken ct)
        {
            var chainCount   = 0;
            var clearedCount = 0;

            while (TryFindChain(out var targets))
            {
                chainCount++;
                clearedCount += targets.Count;
                await ClearPuyosAsync(targets, ct);
                await DropFloatingPuyosAsync(ct);
            }

            // 連鎖が1回以上あればイベント発火（パチンコ玉発射トリガー）
            if (chainCount > 0)
                OnChainCompleted?.Invoke(chainCount, clearedCount);

            // 積まれたお邪魔ぷよを次のペアスポーン前に降らせる（最大30個/ターン、超過分は持ち越し）
            if (_pendingOjama > 0)
            {
                var count     = Mathf.Min(_pendingOjama, MaxOjamaPerTurn);
                _pendingOjama -= count;
                await DropOjamaAsync(count, ct);
            }

            SpawnNextPair();
        }

        /// <summary>
        /// BFS で同色4つ以上の連結グループを探す。
        /// 見つかった消去対象ぷよを targets に格納して true を返す。
        /// </summary>
        private bool TryFindChain(out List<PuyoPiece> targets)
        {
            targets = new List<PuyoPiece>();
            var visited = new bool[COLS, ROWS];

            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < ROWS; y++)
            {
                var piece = _grid[x, y];
                if (piece == null || visited[x, y] || piece.Color == PuyoColor.OJAMA) continue;

                // BFS で同色の連結グループを収集
                var group = new List<PuyoPiece>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(x, y));
                visited[x, y] = true;

                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    group.Add(_grid[cell.x, cell.y]);

                    foreach (var dir in _dirs)
                    {
                        var next = cell + dir;
                        if (next.x < 0 || next.x >= COLS || next.y < 0 || next.y >= ROWS) continue;
                        if (visited[next.x, next.y]) continue;
                        var neighbor = _grid[next.x, next.y];
                        if (neighbor == null || neighbor.Color != piece.Color) continue;

                        visited[next.x, next.y] = true;
                        queue.Enqueue(next);
                    }
                }

                if (group.Count >= 4)
                {
                    targets.AddRange(group);

                    // グループに隣接するお邪魔ぷよも消去対象に加える
                    foreach (var member in group)
                    foreach (var dir in _dirs)
                    {
                        var next = member.Cell + dir;
                        if (next.x < 0 || next.x >= COLS || next.y < 0 || next.y >= ROWS) continue;
                        var neighbor = _grid[next.x, next.y];
                        if (neighbor == null || neighbor.Color != PuyoColor.OJAMA) continue;
                        if (targets.Contains(neighbor)) continue; // 重複防止
                        targets.Add(neighbor);
                    }
                }
            }

            return targets.Count > 0;
        }

        /// <summary>
        /// グリッドから除去し、消去アニメーションを並列実行する。
        /// </summary>
        private async UniTask ClearPuyosAsync(List<PuyoPiece> targets, CancellationToken ct)
        {
            foreach (var piece in targets)
                _grid[piece.Cell.x, piece.Cell.y] = null;

            await UniTask.WhenAll(targets.Select(p => ((IClearable)p).ClearAsync(ct)));
        }

        /// <summary>
        /// 消去後に浮いたぷよを下に詰め、DOTween で落下アニメーションを再生する。
        /// </summary>
        private async UniTask DropFloatingPuyosAsync(CancellationToken ct)
        {
            const float dropDuration = 0.12f;
            var moved = false;

            for (var x = 0; x < COLS; x++)
            {
                var writeY = 0;
                for (var readY = 0; readY < ROWS; readY++)
                {
                    if (_grid[x, readY] == null) continue;
                    if (readY != writeY)
                    {
                        _grid[x, writeY]      = _grid[x, readY];
                        _grid[x, readY]       = null;
                        _grid[x, writeY].Cell = new Vector2Int(x, writeY);

                        _grid[x, writeY].transform
                            .DOMove(CellToWorld(new Vector2Int(x, writeY)), dropDuration)
                            .SetEase(Ease.InQuad);
                        moved = true;
                    }
                    writeY++;
                }
            }

            if (moved)
                await UniTask.Delay(Mathf.RoundToInt(dropDuration * 1000) + 20, cancellationToken: ct);
        }

        // ─── お邪魔ぷよ ─────────────────────────────────────────

        /// <summary>
        /// 次ターン開始前に降らせるお邪魔ぷよを予約する。
        /// 実際の落下は ProcessChainAsync 末尾（次ペアスポーン直前）で行われる。
        /// </summary>
        public void AddPendingOjama(int count) => _pendingOjama += count;

        /// <summary>列の最上端（積み高さ）を返す。空列なら 0。</summary>
        public int GetColumnTop(int col)
        {
            for (var y = ROWS - 1; y >= 0; y--)
                if (_grid[col, y] != null) return y + 1;
            return 0;
        }

        /// <summary>
        /// お邪魔ぷよを count 個、左列から右列へ均等に配置して落下アニメーションを再生する。
        /// 各列を並列で実行し、列間に 40ms のウェーブ効果をかける。
        /// </summary>
        private async UniTask DropOjamaAsync(int count, CancellationToken ct)
        {
            // ランダムな列順で均等配置（ウェーブ効果もランダム順に追従）
            var shuffled = Enumerable.Range(0, COLS).OrderBy(_ => UnityEngine.Random.value).ToArray();
            var drops    = new int[COLS];
            for (var i = 0; i < count; i++)
                drops[shuffled[i % COLS]]++;

            var tasks = shuffled
                .Select((col, order) => drops[col] > 0
                    ? DropOjamaColumnAsync(col, drops[col], order * 40, ct)
                    : UniTask.CompletedTask);

            await UniTask.WhenAll(tasks);
        }

        /// <summary>1列分のお邪魔ぷよを上から順に落下させる。</summary>
        private async UniTask DropOjamaColumnAsync(int col, int count, int staggerMs, CancellationToken ct)
        {
            if (staggerMs > 0)
                await UniTask.Delay(staggerMs, cancellationToken: ct);

            const float fallDuration = 0.2f;
            for (var i = 0; i < count; i++)
            {
                var topY = GetColumnTop(col);
                if (topY >= ROWS) break; // 列が満杯ならそれ以上積まない

                var cell  = new Vector2Int(col, topY);
                var piece = Instantiate(_piecePrefab, transform);
                piece.Setup(PuyoColor.OJAMA, _colorSprites[(int)PuyoColor.OJAMA]);
                piece.Cell             = cell;
                _grid[cell.x, cell.y] = piece;

                // ボード上端の1マス外から落下開始
                piece.transform.position = CellToWorld(new Vector2Int(col, ROWS + 1));
                piece.transform.DOMove(CellToWorld(cell), fallDuration).SetEase(Ease.InQuad);

                await UniTask.Delay(Mathf.RoundToInt(fallDuration * 1000) + 30, cancellationToken: ct);
            }
        }

        // ─── スキル用ヘルパー ─────────────────────────────────────

        /// <summary>最もぷよが多い行のインデックスを返す。盤面が空なら -1。</summary>
        public int FindDensestRow()
        {
            var best = -1;
            var bestCount = 0;
            for (var y = 0; y < ROWS; y++)
            {
                var count = 0;
                for (var x = 0; x < COLS; x++)
                    if (_grid[x, y] != null) count++;
                if (count > bestCount) { bestCount = count; best = y; }
            }
            return best;
        }

        /// <summary>最もぷよが多い列のインデックスを返す。盤面が空なら -1。</summary>
        public int FindDensestColumn()
        {
            var best = -1;
            var bestCount = 0;
            for (var x = 0; x < COLS; x++)
            {
                var count = GetColumnTop(x);
                if (count > bestCount) { bestCount = count; best = x; }
            }
            return best;
        }

        /// <summary>
        /// ぷよが最も密集している 2×2 エリアの左下座標を返す。
        /// 同数の場合は最初に見つかったエリアを返す。盤面が空なら (-1,-1)。
        /// </summary>
        public Vector2Int FindDensest2x2()
        {
            var best = new Vector2Int(-1, -1);
            var bestCount = 0;
            for (var x = 0; x < COLS - 1; x++)
            for (var y = 0; y < ROWS - 1; y++)
            {
                var count = 0;
                if (_grid[x,     y    ] != null) count++;
                if (_grid[x + 1, y    ] != null) count++;
                if (_grid[x,     y + 1] != null) count++;
                if (_grid[x + 1, y + 1] != null) count++;
                if (count > bestCount) { bestCount = count; best = new Vector2Int(x, y); }
            }
            return best;
        }

        /// <summary>ぷよが存在する最も高い行のインデックスを返す。盤面が空なら -1。</summary>
        public int GetHighestRow()
        {
            for (var y = ROWS - 1; y >= 0; y--)
                for (var x = 0; x < COLS; x++)
                    if (_grid[x, y] != null) return y;
            return -1;
        }

        /// <summary>お邪魔ぷよの全座標リストを返す。</summary>
        public List<Vector2Int> GetOjamaPositions()
        {
            var positions = new List<Vector2Int>();
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < ROWS; y++)
                if (_grid[x, y] != null && _grid[x, y].Color == PuyoColor.OJAMA)
                    positions.Add(new Vector2Int(x, y));
            return positions;
        }

        /// <summary>指定行にある非 null セルの数を返す（スキルダメージ計算用）。</summary>
        public int GetNonNullCountInRow(int row)
        {
            var count = 0;
            for (var x = 0; x < COLS; x++)
                if (_grid[x, row] != null) count++;
            return count;
        }

        /// <summary>指定列にある非 null セルの数を返す（スキルダメージ計算用）。</summary>
        public int GetNonNullCountInCol(int col)
        {
            var count = 0;
            for (var y = 0; y < ROWS; y++)
                if (_grid[col, y] != null) count++;
            return count;
        }

        /// <summary>指定座標リストの中で非 null セルの数を返す（スキルダメージ計算用）。</summary>
        public int GetNonNullCountInCells(Vector2Int[] cells)
        {
            var count = 0;
            foreach (var c in cells)
                if (c.x >= 0 && c.x < COLS && c.y >= 0 && c.y < ROWS && _grid[c.x, c.y] != null)
                    count++;
            return count;
        }

        /// <summary>非お邪魔ぷよの座標をランダムに最大 count 個返す。</summary>
        public List<Vector2Int> GetRandomPuyoPositions(int count)
        {
            var all = new List<Vector2Int>();
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < ROWS; y++)
                if (_grid[x, y] != null && _grid[x, y].Color != PuyoColor.OJAMA)
                    all.Add(new Vector2Int(x, y));
            return all.OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();
        }

        /// <summary>
        /// スキルによる1セル消去。通常連鎖と異なり、吹き飛ぶ演出で消える。
        /// </summary>
        public async UniTask ClearCellBySkillAsync(Vector2Int cell, CancellationToken ct)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0 || cell.y >= ROWS) return;
            var piece = _grid[cell.x, cell.y];
            if (piece == null) return;
            _grid[cell.x, cell.y] = null;

            const float duration = 0.12f;
            var flyDir = new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                UnityEngine.Random.Range(1f, 3f),
                0f
            );
            piece.transform.DOMove(piece.transform.position + flyDir, duration).SetEase(Ease.OutQuad);
            piece.transform.DOScale(0f, duration).SetEase(Ease.InQuad);
            await UniTask.Delay(Mathf.RoundToInt(duration * 1000), cancellationToken: ct);
            Destroy(piece.gameObject);
        }

        /// <summary>盤面の全ぷよを消す（ストックMAX全消し用）。通常の消去アニメーションを使う。</summary>
        public async UniTask ClearAllPuyosAsync(CancellationToken ct)
        {
            var pieces = new List<PuyoPiece>();
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < ROWS; y++)
            {
                if (_grid[x, y] == null) continue;
                pieces.Add(_grid[x, y]);
                _grid[x, y] = null;
            }
            await UniTask.WhenAll(pieces.Select(p => ((IClearable)p).ClearAsync(ct)));
        }

        /// <summary>スキル消去後に浮いたぷよを落下させる。</summary>
        public UniTask DropFloatingAfterSkillAsync(CancellationToken ct) => DropFloatingPuyosAsync(ct);

        // ─── スポーン ────────────────────────────────────────────

        /// <summary>
        /// 次の落下ペアをスポーンする。
        /// スポーン位置：上から2行目・中央左よりの列。
        /// </summary>
        private void SpawnNextPair()
        {
            var spawnCell = new Vector2Int(COLS / 2 - 1, ROWS - 1);

            // スポーン位置が塞がれていればゲームオーバー
            if (!IsEmpty(spawnCell))
            {
                OnGameOver?.Invoke();
                return;
            }

            var colors = _nextQueue.Dequeue();
            _nextQueue.Enqueue(RandomPairColors()); // 末尾補充して常に3つ維持
            var pair = Instantiate(_pairPrefab, transform);
            pair.Initialize(
                board:       this,
                piecePrefab: _piecePrefab,
                spawnCell:   spawnCell,
                mainColor:   colors.Main,
                subColor:    colors.Sub,
                sprites:     _colorSprites
            );
            _activePair = pair;

            // NEXT・NEXT NEXT（キュー先頭2つ）を通知
            OnNextQueueChanged?.Invoke(NextPairs);
        }

        /// <summary>ゲーム中に色数を切り替える（デバッグ用）。次のスポーンから反映される。</summary>
        public void SetColorCount(int count)
            => _colorCount = Mathf.Clamp(count, 2, (int)PuyoColor.OJAMA);

        /// <summary>ステージの色数に応じてランダムな色を返す（OJAMAは対象外）</summary>
        private PuyoColor RandomColor() => (PuyoColor)UnityEngine.Random.Range(0, _colorCount);

        private PuyoPairColors RandomPairColors() => new PuyoPairColors { Main = RandomColor(), Sub = RandomColor() };

#if UNITY_EDITOR
        /// <summary>デバッグ用：各列の積み上げ高さを返す（AutoPlayAgent が使用）。</summary>
        public int[] Debug_GetColumnHeights()
        {
            var heights = new int[COLS];
            for (var x = 0; x < COLS; x++)
                for (var y = ROWS - 1; y >= 0; y--)
                    if (_grid[x, y] != null) { heights[x] = y + 1; break; }
            return heights;
        }

        /// <summary>デバッグ用：盤面の下 heightRows 行をランダムなぷよで埋める。</summary>
        public void Debug_FillBoard(int heightRows = 6)
        {
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < heightRows; y++)
            {
                if (_grid[x, y] != null) continue;
                var cell  = new Vector2Int(x, y);
                var piece = Instantiate(_piecePrefab, transform);
                var color = (PuyoColor)UnityEngine.Random.Range(0, _colorCount);
                piece.Setup(color, _colorSprites[(int)color]);
                piece.Cell                 = cell;
                _grid[cell.x, cell.y]      = piece;
                piece.transform.position   = CellToWorld(cell);
            }
        }

#endif
    }
}
