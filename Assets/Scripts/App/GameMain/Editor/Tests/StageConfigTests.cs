#nullable enable
using NUnit.Framework;
using UnityEngine;
using static App.EnemySkillType;
using static App.Tests.EnemySkillRuleTests;
using Object = UnityEngine.Object;

namespace App.Tests
{
    /// <summary>
    /// StageConfig の敵関連プロパティ（敵アセット参照・2戦目フラグ）の検証。
    /// </summary>
    public sealed class StageConfigTests
    {
        // 敵を入れ忘れたステージでも例外を出さず、安全な既定値を返す
        [Test]
        public void NoEnemyNoLegacy_ReturnsSafeDefaults()
        {
            var s = new StageConfig();
            Assert.That(s.EnemyName, Is.EqualTo(""));
            Assert.That(s.EnemySprite, Is.Null);
            Assert.That(s.EnemyLevel, Is.EqualTo(1));
            Assert.That(s.EnemySkills, Is.Empty);
        }

        [Test]
        public void NoEnemy_FallsBackToLegacy()
        {
            var s = new StageConfig { LegacyEnemyName = "クロマスク", LegacyEnemyLevel = 1 };
            Assert.That(s.EnemyName, Is.EqualTo("クロマスク"));
        }

        [Test]
        public void WithEnemy_Rematch_UsesRuleAndLevel2()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.Configure("アニマスク", null, new[] { E(Clock, 1) }, new[] { E(GarbageDrop, 1) });
            var s = new StageConfig { Enemy = enemy, IsRematch = true, LegacyEnemyName = "古い名前" };
            Assert.That(s.EnemyName, Is.EqualTo("アニマスク"));
            Assert.That(s.EnemyLevel, Is.EqualTo(2));
            Assert.That(s.EnemySkills, Is.EqualTo(new[] { E(Clock, 2), E(GarbageDrop, 1) }));
            Object.DestroyImmediate(enemy);
        }
    }
}
#nullable disable
