#nullable enable
using App.Puyo;
using NUnit.Framework;
using UnityEngine;

namespace App.Tests
{
    /// <summary>
    /// 盤面画像のない敵スキル用の仮表示（色・四角スプライト）の検証。
    /// </summary>
    public sealed class FallbackSkillVisualsTests
    {
        // 炎マスの四角は1ワールド単位の正方形で、毎回作り直さず使い回す
        [Test]
        public void SquareSprite_IsOneUnitAndCached()
        {
            var sprite = FallbackSkillVisuals.SquareSprite;
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(sprite.bounds.size.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(FallbackSkillVisuals.SquareSprite, Is.SameAs(sprite));
        }

        // 岩のヒット後：専用画像があれば灰色の乗算を外して画像本来の色で見せ、なければ暗い灰色にする
        [Test]
        public void RockHitTint_WithSprite_IsWhite_WithoutSprite_IsRockHit()
        {
            var sprite = FallbackSkillVisuals.SquareSprite;
            Assert.That(RockPuyoState.HitTintFor(sprite), Is.EqualTo(Color.white));
            Assert.That(RockPuyoState.HitTintFor(null), Is.EqualTo(FallbackSkillVisuals.RockHit));
        }

        // 炎マスは半透明、爆弾・岩は不透明
        [Test]
        public void Colors_FireIsTranslucent_OthersOpaque()
        {
            Assert.That(FallbackSkillVisuals.FireTile.a, Is.LessThan(1f));
            Assert.That(FallbackSkillVisuals.Bomb.a, Is.EqualTo(1f));
            Assert.That(FallbackSkillVisuals.Rock.a, Is.EqualTo(1f));
            Assert.That(FallbackSkillVisuals.RockHit.r, Is.LessThan(FallbackSkillVisuals.Rock.r));
        }
    }
}
#nullable disable
