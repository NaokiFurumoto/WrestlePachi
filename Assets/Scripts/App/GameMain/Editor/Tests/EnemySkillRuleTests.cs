#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using static App.EnemySkillType;
using Object = UnityEngine.Object;

namespace App.Tests
{
    /// <summary>
    /// 敵スキル決定ルール（1戦目／2戦目）の検証。
    /// </summary>
    public sealed class EnemySkillRuleTests
    {
        /// <summary>テスト用に EnemySkillEntry を簡潔に作るヘルパー</summary>
        internal static EnemySkillEntry E(EnemySkillType type, int level)
            => new EnemySkillEntry { Type = type, Level = level };

        [Test]
        public void FirstFight_ReturnsSignatureAsIs()
            => Assert.That(EnemySkillRule.Resolve(new[] { E(Clock, 1) }, new[] { E(GarbageDrop, 1) }, false),
                           Is.EqualTo(new[] { E(Clock, 1) }));

        [Test]
        public void Rematch_RaisesSignatureLevelAndAddsExtra()
            => Assert.That(EnemySkillRule.Resolve(new[] { E(Clock, 1) }, new[] { E(GarbageDrop, 1) }, true),
                           Is.EqualTo(new[] { E(Clock, 2), E(GarbageDrop, 1) }));

        [Test]
        public void Rematch_LevelIsCappedAt3()
            => Assert.That(EnemySkillRule.Resolve(new[] { E(FireTile, 3) }, Array.Empty<EnemySkillEntry>(), true),
                           Is.EqualTo(new[] { E(FireTile, 3) }));

        // クロマスク：得意技なし・2戦目だけ技を使う
        [Test]
        public void Rematch_EmptySignature_ReturnsExtraOnly()
            => Assert.That(EnemySkillRule.Resolve(Array.Empty<EnemySkillEntry>(), new[] { E(GarbageDrop, 1) }, true),
                           Is.EqualTo(new[] { E(GarbageDrop, 1) }));

        [Test]
        public void Rematch_DuplicateType_KeepsHigherLevel()
            => Assert.That(EnemySkillRule.Resolve(new[] { E(Rock, 1) }, new[] { E(Rock, 1) }, true),
                           Is.EqualTo(new[] { E(Rock, 2) }));

        [Test]
        public void NullEnemy_ReturnsEmpty()
            => Assert.That(EnemySkillRule.Resolve((EnemyData?)null, true), Is.Empty);

        [Test]
        public void EnemyDataOverload_UsesConfiguredSkills()
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.Configure("テスト", null, new[] { E(Shark, 1) }, new[] { E(Rock, 2) });
            Assert.That(EnemySkillRule.Resolve(enemy, true), Is.EqualTo(new[] { E(Shark, 2), E(Rock, 2) }));
            Object.DestroyImmediate(enemy);
        }
    }
}
#nullable disable
