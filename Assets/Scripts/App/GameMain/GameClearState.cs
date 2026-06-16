#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;

namespace App
{
    /// <summary>
    /// 敵撃破後のステージクリア演出を管理するステート。
    /// SuspendAll → ゴング×4 → ぷよ全消去 → GameClearView 表示 の順で進行する。
    /// hasNextStage=true のときは「次のステージへ」ボタンが有効になる。
    /// </summary>
    public sealed class GameClearState : GameStateBase
    {
        private readonly bool    _hasNextStage;
        private readonly Action? _onNextStage;

        public GameClearState(GameContext ctx, bool hasNextStage = false, Action? onNextStage = null) : base(ctx)
        {
            _hasNextStage = hasNextStage;
            _onNextStage  = onNextStage;
        }

        public override GamePhase Phase        => GamePhase.GameClear;
        public override bool      AcceptsInput => false;

        public override void OnBoardGameOver() { }

        public override void OnEnter(CancellationToken ct)
            => RunAsync(ct).Forget();

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            SuspendAll();
            if (await WaitFlashAndGongsAsync(ct)) return;
            await _ctx.Contents.PuyoBoard.ClearAllPuyosAsync(ct);

            var viewData = new GameClearView.GameClearViewData
            {
                StageNumber  = (_ctx.Enemy?.EnemyIndex ?? 0) + 1,
                EnemyName    = _ctx.Enemy?.EnemyName   ?? "",
                EnemySprite  = _ctx.Enemy?.FaceSprite,
                HasNextStage = _hasNextStage,
                OnNextStage  = _onNextStage,
                OnRetry      = _ctx.RestartGame,
            };
            await ViewManager.PushViewAsync<GameClearView>(ViewKeys.GAME_CLEAR, viewData);
        }

        private void SuspendAll()
        {
            _ctx.Contents.PuyoBoard.Suspend();
            _ctx.Contents.PuyoBoard.ClearAll();
            _ctx.Contents.BallLauncher.Suspend();
            AppSound.FadeOutBGM(3f);
        }

        private static async UniTask<bool> WaitFlashAndGongsAsync(CancellationToken ct)
        {
            await UniTask.Delay(500, DelayType.Realtime);

            for (int i = 0; i < 4; i++)
            {
                AppSound.PlayGong();
                if (i < 3) await UniTask.Delay(330, DelayType.Realtime);
            }

            return ct.IsCancellationRequested;
        }
    }
}
#nullable disable
