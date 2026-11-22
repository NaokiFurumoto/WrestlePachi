using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameSys;
using UnityEngine;
using App;

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
    public sealed class PuyoBoard : MonoBehaviour, ISuspendable
    {
        // ─── 定数 ───────────────────────────────────────────────
        public const int COLS              = 6;  // 横列数
        public const int VISIBLE_ROWS      = 12; // デフォルト表示縦行数（エディタツール用）
        public const int MaxOjamaPerTurn   = 30; // 1ターンに落下できるお邪魔ぷよの最大数（本家準拠）

        // ─── Inspector設定 ──────────────────────────────────────
        [Header("グリッド設定")]
        [SerializeField] private float _cellSize = 1f;

        [Header("背景")]
        [SerializeField] private GameObject? _bg12;
        [SerializeField] private GameObject? _bg10;

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
        public event Action<int, int>?                       OnChainCompleted;
        public event Action<IReadOnlyList<PuyoPairColors>>? OnNextQueueChanged;

        /// <summary>ペアが着地してグリッドに固定されたとき発火する。</summary>
        public event Action? OnPairLocked;

        /// <summary>スポーン位置が埋まってゲームオーバーになったとき発火する。</summary>
        public event Action? OnGameOver;

        /// <summary>ぷよが消える直前に発火する（消去対象の座標リスト）。</summary>
        public event Action<IReadOnlyList<Vector2Int>>? OnAboutToClear;

        /// <summary>1連鎖ステップ完了後に発火する（何連鎖目か・このステップで消えた数）。</summary>
        public event Action<int, int>? OnChainStep;

        // ─── 内部状態 ────────────────────────────────────────────
        private int                   _rows        = 14; // 内部縦行数（表示行 + 上2段バッファ）
        private int                   _visibleRows = 12; // 表示縦行数

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

        private int   _colorCount;          // 使用する色数（ステージで変わる）
        private float _fallSpeedMultiplier = 1f; // 落下速度倍率（ステージで変わる）
        private CancellationTokenSource _cts;

        // 敵スキル「炎」用：炎マスに指定されたセル集合と、その背面表示マーカー
        private readonly HashSet<Vector2Int> _fireTiles = new();
        private readonly Dictionary<Vector2Int, GameObject> _fireTileMarkers = new();

        /// <summary>炎マスのマーカーをぷよの描画順からいくつ奥に置くか（盤面背景20 ＜ マーカー30 ＜ 発光45 ＜ ぷよ50）</summary>
        private const int FireTileMarkerOrderOffset = 20;

        // ─── 初期化 ──────────────────────────────────────────────

        /// <summary>
        /// ゲーム開始時に呼ぶ。
        /// </summary>
        public void Initialize(int colorCount = 4, int garbageInitialRows = 0, int visibleRows = 12)
        {
            _visibleRows  = Mathf.Clamp(visibleRows, 10, 12);
            _rows         = _visibleRows + 2;
            _colorCount   = Mathf.Clamp(colorCount, 2, (int)PuyoColor.OJAMA);
            _grid         = new PuyoPiece[COLS, _rows];
            _pendingOjama = 0;
            _cts          = CancellationTokenSource.CreateLinkedTokenSource(
                              destroyCancellationToken);

            // 背景切り替え
            if (_bg12 != null) _bg12.SetActive(_visibleRows == 12);
            if (_bg10 != null) _bg10.SetActive(_visibleRows == 10);

            // Inspector 未アサイン時のフォールバック
            if (_pairPrefab  == null) _pairPrefab  = Resources.Load<PuyoPair> ("Prefabs/GameMain/Puyo/PuyoPair");
            if (_piecePrefab == null) _piecePrefab = Resources.Load<PuyoPiece>("Prefabs/GameMain/Puyo/PuyoPiece");

            if (garbageInitialRows > 0)
                PlaceInitialGarbage(garbageInitialRows);

            // キューを3つ分初期化（落下中1 + NEXT + NEXT NEXT）
            _nextQueue.Clear();
            _nextQueue.Enqueue(RandomPairColors());
            _nextQueue.Enqueue(RandomPairColors());
            _nextQueue.Enqueue(RandomPairColors());

            SpawnNextPair();
        }

        /// <summary>ゲーム開始時に盤面の下から rows 行をお邪魔ぷよで即時配置する。</summary>
        private void PlaceInitialGarbage(int rows)
        {
            var clampedRows = Mathf.Clamp(rows, 0, _visibleRows - 2); // 上2行は常に空ける
            for (var y = 0; y < clampedRows; y++)
            for (var x = 0; x < COLS; x++)
            {
                var cell  = new Vector2Int(x, y);
                var piece = Instantiate(_piecePrefab, transform);
                piece.Setup(PuyoColor.OJAMA, _colorSprites[(int)PuyoColor.OJAMA]);
                piece.Cell                = cell;
                _grid[cell.x, cell.y]    = piece;
                piece.transform.position = CellToWorld(cell);
            }
        }

        private void OnDestroy() => _cts?.Cancel();

        /// <summary>落下ループを停止し、操作中のペアを即時破棄する。</summary>
        public void Suspend()
        {
            _cts?.Cancel();
            if (_activePair != null)
            {
                Destroy(_activePair.gameObject);
                _activePair = null;
            }
        }

        /// <summary>グリッド上の全ぷよを即時破棄する。</summary>
        public void ClearAll()
        {
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < _rows; y++)
            {
                if (_grid[x, y] == null) continue;
                Destroy(_grid[x, y].gameObject);
                _grid[x, y] = null;
            }
        }

        // ─── 座標変換 ────────────────────────────────────────────

        public float CellSize => _cellSize;

        /// <summary>グリッド座標をワールド座標に変換する</summary>
        public Vector3 CellToWorld(Vector2Int cell) =>
            transform.position + new Vector3(cell.x * _cellSize, cell.y * _cellSize);

        /// <summary>直前の連鎖で消えたぷよ群の重心（ワールド座標）。COMBO 表示位置に使う。</summary>
        public Vector3 LastChainCentroidWorld { get; private set; }

        // ─── グリッド操作 ────────────────────────────────────────

        /// <summary>
        /// 指定セルが空きかどうかを返す。
        /// 盤外（左右・下）は false、上空（_rows以上）は true を返す。
        /// </summary>
        public bool IsEmpty(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0) return false;
            if (cell.y >= _rows) return true; // スポーン時に上空を許容
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

            AppSound.PlayPuyoLand();
            OnPairLocked?.Invoke();

            // 連鎖チェックを非同期で実行（完了後に次のペアをスポーン）
            ProcessChainAsync(_cts.Token).Forget();
        }

        /// <summary>ぷよをグリッドに書き込み、位置をセルに合わせる</summary>
        private void Place(PuyoPiece piece)
        {
            var cell = piece.Cell;
            if (cell.y < _rows)
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
            // 敵スキル「炎」：炎マス上に置かれたぷよを連鎖判定より先に焼き消し、上に乗っていたぷよを落とす
            if (await BurnFireTilePiecesAsync(ct) > 0)
                await DropFloatingPuyosAsync(ct);

            var chainCount   = 0;
            var clearedCount = 0;

            while (TryFindChain(out var targets))
            {
                // 岩耐久チェック：耐久が残っているものは今回は消さず、見た目だけ変えて盤面に残す
                targets = ApplyRockDurability(targets);
                if (targets.Count == 0) break; // 全員が耐久ヒットのみだった（無限ループ防止）

                chainCount++;
                clearedCount += targets.Count;

                // 消えるぷよの重心をワールド座標で保存（COMBO 表示位置用）
                var centroidSum = Vector2.zero;
                foreach (var p in targets) centroidSum += new Vector2(p.Cell.x, p.Cell.y);
                var centroidAvg = centroidSum / targets.Count;
                LastChainCentroidWorld = transform.position
                    + new Vector3(centroidAvg.x * _cellSize, centroidAvg.y * _cellSize);

                OnAboutToClear?.Invoke(targets.Select(p => p.Cell).ToList());
                AppSound.PlayPuyoFlash();
                await UniTask.WhenAll(targets.Select(p => p.FlashMatchedAsync(ct)));
                AppSound.PlayPuyoClear();
                await ClearPuyosAsync(targets, ct);
                await DropFloatingPuyosAsync(ct);
                await UniTask.Delay(200, cancellationToken: ct);
                OnChainStep?.Invoke(chainCount, targets.Count);
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

            if (ct.IsCancellationRequested) return;

            // 盤面が落ち着いたので、炎マスを各列の「次にぷよが乗る場所」へ移す
            RelocateFireTilesToSurface();
            SpawnNextPair();
        }

        /// <summary>
        /// BFS で同色4つ以上の連結グループを探す。
        /// 見つかった消去対象ぷよを targets に格納して true を返す。
        /// </summary>
        private bool TryFindChain(out List<PuyoPiece> targets)
        {
            targets = new List<PuyoPiece>();
            var visited = new bool[COLS, _rows];

            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < _rows; y++)
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
                        if (next.x < 0 || next.x >= COLS || next.y < 0 || next.y >= _rows) continue;
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
                        if (next.x < 0 || next.x >= COLS || next.y < 0 || next.y >= _rows) continue;
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
        /// 敵スキル「炎」用：炎マス上に乗っているぷよを連鎖判定前に焼き消す。
        /// </summary>
        /// <summary>炎マスの上にあるぷよを焼く。焼いた数を返す</summary>
        private async UniTask<int> BurnFireTilePiecesAsync(CancellationToken ct)
        {
            if (_fireTiles.Count == 0) return 0;

            var burning = _fireTiles.Where(cell =>
                cell.x >= 0 && cell.x < COLS && cell.y >= 0 && cell.y < _rows && _grid[cell.x, cell.y] != null
            ).ToList();
            if (burning.Count == 0) return 0;

            await UniTask.WhenAll(burning.Select(cell => BurnCellAsync(cell, ct)));
            return burning.Count;
        }

        /// <summary>
        /// 炎マスの上のぷよを焼く：炎がボッと大きくなり、ぷよは黒く焦げながら縮んで少し浮き上がって消える。
        /// </summary>
        private async UniTask BurnCellAsync(Vector2Int cell, CancellationToken ct)
        {
            var piece = _grid[cell.x, cell.y];
            if (piece == null) return;
            _grid[cell.x, cell.y] = null;

            if (_fireTileMarkers.TryGetValue(cell, out var marker))
                marker.transform.DOPunchScale(Vector3.one * marker.transform.localScale.x * 0.4f, 0.3f, 6)
                    .SetLink(marker);

            const float charTime   = 0.12f; // 焦げる
            const float shrinkTime = 0.15f; // 縮んで消える
            piece.SetTint(new Color(0.25f, 0.18f, 0.12f));
            piece.transform.DOShakePosition(charTime, 0.08f * _cellSize, 20).SetLink(piece.gameObject);
            if (await UniTask.Delay(Mathf.RoundToInt(charTime * 1000), cancellationToken: ct)
                    .SuppressCancellationThrow()) { Destroy(piece.gameObject); return; }

            piece.transform.DOMove(piece.transform.position + Vector3.up * (0.3f * _cellSize), shrinkTime)
                .SetEase(Ease.OutQuad).SetLink(piece.gameObject);
            piece.transform.DOScale(0f, shrinkTime).SetEase(Ease.InQuad).SetLink(piece.gameObject);
            await UniTask.Delay(Mathf.RoundToInt(shrinkTime * 1000), cancellationToken: ct).SuppressCancellationThrow();
            Destroy(piece.gameObject);
        }

        /// <summary>
        /// 消去対象から岩ぷよ（耐久が残っているもの）を取り除く。
        /// 取り除かれたぷよは実際には消さず、耐久を1減らして見た目を変えるだけに留める。
        /// </summary>
        private List<PuyoPiece> ApplyRockDurability(List<PuyoPiece> targets)
        {
            var result = new List<PuyoPiece>(targets.Count);
            foreach (var piece in targets)
            {
                var rock = piece.GetComponent<RockPuyoState>();
                if (rock != null && rock.OnHit()) continue; // まだ耐久が残っている → 消去対象から除外
                result.Add(piece);
            }
            return result;
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
                for (var readY = 0; readY < _rows; readY++)
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
            for (var y = _rows - 1; y >= 0; y--)
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
                if (topY >= _rows) break; // 列が満杯ならそれ以上積まない

                var cell  = new Vector2Int(col, topY);
                var piece = Instantiate(_piecePrefab, transform);
                piece.Setup(PuyoColor.OJAMA, _colorSprites[(int)PuyoColor.OJAMA]);
                piece.Cell             = cell;
                _grid[cell.x, cell.y] = piece;

                // ボード上端の1マス外から落下開始
                var landPos = CellToWorld(cell);
                piece.transform.position = CellToWorld(new Vector2Int(col, _rows + 1));
                piece.transform.DOMove(landPos, fallDuration).SetEase(Ease.InQuad);

                await UniTask.Delay(Mathf.RoundToInt(fallDuration * 1000) + 30, cancellationToken: ct);

                // 着地演出
                AppSound.PlayOjamaLand();
                piece.PlayLandBounce();
            }
        }

        // ─── スキル用ヘルパー ─────────────────────────────────────

        /// <summary>最もぷよが多い行のインデックスを返す。盤面が空なら -1。</summary>
        public int FindDensestRow()
        {
            var best = -1;
            var bestCount = 0;
            for (var y = 0; y < _rows; y++)
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
            for (var y = 0; y < _rows - 1; y++)
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
            for (var y = _rows - 1; y >= 0; y--)
                for (var x = 0; x < COLS; x++)
                    if (_grid[x, y] != null) return y;
            return -1;
        }

        /// <summary>お邪魔ぷよの全座標リストを返す。</summary>
        public List<Vector2Int> GetOjamaPositions()
        {
            var positions = new List<Vector2Int>();
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < _rows; y++)
                if (_grid[x, y] != null && _grid[x, y].Color == PuyoColor.OJAMA)
                    positions.Add(new Vector2Int(x, y));
            return positions;
        }

        /// <summary>指定座標のぷよの色を返す。空・範囲外は null。</summary>
        public PuyoColor? GetColorAt(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0 || cell.y >= _rows) return null;
            return _grid[cell.x, cell.y]?.Color;
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
            for (var y = 0; y < _rows; y++)
                if (_grid[col, y] != null) count++;
            return count;
        }

        /// <summary>指定座標リストの中で非 null セルの数を返す（スキルダメージ計算用）。</summary>
        public int GetNonNullCountInCells(Vector2Int[] cells)
        {
            var count = 0;
            foreach (var c in cells)
                if (c.x >= 0 && c.x < COLS && c.y >= 0 && c.y < _rows && _grid[c.x, c.y] != null)
                    count++;
            return count;
        }

        /// <summary>非お邪魔ぷよの座標をランダムに最大 count 個返す。</summary>
        public List<Vector2Int> GetRandomPuyoPositions(int count)
        {
            var all = new List<Vector2Int>();
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < _rows; y++)
                if (_grid[x, y] != null && _grid[x, y].Color != PuyoColor.OJAMA)
                    all.Add(new Vector2Int(x, y));
            return all.OrderBy(_ => UnityEngine.Random.value).Take(count).ToList();
        }

        /// <summary>
        /// スキルによる1セル消去。通常連鎖と異なり、吹き飛ぶ演出で消える。
        /// </summary>
        public async UniTask ClearCellBySkillAsync(Vector2Int cell, CancellationToken ct)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0 || cell.y >= _rows) return;
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

        /// <summary>指定セルのぷよを返す。範囲外・空なら null。</summary>
        public PuyoPiece? GetPieceAt(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0 || cell.y >= _rows) return null;
            return _grid[cell.x, cell.y];
        }

        /// <summary>
        /// 演出用：指定セルに expected がまだ残っていれば盤面から外して true を返す。
        /// 消える動きと破棄は呼び出し側（敵スキルの演出）で行う。
        /// 連鎖で消えた・落下して別のセルへ移ったなどで残っていなければ何もせず false。
        /// </summary>
        public bool DetachPiece(Vector2Int cell, PuyoPiece expected)
        {
            if (GetPieceAt(cell) != expected) return false;
            _grid[cell.x, cell.y] = null;
            return true;
        }

        /// <summary>空きマス（表示範囲内）をランダムに1つ返す。空きがなければ null。</summary>
        public Vector2Int? GetRandomEmptyCell()
        {
            var empties = new List<Vector2Int>();
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < _visibleRows; y++)
                if (_grid[x, y] == null) empties.Add(new Vector2Int(x, y));
            if (empties.Count == 0) return null;
            return empties[UnityEngine.Random.Range(0, empties.Count)];
        }

        /// <summary>
        /// 敵スキル「岩」用：既存ぷよを岩化し、耐久値を付与する。
        /// 既に岩化済み・お邪魔ぷよ・セルが空の場合は何もせず false を返す。
        /// </summary>
        public bool ConvertToRock(Vector2Int cell, int durability, Sprite? hitSprite)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0 || cell.y >= _rows) return false;
            var piece = _grid[cell.x, cell.y];
            if (piece == null || piece.Color == PuyoColor.OJAMA) return false;
            if (piece.GetComponent<RockPuyoState>() != null) return false;

            var state = piece.gameObject.AddComponent<RockPuyoState>();
            state.Initialize(durability, hitSprite);
            // 岩になったことが一目でわかるよう灰色に染める（ヒット後はスプライト or さらに暗い灰色に変わる）
            piece.SetTint(FallbackSkillVisuals.Rock);
            return true;
        }

        /// <summary>
        /// 敵スキル「時限爆弾」用：盤面の空きマスに爆弾ぷよを設置する。
        /// お邪魔ぷよと同じ扱い（PuyoColor.OJAMA）にすることで、通常消去に隣接すればそのまま巻き込まれて解除される。
        /// 空きマスがなければ何もせず null を返す。
        /// </summary>
        public Vector2Int? PlaceBomb(Sprite? bombSprite, float fuseSeconds)
        {
            var cellOpt = GetRandomEmptyCell();
            if (cellOpt == null) return null;
            var cell = cellOpt.Value;

            var piece = Instantiate(_piecePrefab, transform);
            if (bombSprite != null)
            {
                piece.Setup(PuyoColor.OJAMA, bombSprite);
            }
            else
            {
                // 爆弾の画像が未設定なら、お邪魔ぷよを赤く染めて仮表示する
                piece.Setup(PuyoColor.OJAMA, _colorSprites[(int)PuyoColor.OJAMA]);
                piece.SetTint(FallbackSkillVisuals.Bomb);
            }
            piece.Cell                = cell;
            _grid[cell.x, cell.y]    = piece;
            piece.transform.position = CellToWorld(cell);

            var bomb = piece.gameObject.AddComponent<BombPuyoState>();
            bomb.Initialize(this, fuseSeconds, _cts.Token);

            return cell;
        }

        /// <summary>
        /// 敵スキル「時限爆弾」用：指定セルを中心に上下左右+自身の最大5マスをスキル消去する（爆発）。
        /// </summary>
        public async UniTask ExplodeAroundAsync(Vector2Int center, CancellationToken ct)
        {
            var cells = new[]
            {
                center, center + Vector2Int.up, center + Vector2Int.down,
                center + Vector2Int.left, center + Vector2Int.right,
            };
            await UniTask.WhenAll(cells.Select(c => ClearCellBySkillAsync(c, ct)));
        }

        /// <summary>
        /// 敵スキル「炎」用：指定セルを炎マスに指定する（ぷよの有無は問わない）。
        /// ぷよの背面に表示用マーカーを生成する。sprite 未指定時は半透明オレンジの四角で仮表示する。
        /// </summary>
        public void AddFireTile(Vector2Int cell, Sprite? sprite = null, Material? material = null)
        {
            _fireTiles.Add(cell);
            if (_fireTileMarkers.ContainsKey(cell)) return;

            var marker = new GameObject("FireTileMarker");
            marker.transform.SetParent(transform);
            marker.transform.position = CellToWorld(cell);
            var renderer = marker.AddComponent<SpriteRenderer>();
            // ぷよと同じソーティングレイヤーに置き、盤面背景より手前・ぷよ（とその発光）より奥に描く
            var pieceRenderer = _piecePrefab.GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = pieceRenderer.sortingLayerID;
            renderer.sortingOrder   = pieceRenderer.sortingOrder - FireTileMarkerOrderOffset;
            if (sprite != null)
            {
                // 炎の絵：マス1つに収まる大きさにし、ポンと出てから燃え続ける。
                // 揺らめきは専用シェーダー（material）が担当し、ここでは軽い呼吸のような伸び縮みだけ付ける
                renderer.sprite = sprite;
                if (material != null) renderer.sharedMaterial = material;
                var fit = _cellSize * FireTileSizeInCells / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                marker.transform.localScale = Vector3.zero;
                marker.transform.DOScale(fit, 0.2f).SetEase(Ease.OutBack).SetLink(marker)
                    .OnComplete(() => StartFlicker(marker.transform, fit));
            }
            else
            {
                renderer.sprite = FallbackSkillVisuals.SquareSprite;
                renderer.color  = FallbackSkillVisuals.FireTile;
                // 四角は1ワールド単位なので、マスより少し小さく合わせる
                marker.transform.localScale = Vector3.one * (_cellSize * 0.95f);
            }
            _fireTileMarkers[cell] = marker;
        }

        /// <summary>炎マスの絵の大きさ（ぷよ何個分か）</summary>
        private const float FireTileSizeInCells = 1.0f;

        /// <summary>炎の呼吸のような伸び縮み（揺らめき本体はシェーダー側。ずっと繰り返す）</summary>
        private static void StartFlicker(Transform marker, float baseScale)
        {
            marker.DOScale(new Vector3(baseScale * 0.98f, baseScale * 1.04f, 1f), 0.35f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(marker.gameObject);
        }

        // ─── 炎マスを「各列のいちばん上」で燃やす ──────────────────────

        /// <summary>
        /// 指定列で次にぷよが乗る場所（下から見て最初の空きマス）を返す。列が表示範囲いっぱいなら null。
        /// </summary>
        public Vector2Int? GetColumnSurface(int col)
        {
            if (col < 0 || col >= COLS) return null;
            for (var y = 0; y < _visibleRows; y++)
                if (_grid[col, y] == null) return new Vector2Int(col, y);
            return null;
        }

        /// <summary>指定列に炎マスがあるか</summary>
        public bool IsColumnOnFire(int col) => _fireTiles.Any(c => c.x == col);

        /// <summary>指定列の炎マスを解除する（表示マーカーも破棄する）</summary>
        public void RemoveFireTileInColumn(int col)
        {
            foreach (var cell in _fireTiles.Where(c => c.x == col).ToList())
                RemoveFireTile(cell);
        }

        /// <summary>
        /// 炎マスを、それぞれの列の「次にぷよが乗る場所」へ移す（ぷよが消えたり積み上がったりした後に呼ぶ）。
        /// 炎の絵はスッと新しい位置へ移動する。列が満杯なら動かさない。
        /// </summary>
        private void RelocateFireTilesToSurface()
        {
            foreach (var cell in _fireTiles.ToList())
            {
                var surface = GetColumnSurface(cell.x);
                if (surface == null || surface.Value == cell) continue;

                _fireTiles.Remove(cell);
                _fireTiles.Add(surface.Value);
                if (!_fireTileMarkers.Remove(cell, out var marker)) continue;
                _fireTileMarkers[surface.Value] = marker;
                marker.transform.DOMove(CellToWorld(surface.Value), 0.2f).SetEase(Ease.OutQuad).SetLink(marker);
            }
        }

        /// <summary>表示されている縦の行数</summary>
        public int VisibleRows => _visibleRows;

        /// <summary>
        /// 演出用の SpriteRenderer を、ぷよと同じソーティングレイヤーで、ぷよより orderAbovePuyo 段手前に描くよう設定する。
        /// </summary>
        public void ApplyPieceSorting(SpriteRenderer renderer, int orderAbovePuyo)
        {
            var pieceRenderer = _piecePrefab.GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = pieceRenderer.sortingLayerID;
            renderer.sortingOrder   = pieceRenderer.sortingOrder + orderAbovePuyo;
        }

        /// <summary>指定セルの炎マス指定を解除する（表示マーカーも破棄する）。</summary>
        public void RemoveFireTile(Vector2Int cell)
        {
            _fireTiles.Remove(cell);
            if (!_fireTileMarkers.TryGetValue(cell, out var marker)) return;
            Destroy(marker);
            _fireTileMarkers.Remove(cell);
        }

        /// <summary>全ての炎マス指定を解除する（表示マーカーも破棄する）。</summary>
        public void ClearFireTiles()
        {
            _fireTiles.Clear();
            foreach (var marker in _fireTileMarkers.Values) Destroy(marker);
            _fireTileMarkers.Clear();
        }

        /// <summary>指定セルが炎マスかどうかを返す。</summary>
        public bool IsFireTile(Vector2Int cell) => _fireTiles.Contains(cell);

        /// <summary>
        /// 指定セルのぷよを震わせる（被弾演出）。
        /// </summary>
        public async UniTask ShakePuyoAsync(Vector2Int cell, float duration, CancellationToken ct)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0 || cell.y >= _rows) return;
            var piece = _grid[cell.x, cell.y];
            if (piece == null) return;

            var shake = piece.transform.DOShakePosition(duration, strength: 0.12f, vibrato: 30);
            using (ct.Register(() => shake.Kill()))
                await shake.AsyncWaitForCompletion();
        }

        /// <summary>
        /// アニメーションなしで即時消去する。Dissolve などエフェクト側で演出済みの場合に使う。
        /// </summary>
        public void RemoveCellInstant(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= COLS || cell.y < 0 || cell.y >= _rows) return;
            var piece = _grid[cell.x, cell.y];
            if (piece == null) return;
            _grid[cell.x, cell.y] = null;
            Destroy(piece.gameObject);
        }

        /// <summary>盤面の全ぷよを消す（ストックMAX全消し用）。通常の消去アニメーションを使う。</summary>
        public async UniTask ClearAllPuyosAsync(CancellationToken ct)
        {
            var pieces = new List<PuyoPiece>();
            for (var x = 0; x < COLS; x++)
            for (var y = 0; y < _rows; y++)
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
            var spawnCell = new Vector2Int(COLS / 2 - 1, _rows - 1);

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
            if (_fallSpeedMultiplier != 1f)
                pair.SetFallSpeedMultiplier(_fallSpeedMultiplier);
            _activePair = pair;

            // NEXT・NEXT NEXT（キュー先頭2つ）を通知
            OnNextQueueChanged?.Invoke(NextPairs);
        }

        /// <summary>ゲーム中に色数を切り替える。次のスポーンから反映される。</summary>
        public void SetColorCount(int count)
            => _colorCount = Mathf.Clamp(count, 2, (int)PuyoColor.OJAMA);

        /// <summary>落下速度倍率を設定する。次のスポーンから反映される。</summary>
        public void SetFallSpeedMultiplier(float multiplier)
            => _fallSpeedMultiplier = Mathf.Max(multiplier, 0.01f);

        /// <summary>ステージの色数に応じてランダムな色を返す（OJAMAは対象外）</summary>
        private PuyoColor RandomColor() => (PuyoColor)UnityEngine.Random.Range(0, _colorCount);

        private PuyoPairColors RandomPairColors() => new PuyoPairColors { Main = RandomColor(), Sub = RandomColor() };

#if UNITY_EDITOR
        /// <summary>デバッグ用：各列の積み上げ高さを返す（AutoPlayAgent が使用）。</summary>
        public int[] Debug_GetColumnHeights()
        {
            var heights = new int[COLS];
            for (var x = 0; x < COLS; x++)
                for (var y = _rows - 1; y >= 0; y--)
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
