#nullable enable
using NUnit.Framework;

namespace App.Tests
{
    /// <summary>
    /// 敵スキルの予告で画面の縁を赤く光らせる、光の濃さの検証。
    /// 盤面のある中央はふさがず、端へ近づくほど濃く、角がいちばん濃い。
    /// </summary>
    public sealed class EnemyWarningEdgeGlowTests
    {
        [Test]
        public void Center_IsClear()
            => Assert.That(GameMainHudView.EdgeGlowAlpha(0.5f, 0.5f), Is.EqualTo(0f).Within(1e-5f));

        // 盤面の左右の端あたり（横の 1/4）まではふさがない
        [Test]
        public void InnerArea_IsClear()
        {
            Assert.That(GameMainHudView.EdgeGlowAlpha(0.2f, 0.5f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(GameMainHudView.EdgeGlowAlpha(0.5f, 0.15f), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Edges_AreStrong_CornerStrongest()
        {
            Assert.That(GameMainHudView.EdgeGlowAlpha(0f, 0.5f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(GameMainHudView.EdgeGlowAlpha(0.5f, 1f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(GameMainHudView.EdgeGlowAlpha(0.05f, 0.5f), Is.GreaterThan(GameMainHudView.EdgeGlowAlpha(0.1f, 0.5f)));
            Assert.That(GameMainHudView.EdgeGlowAlpha(0.08f, 0.04f), Is.GreaterThan(GameMainHudView.EdgeGlowAlpha(0.08f, 0.5f)));
        }
    }
}
#nullable disable
