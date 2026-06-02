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
        public const int COLS          = 6;  // 横列数
        public const int ROWS          = 14; // 内部縦行数（上2段は非表示バッファ）
        public const int VISIBLE_ROWS  = 12; // 表示縦行数（本家準拠）

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
            _colorCount = Mathf.Clamp(colorCount, 2, (int)PuyoColor.OJAMA);
            _grid       = new PuyoPiece[COLS, ROWS];
            _cts        = CancellationTokenSource.CreateLinkedTokenSource(
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
                    targets.AddRange(group);
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
#endif
    }
}
