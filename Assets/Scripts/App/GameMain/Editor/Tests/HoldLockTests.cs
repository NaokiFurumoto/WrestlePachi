#nullable enable
using System.Threading;
using NUnit.Framework;

namespace App.Tests
{
    /// <summary>
    /// 敵スキル「スキル封印」：一定間隔で敵が狙い、次に出てきた保留を鎖でロックする。
    /// ロックされた保留は消化のとき不発になる。虹保留と黒保留はロックしない。
    /// </summary>
    public sealed class HoldLockTests
    {
        /// <summary>
        /// 消化シーケンス（時間待ち）が走らないよう、キャンセル済みのトークンで作る。
        /// 保留の追加・ロック・取り出しだけを確かめる。
        /// </summary>
        private static HoldSystem NewHolds()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            return new HoldSystem(cts.Token);
        }

        // 狙ったあと最初に出てきた保留だけがロックされる
        [Test]
        public void RequestLock_LocksOnlyNextHold()
        {
            var holds = NewHolds();
            holds.RequestLock();
            holds.AddHold(HoldType.Red);
            holds.AddHold(HoldType.Blue);
            Assert.That(holds.IsLocked(0), Is.True);
            Assert.That(holds.IsLocked(1), Is.False);
            Assert.That(holds.HasLockRequest, Is.False);
        }

        // 狙っていなければロックしない
        [Test]
        public void NoRequest_NoLock()
        {
            var holds = NewHolds();
            holds.AddHold(HoldType.Red);
            Assert.That(holds.IsLocked(0), Is.False);
        }

        // 虹・黒はロックせず、その次の普通の保留をロックする
        [Test]
        public void RainbowAndBlack_AreSkipped()
        {
            var holds = NewHolds();
            holds.RequestLock();
            holds.AddHold(HoldType.Rainbow);
            holds.AddHold(HoldType.Black);
            holds.AddHold(HoldType.Green);
            Assert.That(holds.IsLocked(0), Is.False);
            Assert.That(holds.IsLocked(1), Is.False);
            Assert.That(holds.IsLocked(2), Is.True);
        }

        // ステージが終わったら狙いは取り消す
        [Test]
        public void CancelLockRequest_PreventsLock()
        {
            var holds = NewHolds();
            holds.RequestLock();
            holds.CancelLockRequest();
            holds.AddHold(HoldType.Red);
            Assert.That(holds.IsLocked(0), Is.False);
        }

        // 消化のとき、ロックされていたかを返し、残りは左へ詰まる（ロックも一緒に動く）
        [Test]
        public void TakeNext_ReportsLock_AndLockShiftsWithHold()
        {
            var holds = NewHolds();
            holds.AddHold(HoldType.Blue);
            holds.RequestLock();
            holds.AddHold(HoldType.Red);

            var first = holds.TakeNext();
            Assert.That(first.Type, Is.EqualTo(HoldType.Blue));
            Assert.That(first.Locked, Is.False);
            Assert.That(holds.IsLocked(0), Is.True, "ロックされた赤が先頭へ詰まる");

            var second = holds.TakeNext();
            Assert.That(second.Type, Is.EqualTo(HoldType.Red));
            Assert.That(second.Locked, Is.True);
            Assert.That(holds.HoldCount, Is.EqualTo(0));
        }
    }
}
#nullable disable
