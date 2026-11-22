#nullable enable
using App.Effects;
using NUnit.Framework;
using UnityEngine;

namespace App.Tests
{
    /// <summary>
    /// 敵スキル「混乱」の演出の検証。
    /// ヒヨコの数が残り時間を表す（3羽が1羽ずつ飛び去り、いなくなったら終わり）ことと、
    /// 混乱中は左右ボタンに紫の「逆向きの矢印」を出すこと。
    /// </summary>
    public sealed class ConfusionEffectTests
    {
        // 3羽は時間の 1/3 ごとに1羽ずつ減り、終わった瞬間に0羽になる
        [Test]
        public void ChicksRemaining_DecreasesEveryThird()
        {
            Assert.That(ConfusionChicksEffect.ChicksRemaining(0f,    6f, 3), Is.EqualTo(3));
            Assert.That(ConfusionChicksEffect.ChicksRemaining(1.9f,  6f, 3), Is.EqualTo(3));
            Assert.That(ConfusionChicksEffect.ChicksRemaining(2.1f,  6f, 3), Is.EqualTo(2));
            Assert.That(ConfusionChicksEffect.ChicksRemaining(4.1f,  6f, 3), Is.EqualTo(1));
            Assert.That(ConfusionChicksEffect.ChicksRemaining(5.99f, 6f, 3), Is.EqualTo(1));
            Assert.That(ConfusionChicksEffect.ChicksRemaining(6f,    6f, 3), Is.EqualTo(0));
            Assert.That(ConfusionChicksEffect.ChicksRemaining(9f,    6f, 3), Is.EqualTo(0));
        }

        // 長さ0以下でも例外にならず、ヒヨコは出ない
        [Test]
        public void ChicksRemaining_ZeroDuration_ReturnsZero()
            => Assert.That(ConfusionChicksEffect.ChicksRemaining(0f, 0f, 3), Is.EqualTo(0));

        // ヒヨコの輪はペアの頭の少し上に出るが、盤面のいちばん上より上には行かない
        [Test]
        public void OrbitCenter_AbovePair_ClampedToBoardTop()
        {
            var low = ConfusionChicksEffect.OrbitCenter(new Vector3(1f, 2f, 0f), cellSize: 1f, maxY: 10f);
            Assert.That(low.x, Is.EqualTo(1f));
            Assert.That(low.y, Is.EqualTo(2f + ConfusionChicksEffect.HeightAbovePairInCells).Within(1e-5f));

            var high = ConfusionChicksEffect.OrbitCenter(new Vector3(1f, 12f, 0f), cellSize: 1f, maxY: 10f);
            Assert.That(high.y, Is.EqualTo(10f));
        }

        // 混乱中は、左ボタンに紫の右矢印・右ボタンに紫の左矢印を出す。紫の絵がなければ普通の矢印を入れ替える
        [Test]
        public void ArrowSprite_SwapsAndTurnsPurpleWhileReversed()
        {
            var left       = MakeSprite();
            var right      = MakeSprite();
            var purpleLeft = MakeSprite();
            var purpleRight = MakeSprite();

            Assert.That(PuyoInputView.ArrowSpriteFor(true,  false, left, right, purpleLeft, purpleRight), Is.SameAs(left));
            Assert.That(PuyoInputView.ArrowSpriteFor(false, false, left, right, purpleLeft, purpleRight), Is.SameAs(right));
            Assert.That(PuyoInputView.ArrowSpriteFor(true,  true,  left, right, purpleLeft, purpleRight), Is.SameAs(purpleRight));
            Assert.That(PuyoInputView.ArrowSpriteFor(false, true,  left, right, purpleLeft, purpleRight), Is.SameAs(purpleLeft));
            Assert.That(PuyoInputView.ArrowSpriteFor(true,  true,  left, right, null, null), Is.SameAs(right));
        }

        private static Sprite MakeSprite()
            => Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
    }
}
#nullable disable
