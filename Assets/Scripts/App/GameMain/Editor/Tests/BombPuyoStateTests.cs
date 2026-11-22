#nullable enable
using App.Puyo;
using NUnit.Framework;
using UnityEngine;

namespace App.Tests
{
    /// <summary>
    /// 時限爆弾の点滅の速さと、導火線の火花の位置の検証。
    /// </summary>
    public sealed class BombPuyoStateTests
    {
        // 置いた直後はゆっくり、爆発直前ほど速く点滅する
        [Test]
        public void PulsePeriod_GetsShorterAsFuseRunsOut()
        {
            Assert.That(BombPuyoState.PulsePeriodFor(1f), Is.EqualTo(BombPuyoState.SlowestPulseSec).Within(0.001f));
            Assert.That(BombPuyoState.PulsePeriodFor(0f), Is.EqualTo(BombPuyoState.FastestPulseSec).Within(0.001f));
            Assert.That(BombPuyoState.PulsePeriodFor(0.3f), Is.LessThan(BombPuyoState.PulsePeriodFor(0.7f)));
            // 範囲外の値でも上下限に収まる
            Assert.That(BombPuyoState.PulsePeriodFor(-1f), Is.EqualTo(BombPuyoState.FastestPulseSec).Within(0.001f));
            Assert.That(BombPuyoState.PulsePeriodFor(2f), Is.EqualTo(BombPuyoState.SlowestPulseSec).Within(0.001f));
        }

        // 導火線の先端（絵の中の割合の位置）を、爆弾の中心から見た位置（ワールド単位）に直す
        [Test]
        public void FuseTipLocal_ConvertsFromPivotUsingPixelsPerUnit()
        {
            // 4x4px・中心を基準点・4px=1単位の絵で、右上隅(1,1)は中心から(0.5,0.5)
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
            var local  = BombPuyoState.FuseTipLocal(sprite, new Vector2(1f, 1f));
            Assert.That(local.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(local.y, Is.EqualTo(0.5f).Within(0.001f));
            // 絵がなければ真上あたりに置く
            Assert.That(BombPuyoState.FuseTipLocal(null, new Vector2(1f, 1f)), Is.EqualTo(BombPuyoState.FallbackFuseTip));
        }
    }
}
#nullable disable
