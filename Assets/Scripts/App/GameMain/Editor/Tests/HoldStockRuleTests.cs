#nullable enable
using System.Threading;
using NUnit.Framework;

namespace App.Tests
{
    /// <summary>
    /// 天撃ボタンのストックと保留の出し方の決まり（2026-11-25 ユーザー決定）。
    /// ・保留が満杯（4個）のときにへそに入った分は、天撃ボタンにストックする（1個・新しいほうで上書き）。
    ///   ただし黒保留はストックせず、その場で降らせる。
    /// ・保留の番が来たとき、連鎖中など技を割り込ませられない・技を出せないときは、ストックせず出せるまで待つ。
    ///   ゲームオーバー・ゲームクリアになったら、その保留は出さずに捨てる（敵の撃破演出中は次のステージまで待つ）。
    ///   黒保留は待たずに降らせる。
    /// </summary>
    public sealed class HoldStockRuleTests
    {
        private static HoldSystem NewHolds()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            return new HoldSystem(cts.Token);
        }

        // 満杯のときの入賞は、捨てずに知らせる（保留の並びは変わらない）
        [Test]
        public void AddHold_WhenFull_RaisesOverflow()
        {
            var holds = NewHolds();
            HoldType? overflow = null;
            holds.OnHoldOverflow += t => overflow = t;
            for (var i = 0; i < HoldSystem.MaxHolds; i++) holds.AddHold(HoldType.Red);
            Assert.That(overflow, Is.Null, "4個目までは普通に並ぶ");

            holds.AddHold(HoldType.Blue);

            Assert.That(overflow, Is.EqualTo(HoldType.Blue));
            Assert.That(holds.HoldCount, Is.EqualTo(HoldSystem.MaxHolds));
            Assert.That(holds.TypeAt(3), Is.EqualTo(HoldType.Red));
        }

        // あふれた保留：普通の技と虹はストック、黒はその場で降らせる
        [Test]
        public void Overflow_StocksExceptBlack()
        {
            Assert.That(GameMainController.ShouldStockOverflow(HoldType.Red),     Is.True);
            Assert.That(GameMainController.ShouldStockOverflow(HoldType.Rainbow), Is.True);
            Assert.That(GameMainController.ShouldStockOverflow(HoldType.Black),   Is.False);
        }

        // 番が来た保留：出せるなら出す、連鎖中・出せないなら待つ、ゲームが終わったら捨てる
        [Test]
        public void Activation_WaitsUntilItCanFire()
        {
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.Playing,        true),  Is.EqualTo(GameMainController.HoldActivation.Fire));
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.Clearing,       true),  Is.EqualTo(GameMainController.HoldActivation.Wait));
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.SkillExecuting, true),  Is.EqualTo(GameMainController.HoldActivation.Wait));
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.Paused,         true),  Is.EqualTo(GameMainController.HoldActivation.Wait));
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.Playing,        false), Is.EqualTo(GameMainController.HoldActivation.Wait));
        }

        [Test]
        public void Activation_DropsWhenTheStageIsOver()
        {
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.GameOver,   true), Is.EqualTo(GameMainController.HoldActivation.Drop));
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.GameClear,  true), Is.EqualTo(GameMainController.HoldActivation.Drop));
            Assert.That(GameMainController.DecideActivation(HoldType.Red, GamePhase.EnemyDying, true), Is.EqualTo(GameMainController.HoldActivation.Wait), "撃破演出の後は次のステージで出す");
        }

        // 黒保留は連鎖中でも待たずに降らせる（降るのは次にペアが着地したとき）
        [Test]
        public void BlackHold_FiresWithoutWaiting()
        {
            Assert.That(GameMainController.DecideActivation(HoldType.Black, GamePhase.Clearing, true), Is.EqualTo(GameMainController.HoldActivation.Fire));
            Assert.That(GameMainController.DecideActivation(HoldType.Black, GamePhase.GameOver, true), Is.EqualTo(GameMainController.HoldActivation.Drop));
        }
    }
}
#nullable disable
