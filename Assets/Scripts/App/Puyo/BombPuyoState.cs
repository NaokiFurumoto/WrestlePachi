#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Puyo
{
    /// <summary>
    /// 時限爆弾ぷよの状態。
    /// 敵スキル「時限爆弾」により PuyoBoard.PlaceBomb で生成されたぷよにアタッチされる。
    /// 設置から一定時間放置されると周囲を巻き込んで自動的に爆発する。
    /// 通常のぷよ消去に巻き込まれて先に破棄された場合（＝隣接消去で解除された場合）は
    /// OnDestroy でタイマーを止め、爆発しない。
    /// </summary>
    [RequireComponent(typeof(PuyoPiece))]
    public sealed class BombPuyoState : MonoBehaviour
    {
        private PuyoPiece?               _piece;
        private PuyoBoard?               _board;
        private CancellationTokenSource? _cts;

        private void Awake() => _piece = GetComponent<PuyoPiece>();

        /// <summary>設置後 fuseSeconds 秒放置されると自動で爆発する</summary>
        public void Initialize(PuyoBoard board, float fuseSeconds, CancellationToken linkedCt)
        {
            _board = board;
            _cts   = CancellationTokenSource.CreateLinkedTokenSource(linkedCt);
            RunFuseAsync(fuseSeconds, _cts.Token).Forget();
        }

        private async UniTaskVoid RunFuseAsync(float fuseSeconds, CancellationToken ct)
        {
            await UniTask.Delay(Mathf.RoundToInt(fuseSeconds * 1000), cancellationToken: ct)
                .SuppressCancellationThrow();
            if (ct.IsCancellationRequested || _piece == null || _board == null) return;

            await _board.ExplodeAroundAsync(_piece.Cell, ct);
        }

        private void OnDestroy()
        {
            // 通常消去に巻き込まれて先に破棄された（＝隣接消去で解除された）場合はここでタイマーを止める
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
#nullable disable
