#nullable enable
namespace App
{
    /// <summary>
    /// 周期発動型の敵スキル基底クラス。
    /// 一定間隔（GetInterval）ごとに Execute を呼ぶタイマー処理を共通化する。
    /// レベルによる間隔・強度の変化は派生クラスで GetInterval / GetPower をオーバーライドする。
    /// </summary>
    public abstract class EnemySkillBase : IEnemySkill
    {
        public abstract EnemySkillType Type { get; }

        /// <summary>スキルレベル（1始まり）。高いほど発動頻度・強度が上がる</summary>
        public int Level { get; }

        private float _elapsed;

        protected EnemySkillBase(int level)
        {
            Level = level;
        }

        /// <summary>ステージ開始時に呼ばれる。経過時間をリセットする</summary>
        public virtual void OnEnter(GameContext ctx)
        {
            _elapsed = 0f;
        }

        /// <summary>毎フレーム呼ばれる。間隔に達したら Execute を発動する</summary>
        public void Tick(float deltaTime, GameContext ctx)
        {
            var interval = GetInterval(Level);
            if (interval <= 0f) return;

            _elapsed += deltaTime;
            if (_elapsed < interval) return;

            _elapsed = 0f;
            Execute(ctx);
        }

        /// <summary>次の発動までの残り秒数（デバッグ表示用）</summary>
        public float SecondsUntilNext => System.Math.Max(0f, GetInterval(Level) - _elapsed);

        /// <summary>
        /// 間隔を待たずに今すぐ発動し、タイマーを0から数え直す（デバッグウィンドウ用）。
        /// </summary>
        public void TriggerNow(GameContext ctx)
        {
            _elapsed = 0f;
            Execute(ctx);
        }

        /// <summary>ステージ終了・敵切替時に呼ばれる</summary>
        public virtual void OnRelease(GameContext ctx) { }

        /// <summary>レベルに応じた発動間隔（秒）を返す</summary>
        protected abstract float GetInterval(int level);

        /// <summary>レベルに応じた効果量（消去数・回復量など）を返す。使わないスキルはデフォルト(1)のままでよい</summary>
        protected virtual int GetPower(int level) => 1;

        /// <summary>発動間隔に達したときの実際の処理</summary>
        protected abstract void Execute(GameContext ctx);
    }
}
#nullable disable
