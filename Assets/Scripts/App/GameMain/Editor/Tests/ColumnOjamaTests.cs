#nullable enable
using App.Puyo;
using NUnit.Framework;

namespace App.Tests
{
    /// <summary>
    /// 敵スキル「お邪魔落下」が狙う列の選び方の検証。
    /// </summary>
    public sealed class ColumnOjamaTests
    {
        // いちばん高く積まれている列を狙う
        [Test]
        public void PickTallest_ReturnsHighestColumn()
            => Assert.That(PuyoBoard.PickTallestColumn(new[] { 1, 4, 2, 0, 3, 1 }, 12, 0.5f), Is.EqualTo(1));

        // 同じ高さが並んだら、乱数でどれか1つを選ぶ（0〜1を候補数で区切る）
        [Test]
        public void PickTallest_TieUsesRandom()
        {
            var heights = new[] { 3, 0, 3, 0, 3, 0 };
            Assert.That(PuyoBoard.PickTallestColumn(heights, 12, 0.0f),  Is.EqualTo(0));
            Assert.That(PuyoBoard.PickTallestColumn(heights, 12, 0.5f),  Is.EqualTo(2));
            Assert.That(PuyoBoard.PickTallestColumn(heights, 12, 0.99f), Is.EqualTo(4));
        }

        // 満杯の列は積めないので狙わない。全部満杯なら -1
        [Test]
        public void PickTallest_SkipsFullColumns()
        {
            Assert.That(PuyoBoard.PickTallestColumn(new[] { 12, 5, 12, 2, 0, 0 }, 12, 0.5f), Is.EqualTo(1));
            Assert.That(PuyoBoard.PickTallestColumn(new[] { 12, 12, 12, 12, 12, 12 }, 12, 0.5f), Is.EqualTo(-1));
        }
    }
}
#nullable disable
