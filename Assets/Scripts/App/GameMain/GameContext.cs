#nullable enable
using System;
using GameSys;

namespace App
{
    /// <summary>
    /// ステート間で共有するコンテキスト。
    /// 各ステートはこれを通じてステート遷移・コンテンツへのアクセスを行う。
    /// </summary>
    public sealed class GameContext
    {
        public  Game2DContents  Contents { get; }
        public  ViewManager?    ViewMng  { get; }
        public  GameModeConfig  Config   { get; }

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
