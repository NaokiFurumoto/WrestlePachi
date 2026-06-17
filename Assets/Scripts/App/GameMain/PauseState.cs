#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;

namespace App
{
    /// <summary>
    /// ポーズ中ステート。
    /// OnEnter でゲームを一時停止してオプション画面を開き、
    /// 閉じられたら PlayingState へ戻る。
    /// </summary>
    public sealed class PauseState : GameStateBase
    {
        public override GamePhase Phase        => GamePhase.Paused;
        public override bool      AcceptsInput => false;
        public override bool      CanPause     => false; // ポーズ中は再ポーズ不可

        public PauseState(GameContext ctx) : base(ctx) { }

        public override void OnEnter(CancellationToken ct)
        {
            TimeManager.Pause();
            RunAsync(ct).Forget();
        }

        public override void OnExit()
        {
            TimeManager.Resume();
        }

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            var handle = await ViewManager.PushViewAsync<GameOptionView>(ViewKeys.GAME_OPTION);

            // GameOptionView が閉じられるまで待機
            await UniTask.WaitUntil(
                () => handle?.View == null || handle.View.IsClosed,
                cancellationToken: ct
            );

            if (!ct.IsCancellationRequested)
                _ctx.ChangeState(new PlayingState(_ctx));
        }
    }
}
#nullable disable
