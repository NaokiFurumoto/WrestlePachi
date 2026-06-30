#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace App
{
    /// <summary>
    /// ゲームオーバーステート。
    /// スポーン位置が埋まった・時間切れのときに遷移し、入力をすべてブロックする。
    /// </summary>
    public sealed class GameOverState : GameStateBase
    {
        public GameOverState(GameContext ctx) : base(ctx) { }

        public override GamePhase Phase        => GamePhase.GameOver;
        public override bool      AcceptsInput => false;

        public override void OnEnter(CancellationToken ct)
            => RunAsync(ct).Forget();

        private async UniTaskVoid RunAsync(CancellationToken ct)
        {
            _ctx.Contents.PuyoBoard.Suspend();
            _ctx.Contents.BallLauncher.Suspend();
            AppSound.FadeOutBGM(2f);
            AppSound.PlayGameOver();

            var stamina    = StaminaManager.isValid ? StaminaManager.Instance.Current : 0;
            var maxStamina = StaminaManager.isValid ? StaminaManager.Instance.Max     : 0;

            var viewData = new GameOverView.GameOverViewData
            {
                StageNumber    = (_ctx.Enemy?.EnemyIndex ?? 0) + 1,
                EnemyName      = _ctx.Enemy?.EnemyName ?? "",
                EnemySprite    = _ctx.Enemy?.FaceSprite,
                CurrentStamina = stamina,
                MaxStamina     = maxStamina,
                OnRetry        = _ctx.RestartGame,
                OnTitle        = () =>
                {
                    if (SceneManager.isValid)
                        SceneManager.Instance.TransitScene("BootScene");
                    else
                        UnitySceneManager.LoadScene("BootScene");
                },
            };
            await ViewManager.PushViewAsync<GameOverView>(ViewKeys.GAME_OVER, viewData);
        }

        // ゲームオーバー後はいかなるイベントも無視する
        public override void OnPairLocked()                              { }
        public override void OnChainCompleted(int chainCount, int clearedCount) { }
        public override void OnNextPairSpawned()                         { }
        public override void OnBoardGameOver()                           { }
    }
}
#nullable disable
