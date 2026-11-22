#nullable enable
using App.Puyo;
using NUnit.Framework;
using UnityEngine;

namespace App.Tests
{
    /// <summary>
    /// 岩ぷよの耐久と、殻の絵の切り替えの検証。
    /// </summary>
    public sealed class RockPuyoStateTests
    {
        // 耐久2：1回目は耐えて残り、2回目で消える
        [Test]
        public void OnHit_DurabilityTwo_SurvivesOnlyFirstHit()
        {
            var go = new GameObject("rock", typeof(SpriteRenderer), typeof(PuyoPiece));
            try
            {
                var rock  = go.AddComponent<RockPuyoState>();
                var shell = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
                // 殻の絵を渡す（エディタのテストでは PuyoPiece.Awake が走らず、灰色に染める仮表示は使えないため）
                rock.Initialize(2, new RockVisuals(shell, null, null, null));
                Assert.That(rock.OnHit(), Is.True);
                Assert.That(rock.HitCount, Is.EqualTo(1));
                Assert.That(rock.OnHit(), Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }

        // 殻の絵：まだ当たっていなければ普通の殻、1回でも当たればヒビ入り。ヒビ入りの絵がなければ普通の殻のまま
        [Test]
        public void ShellSpriteFor_SwitchesToCrackAfterFirstHit()
        {
            var shell = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            var crack = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            Assert.That(RockPuyoState.ShellSpriteFor(0, shell, crack), Is.SameAs(shell));
            Assert.That(RockPuyoState.ShellSpriteFor(1, shell, crack), Is.SameAs(crack));
            Assert.That(RockPuyoState.ShellSpriteFor(3, shell, crack), Is.SameAs(crack));
            Assert.That(RockPuyoState.ShellSpriteFor(2, shell, null), Is.SameAs(shell));
        }
    }
}
#nullable disable
