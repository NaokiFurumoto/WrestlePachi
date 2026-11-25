#nullable enable
using System.Threading;
using NUnit.Framework;

namespace App.Tests
{
    /// <summary>
    /// 敵スキル「黒保留強化」＝黒塗り：一定間隔で小悪魔が飛んできて、並んでいる保留のうち
    /// いちばん新しい普通の保留を黒保留に塗り替える。並んでいなければ次に出てきた保留を塗る。
    /// 虹・黒・鎖のかかった保留は塗らない。
    /// </summary>
    public sealed class HoldPaintTests
    {
        /// <summary>消化シーケンス（時間待ち）が走らないよう、キャンセル済みのトークンで作る</summary>
        private static HoldSystem NewHolds()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            return new HoldSystem(cts.Token);
        }

        [Test]
        public void RequestPaint_PaintsNewestNormalHold()
        {
            var holds = NewHolds();
            holds.AddHold(HoldType.Red);
            holds.AddHold(HoldType.Blue);
            var painted = -1;
            holds.OnHoldPainted += i => painted = i;

            holds.RequestPaint();

            Assert.That(holds.TypeAt(0), Is.EqualTo(HoldType.Red));
            Assert.That(holds.TypeAt(1), Is.EqualTo(HoldType.Black), "いちばん新しい保留が黒になる");
            Assert.That(painted, Is.EqualTo(1));
            Assert.That(holds.HasPaintRequest, Is.False);
        }

        // 虹・黒・鎖つきは飛ばして、その前の普通の保留を塗る
        [Test]
        public void SkipsRainbowBlackAndLocked()
        {
            var holds = NewHolds();
            holds.AddHold(HoldType.Green);
            holds.RequestLock();
            holds.AddHold(HoldType.Yellow); // 鎖つき
            holds.AddHold(HoldType.Black);
            holds.AddHold(HoldType.Rainbow);

            holds.RequestPaint();

            Assert.That(holds.TypeAt(0), Is.EqualTo(HoldType.Black), "普通の保留は緑だけなので緑が塗られる");
            Assert.That(holds.TypeAt(1), Is.EqualTo(HoldType.Yellow));
            Assert.That(holds.TypeAt(3), Is.EqualTo(HoldType.Rainbow));
        }

        // 塗れる保留がなければ、次に出てきた普通の保留を塗る
        [Test]
        public void NoTarget_PaintsNextHold()
        {
            var holds = NewHolds();
            holds.RequestPaint();
            Assert.That(holds.HasPaintRequest, Is.True);

            holds.AddHold(HoldType.Rainbow);
            holds.AddHold(HoldType.Purple);

            Assert.That(holds.TypeAt(0), Is.EqualTo(HoldType.Rainbow));
            Assert.That(holds.TypeAt(1), Is.EqualTo(HoldType.Black));
            Assert.That(holds.HasPaintRequest, Is.False);
        }

        // 塗られた保留は、消化のとき黒保留として出る
        [Test]
        public void PaintedHold_IsConsumedAsBlack()
        {
            var holds = NewHolds();
            holds.AddHold(HoldType.Red);
            holds.RequestPaint();
            Assert.That(holds.TakeNext().Type, Is.EqualTo(HoldType.Black));
        }

        [Test]
        public void CancelPaintRequest_PreventsPaint()
        {
            var holds = NewHolds();
            holds.RequestPaint();
            holds.CancelPaintRequest();
            holds.AddHold(HoldType.Red);
            Assert.That(holds.TypeAt(0), Is.EqualTo(HoldType.Red));
        }
    }
}
#nullable disable
