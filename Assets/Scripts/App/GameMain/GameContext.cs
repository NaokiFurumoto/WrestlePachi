#nullable enable
using System;
using System.Collections.Generic;
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
        public  Game2DContents   Contents     { get; }
        public  ViewManager?     ViewMng      { get; }
        public  GameModeConfig   Config       { get; }
        /// <summary>現在ステージの設定。null の場合は Config の値をフォールバックとして使う</summary>
        public  StageConfig?     CurrentStage { get; set; }
        /// <summary>敵コントローラー。ダメージ・撃破判定に使用する</summary>
        public  EnemyController? Enemy        { get; set; }

        /// <summary>虹PUSH待ち状態。null=待機中でない。OnInputTengeki がここへ TrySetResult() する</summary>
        public UniTaskCompletionSource? RainbowInputSource { get; set; }

        /// <summary>虹保留中のバイブループ CTS。スキル完了時に Cancel() して停止する</summary>
        public CancellationTokenSource? RainbowVibrationCts { get; set; }

        /// <summary>ゲームをリスタートする。GameClearView の RETRY ボタンから呼ぶ</summary>
        public Action? RestartGame { get; set; }

        /// <summary>敵スキル「混乱」により左右入力が反転しているか</summary>
        public bool IsInputReversed { get; set; }

        /// <summary>敵スキル「スキル封印」により発動不可になっている HoldType の集合</summary>
        public HashSet<HoldType> SealedHoldTypes { get; } = new();

        /// <summary>敵スキル「黒保留強化」による黒保留出現分母の上書き値。null なら通常確率</summary>
        public int? BlackHoldDenominatorOverride { get; set; }

        /// <summary>敵スキル「黒保留威力」による黒保留消化時のお邪魔ぷよ数の上書き値。null なら通常値</summary>
        public int? BlackHoldOjamaCountOverride { get; set; }

        /// <summary>敵スキル「時計」から残り時間を削るためのコールバック。GameMainController が設定する</summary>
        public Action<float>? ReduceTime { get; set; }

        /// <summary>
        /// 敵スキルの予告を表示する（技が出る直前に敵の顔の横で技アイコンを出す）。
        /// GameMainController が HUD の表示処理を登録する。
        /// </summary>
        public Action<EnemySkillType>? ShowEnemySkillWarning { get; set; }

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
