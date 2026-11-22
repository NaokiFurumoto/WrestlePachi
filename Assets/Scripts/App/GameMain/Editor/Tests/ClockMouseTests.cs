#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace App.Tests
{
    /// <summary>
    /// 敵スキル「時計」の演出の検証。
    /// ネズミがタイマーを1回かじるごとに1秒削る（かじる回数＝削る秒数）ことと、
    /// ネズミの前歯がちょうど数字の左端に当たる位置に置くこと。
    /// </summary>
    public sealed class ClockMouseTests
    {
        // 削る秒数をそのままかじる回数にする（端数は四捨五入、最低1回）
        [Test]
        public void BitesFor_RoundsSecondsAtLeastOne()
        {
            Assert.That(ClockSkill.BitesFor(5f),   Is.EqualTo(5));
            Assert.That(ClockSkill.BitesFor(7f),   Is.EqualTo(7));
            Assert.That(ClockSkill.BitesFor(5.4f), Is.EqualTo(5));
            Assert.That(ClockSkill.BitesFor(0.2f), Is.EqualTo(1));
        }

        // 絵の中の前歯の位置（左下0・右上1の割合）が、かじる場所に重なるようにネズミの中心を決める
        [Test]
        public void MouseCenter_PutsTeethOnTarget()
        {
            var size   = new Vector2(200f, 100f);
            var teeth  = new Vector2(0.8f, 0.3f);
            var target = new Vector2(50f, 20f);
            var center = ClockMouseEffect.MouseCenterFor(target, size, teeth);
            Assert.That(center.x, Is.EqualTo(50f - 0.3f * 200f).Within(1e-4f));
            Assert.That(center.y, Is.EqualTo(20f + 0.2f * 100f).Within(1e-4f));
        }
    }
}
#nullable disable
