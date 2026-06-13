using System.Threading;
using App.Skills;
using Cysharp.Threading.Tasks;
using GameSys;
using UnityEngine;

namespace App
{
    /// <summary>
    /// GameMainController の保留・パチンコ系処理。
    /// へそ入賞・ポケット入賞・保留抽選・スキル発動・天撃入力を担う。
    /// </summary>
    public sealed partial class GameMainController
    {
        // ─── パチンコゾーン入賞 ──────────────────────────────────

        private void OnHesoEntered()
        {
            ScreenEffectController.PlayHeso();
            if (_isSimulatorMode) return;
            _holdSystem.AddHold(SelectHoldType());
        }

        private void OnPocketEntered()
        {
            if (_isSimulatorMode) return;
            _contents.BallLauncher.LaunchAsync(1, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// シミュレーターモードを切り替える。
        /// ON にするとぷよが停止し、へそ入賞でも保留が追加されなくなる。
        /// </summary>
        public void SetSimulatorMode(bool active)
        {
            _isSimulatorMode = active;
            if (active) _contents.PuyoBoard.Suspend();
        }

        // ─── 保留・スキル発動 ────────────────────────────────────

        /// <summary>
        /// HoldSystem から await される非同期ハンドラ。
        /// 返した UniTask が完了するまで HoldSystem は次の保留へ進まない。
        /// </summary>
        private UniTask OnTechActivatedAsync(HoldType holdType)
        {
            // PlayingState 以外（連鎖中・玉発射中・スキル実行中）はストックに積んで即完了
            if (_state is not PlayingState)
            {
                if (!_skillStockSystem.TryAddStock(holdType))
                    Debug.LogWarning($"[GameMainController] ストックが満杯のため HoldType={holdType} を破棄");
                return UniTask.CompletedTask;
            }
            return _techSkillManager.StartSkillAsync(holdType, _ctx, _skillStockSystem, destroyCancellationToken);
        }

        /// <summary>保留追加時のハンドラ。虹保留ならバイブレーションループを開始する。</summary>
        private void OnHoldSystemAdded(int index, HoldType holdType)
        {
            if (holdType != HoldType.Rainbow) return;
            _ctx.RainbowVibrationCts?.Cancel();
            _ctx.RainbowVibrationCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            VibrationLoopAsync(_ctx.RainbowVibrationCts.Token).Forget();
        }

        /// <summary>ストックMAX到達時：全ストック消費して盤面を全消しする。</summary>
        private async UniTaskVoid OnSkillStockMaxAsync(CancellationToken ct)
        {
            _skillStockSystem.ConsumeAll();
            await _ctx.Contents.PuyoBoard.ClearAllPuyosAsync(ct);
        }

        // ─── 天撃入力 ────────────────────────────────────────────

        /// <summary>天撃ボタン入力：虹PUSH待ち中は TCS を完了させる。それ以外はストック発動。</summary>
        public void OnInputTengeki()
        {
            if (_ctx.RainbowInputSource != null)
            {
                _ctx.RainbowInputSource.TrySetResult();
                return;
            }
            if (_state is not PlayingState) return;
            if (_skillStockSystem.TryConsumeStock(out var holdType))
                _techSkillManager.StartSkillAsync(holdType, _ctx, _skillStockSystem, destroyCancellationToken).Forget();
        }

        // ─── 保留抽選 ────────────────────────────────────────────

        /// <summary>へそ入賞時の保留種別を抽選する。</summary>
        private HoldType SelectHoldType()
        {
            var denom = _config.RainbowProbabilityDenominator;
            if (denom > 0 && Random.Range(0, denom) == 0)
                return HoldType.Rainbow;

            if (Random.value < _config.BlackHoldProbability)
                return HoldType.Black;

            return (HoldType)Random.Range(0, _config.ColorVariant);
        }

        // ─── バイブレーション ────────────────────────────────────

        private static async UniTaskVoid VibrationLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
#if UNITY_IOS || UNITY_ANDROID
                Handheld.Vibrate();
#endif
                await UniTask.Delay(500, cancellationToken: ct).SuppressCancellationThrow();
            }
        }
    }
}
