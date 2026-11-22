#nullable enable
using App.Puyo;
using NUnit.Framework;

namespace App.Tests
{
    /// <summary>
    /// 敵スキル「お邪魔落下」が狙う列の選び方と、積める数の上限の検証。
    /// 予告されたら即ゲームオーバー、にならないよう、どの列も上2段（ぷよが出てくる列は上3段）を空ける。
    /// </summary>
    public sealed class ColumnOjamaTests
    {
        private const int VisibleRows = 12;
        private const int SpawnCol    = PuyoBoard.SpawnColumn; // 左から3列目

        // 積める数：ふつうの列は上2段、出てくる列は上3段を残した高さまで
        [Test]
        public void Capacity_LeavesTwoRows_SpawnColumnLeavesThree()
        {
            Assert.That(PuyoBoard.ColumnOjamaCapacity(4, 0, VisibleRows), Is.EqualTo(6));        // 12-2-4
            Assert.That(PuyoBoard.ColumnOjamaCapacity(4, SpawnCol, VisibleRows), Is.EqualTo(5)); // 12-3-4
            Assert.That(PuyoBoard.ColumnOjamaCapacity(10, 0, VisibleRows), Is.EqualTo(0));
            Assert.That(PuyoBoard.ColumnOjamaCapacity(12, 0, VisibleRows), Is.EqualTo(0));       // 上限を超えていても0
        }

        // いちばん高く積まれている列を狙う
        [Test]
        public void PickTallest_ReturnsHighestColumn()
            => Assert.That(PuyoBoard.PickTallestColumn(new[] { 1, 4, 2, 0, 3, 1 }, new[] { 9, 6, 8, 10, 7, 9 }, 0.5f), Is.EqualTo(1));

        // 同じ高さが並んだら、乱数でどれか1つを選ぶ（0〜1を候補数で区切る）
        [Test]
        public void PickTallest_TieUsesRandom()
        {
            var heights = new[] { 3, 0, 3, 0, 3, 0 };
            var caps    = new[] { 7, 10, 6, 10, 7, 10 };
            Assert.That(PuyoBoard.PickTallestColumn(heights, caps, 0.0f),  Is.EqualTo(0));
            Assert.That(PuyoBoard.PickTallestColumn(heights, caps, 0.5f),  Is.EqualTo(2));
            Assert.That(PuyoBoard.PickTallestColumn(heights, caps, 0.99f), Is.EqualTo(4));
        }

        // 1個も積めない列は狙わず、次に高い列を狙う。どこにも積めなければ -1
        [Test]
        public void PickTallest_SkipsColumnsWithNoCapacity()
        {
            Assert.That(PuyoBoard.PickTallestColumn(new[] { 10, 5, 9, 2, 0, 0 }, new[] { 0, 5, 0, 8, 10, 10 }, 0.5f), Is.EqualTo(1));
            Assert.That(PuyoBoard.PickTallestColumn(new[] { 10, 10, 9, 10, 10, 10 }, new[] { 0, 0, 0, 0, 0, 0 }, 0.5f), Is.EqualTo(-1));
        }
    }
}
#nullable disable
