#nullable enable
using NUnit.Framework;

namespace App.Tests
{
    /// <summary>
    /// 周期発動型スキルの「次の発動までの秒数」「今すぐ発動」（デバッグ用）の検証。
    /// </summary>
    public sealed class EnemySkillBaseTests
    {
        /// <summary>発動間隔10秒・発動回数を数えるだけのテスト用スキル</summary>
        private sealed class CountingSkill : EnemySkillBase
        {
            public int ExecuteCount;
            public CountingSkill(int level) : base(level) { }
            public override EnemySkillType Type => EnemySkillType.Clock;
            protected override float GetInterval(int level) => 10f;
            protected override void Execute(GameContext ctx) => ExecuteCount++;
        }

        [Test]
        public void SecondsUntilNext_DecreasesWithTick()
        {
            var skill = new CountingSkill(1);
            skill.Tick(4f, null!);
            Assert.That(skill.SecondsUntilNext, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void TriggerNow_ExecutesOnceAndRestartsTimer()
        {
            var skill = new CountingSkill(1);
            skill.Tick(4f, null!);
            skill.TriggerNow(null!);
            Assert.That(skill.ExecuteCount, Is.EqualTo(1));
            Assert.That(skill.SecondsUntilNext, Is.EqualTo(10f).Within(0.001f));
        }

        /// <summary>予告の回数を数える GameContext を作る（盤面などは使わないので null）</summary>
        private static GameContext CountingContext(System.Action onWarn)
        {
            var ctx = new GameContext(null!, null, null!, _ => { });
            ctx.ShowEnemySkillWarning = _ => onWarn();
            return ctx;
        }

        // 予告は発動の0.5秒前に1回だけ出る
        [Test]
        public void Warning_FiresOnceHalfSecondBeforeExecute()
        {
            var warns = 0;
            var ctx   = CountingContext(() => warns++);
            var skill = new CountingSkill(1);

            skill.Tick(9.4f, ctx);
            Assert.That(warns, Is.EqualTo(0), "0.6秒前はまだ出ない");
            skill.Tick(0.2f, ctx);
            Assert.That(warns, Is.EqualTo(1), "0.4秒前で出る");
            skill.Tick(0.2f, ctx);
            Assert.That(warns, Is.EqualTo(1), "同じ周期では2回出ない");
            skill.Tick(0.3f, ctx);
            Assert.That(skill.ExecuteCount, Is.EqualTo(1));
            skill.Tick(9.6f, ctx);
            Assert.That(warns, Is.EqualTo(2), "次の周期でまた出る");
        }

        // デバッグの「今すぐ発動」の後も、次の周期で予告が出る
        [Test]
        public void Warning_FiresAgainAfterTriggerNow()
        {
            var warns = 0;
            var ctx   = CountingContext(() => warns++);
            var skill = new CountingSkill(1);

            skill.Tick(9.6f, ctx);
            skill.TriggerNow(ctx);
            skill.Tick(9.6f, ctx);
            Assert.That(warns, Is.EqualTo(2));
        }

        [Test]
        public void Level_IsReadableFromOutside()
            => Assert.That(((IEnemySkill)new CountingSkill(2)).Level, Is.EqualTo(2));
    }
}
#nullable disable
