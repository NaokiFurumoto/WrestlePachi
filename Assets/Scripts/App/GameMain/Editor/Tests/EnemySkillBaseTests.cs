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

        [Test]
        public void Level_IsReadableFromOutside()
            => Assert.That(((IEnemySkill)new CountingSkill(2)).Level, Is.EqualTo(2));
    }
}
#nullable disable
