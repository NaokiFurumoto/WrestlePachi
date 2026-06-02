using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 落下中のぷよペア（2個1組）を管理するクラス。
    /// 入力は PuyoInputUI から公開メソッド経由で受け取る。
    /// </summary>
    public sealed class PuyoPair : MonoBehaviour
    {
        // ─── サブぷよのオフセット定義 ────────────────────────────
        // 回転インデックス：0=上, 1=右, 2=下, 3=左
        private static readonly Vector2Int[] SUB_OFFSETS =
        {
            new( 0,  1),  // 上
            new( 1,  0),  // 右
            new( 0, -1),  // 下
            new(-1,  0),  // 左
        };

        // ─── 落下設定 ────────────────────────────────────────────
        [Header("落下設定")]
        [SerializeField] private float _fallInterval     = 0.6f;  // 通常落下間隔（秒）
        [SerializeField] private float _softDropInterval = 0.05f; // 高速落下間隔（秒）
        [SerializeField] private float _lockDelay        = 0.3f;  // 着地後のロック猶予（秒）
        [SerializeField] private float _fallDuration     = 0.12f; // 落下アニメーション時間（秒）

        // ─── 内部状態 ────────────────────────────────────────────
        private PuyoBoard  _board;
        private PuyoPiece  _main;       // 軸ぷよ（回転の中心）
        private PuyoPiece  _sub;        // 従ぷよ（軸の周りを回る）
        private Sprite[]   _sprites;
        private Vector2Int _mainCell;   // 軸ぷよのグリッド座標
        private int        _rotation;   // 現在の回転インデックス（0〜3）

        private float _fallTimer;
        private bool  _isSoftDrop;     // 高速落下中フラグ
        private bool  _isLocked;       // ロック済みフラグ（二重処理防止）

        private CancellationTokenSource _cts;

        // ─── 初期化 ──────────────────────────────────────────────

        /// <summary>
        /// ボードから呼ばれる初期化メソッド。
        /// 軸ぷよ・従ぷよを生成してスポーン位置に配置する。
        /// </summary>
        public void Initialize(
            PuyoBoard  board,
            PuyoPiece  piecePrefab,
            Vector2Int spawnCell,
            PuyoColor  mainColor,
            PuyoColor  subColor,
            Sprite[]   sprites)
        {
            _board     = board;
            _sprites   = sprites;
            _rotation  = 0;         // 初期回転：サブが上
            _mainCell  = spawnCell;
            _cts       = CancellationTokenSource.CreateLinkedTokenSource(
                             destroyCancellationToken);

            // 軸ぷよ生成
            _main = Instantiate(piecePrefab, board.CellToWorld(spawnCell), Quaternion.identity, transform);
            _main.Setup(mainColor, sprites[(int)mainColor]);
            _main.Cell = spawnCell;

            // 従ぷよ生成（初期は軸の1つ上）
            var subCell = spawnCell + SUB_OFFSETS[_rotation];
            _sub = Instantiate(piecePrefab, board.CellToWorld(subCell), Quaternion.identity, transform);
            _sub.Setup(subColor, sprites[(int)subColor]);
            _sub.Cell = subCell;
        }

        private void OnDestroy() => _cts?.Cancel();

        // ─── 毎フレーム処理 ──────────────────────────────────────

        private void Update()
        {
            if (_isLocked) return;
            HandleFall();
        }

        /// <summary>タイマーで自動落下を処理する</summary>
        private void HandleFall()
        {
            var interval = _isSoftDrop ? _softDropInterval : _fallInterval;
            _fallTimer += Time.deltaTime;
            if (_fallTimer < interval) return;

            _fallTimer = 0f;

            if (!TryFall())
                LockAsync(_cts.Token).Forget();
        }

        // ─── 公開入力メソッド（PuyoInputUI から呼ぶ） ────────────

        /// <summary>左に1マス移動する</summary>
        public void MoveLeft()
        {
            if (!_isLocked) TryMove(-1);
        }

        /// <summary>右に1マス移動する</summary>
        public void MoveRight()
        {
            if (!_isLocked) TryMove(1);
        }

        /// <summary>時計回りに回転する</summary>
        public void RotateCW()
        {
            if (!_isLocked) TryRotate(1);
        }

        /// <summary>反時計回りに回転する</summary>
        public void RotateCCW()
        {
            if (!_isLocked) TryRotate(-1);
        }

        /// <summary>高速落下を開始する</summary>
        public void BeginSoftDrop()  => _isSoftDrop = true;

#if UNITY_EDITOR
        /// <summary>デバッグ用：軸ぷよの現在列（AutoPlayAgent が使用）。</summary>
        public int Debug_MainCol => _mainCell.x;
#endif

        /// <summary>高速落下を終了する</summary>
        public void EndSoftDrop()    => _isSoftDrop = false;

        // ─── 移動・回転ロジック ───────────────────────────────────

        /// <summary>横移動を試みる。成功したら true を返す</summary>
        private bool TryMove(int dx)
        {
            var newMain = _mainCell + new Vector2Int(dx, 0);
            var newSub  = newMain   + SUB_OFFSETS[_rotation];

            if (!_board.IsEmpty(newMain) || !_board.IsEmpty(newSub)) return false;

            _mainCell = newMain;
            RefreshPositions();
            return true;
        }

        /// <summary>
        /// 落下を1マス試みる。成功したら true を返す。
        /// 失敗（着地）したら false を返す。
        /// </summary>
        private bool TryFall()
        {
            var newMain = _mainCell + new Vector2Int(0, -1);
            var newSub  = newMain   + SUB_OFFSETS[_rotation];

            if (!_board.IsEmpty(newMain) || !_board.IsEmpty(newSub)) return false;

            _mainCell = newMain;
            // ソフトドロップ中はアニメなし（間隔が短すぎるため）
            RefreshPositions(_isSoftDrop ? 0f : _fallDuration);
            return true;
        }

        /// <summary>
        /// 回転を試みる。壁・床に当たる場合は壁蹴りを試みる。
        /// dir: 1=時計回り, -1=反時計回り
        /// </summary>
        private void TryRotate(int dir)
        {
            var newRot    = (_rotation + dir + 4) % 4;
            var newSubPos = _mainCell + SUB_OFFSETS[newRot];

            // そのまま回転できる場合
            if (_board.IsEmpty(newSubPos))
            {
                _rotation = newRot;
                RefreshPositions();
                return;
            }

            // 壁蹴り：サブが壁に入る場合、軸を逆方向にずらす
            var kick         = new Vector2Int(-SUB_OFFSETS[newRot].x, 0);
            var kickedMain   = _mainCell + kick;
            var kickedSubPos = kickedMain + SUB_OFFSETS[newRot];

            if (kick != Vector2Int.zero
                && _board.IsEmpty(kickedMain)
                && _board.IsEmpty(kickedSubPos))
            {
                _mainCell = kickedMain;
                _rotation = newRot;
                RefreshPositions();
            }
        }

        // ─── 位置更新 ────────────────────────────────────────────

        /// <summary>
        /// グリッド座標を元に軸・従ぷよの表示位置を更新する。
        /// animDuration > 0 のとき DOTween でスムーズに移動する。
        /// </summary>
        private void RefreshPositions(float animDuration = 0f)
        {
            var subCell  = _mainCell + SUB_OFFSETS[_rotation];
            var mainPos  = _board.CellToWorld(_mainCell);
            var subPos   = _board.CellToWorld(subCell);

            _main.Cell = _mainCell;
            _sub.Cell  = subCell;

            if (animDuration > 0f)
            {
                _main.transform.DOMove(mainPos, animDuration).SetEase(Ease.Linear);
                _sub.transform.DOMove(subPos,   animDuration).SetEase(Ease.Linear);
            }
            else
            {
                _main.transform.DOKill();
                _sub.transform.DOKill();
                _main.transform.position = mainPos;
                _sub.transform.position  = subPos;
            }
        }

        // ─── ロック処理 ──────────────────────────────────────────

        /// <summary>
        /// 着地後に少し待ってからグリッドにロックする。
        /// _lockDelay 内に移動・回転で再び浮けばキャンセルされる（ずらし入れ対応）。
        /// </summary>
        private async UniTaskVoid LockAsync(CancellationToken ct)
        {
            _isLocked = true;

            // ロック猶予：この間に入力で再度浮けばキャンセル
            await UniTask.Delay(
                (int)(_lockDelay * 1000),
                cancellationToken: ct);

            // 猶予後に再チェック（移動でまだ浮けるなら続行）
            if (TryFall())
            {
                _isLocked = false;
                return;
            }

            // 進行中のアニメを完了させてからボードに渡す
            _main.transform.DOKill(complete: true);
            _sub.transform.DOKill(complete: true);
            _main.transform.SetParent(_board.transform);
            _sub.transform.SetParent(_board.transform);
            _board.LockPair(_main, _sub);

            Destroy(gameObject);
        }
    }
}
