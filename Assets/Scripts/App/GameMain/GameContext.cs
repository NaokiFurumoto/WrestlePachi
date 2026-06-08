#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameSys;

namespace App
{
    /// <summary>
    /// ステート間で共有するコンテキスト。
    /// 各ステートはこれを通じてステート遷移・コンテンツへのアクセスを行う。
    /// </summary>
    public sealed class GameContext
    {
        public  Game2DContents   Contents { get; }
        public  ViewManager?     ViewMng  { get; }
        public  GameModeConfig   Config   { get; }
        /// <summary>敵コントローラー。ダメージ・撃破判定に使用する</summary>
        public  EnemyController? Enemy    { get; set; }

        /// <summary>虹PUSH待ち状態。null=待機中でない。OnInputTengeki がここへ TrySetResult() する</summary>
        public UniTaskCompletionSource? RainbowInputSource { get; set; }

        /// <summary>虹保留中のバイブループ CTS。スキル完了時に Cancel() して停止する</summary>
        public CancellationTokenSource? RainbowVibrationCts { get; set; }

        private readonly Action<IGameState> _changeState;

        public GameContext(
            Game2DContents      contents,
            ViewManager?        viewMng,
            GameModeConfig      config,
            Action<IGameState>  changeState)
        {
            Contents     = contents;
            ViewMng      = viewMng;
            Config       = config;
            _changeState = changeState;
        }

        /// <summary>次のステートへ遷移する</summary>
        public void ChangeState(IGameState next) => _changeState(next);
    }
}
#nullable disable
