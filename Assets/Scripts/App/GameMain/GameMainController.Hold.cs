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
            AppSound.PlayHesoIn();
            ScreenEffectController.PlayHeso();
            if (_isSimulatorMode) return;
            _holdSystem.AddHold(SelectHoldType());
        }

        private void OnPocketEntered()
        {
            if (_isSimulatorMode) return;
            _contents.BallLauncher.LaunchAsync(1).Forget();
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
            bool willStock = _state is not PlayingState || !_techSkillManager.CanExecute(holdType, _ctx);
            FireHoldBeam(holdType, willStock);

            if (_state is not PlayingState)
            {
                _skillStockSystem.SetStock(holdType);
                return UniTask.CompletedTask;
            }
            return _techSkillManager.StartSkillAsync(holdType, _ctx, _skillStockSystem, destroyCancellationToken);
        }

        private void FireHoldBeam(HoldType holdType, bool willStock)
        {
            if (_holdBeamEffect == null || _contents.HoldDisplay == null) return;

            var from = _contents.HoldDisplay.ConsumeOriginPosition;
            var to   = willStock
                ? _tengekiButton?.transform.position ?? _contents.PuyoBoard.transform.position
                : _contents.PuyoBoard.transform.position;

            _holdBeamEffect.PlayAsync(from, to, HoldTypeToBeamColor(holdType), destroyCancellationToken).Forget();
        }

        private static Color HoldTypeToBeamColor(HoldType holdType) => holdType switch
        {
            HoldType.Red     => new Color(1f,  0.2f, 0.2f),
            HoldType.Yellow  => new Color(1f,  0.9f, 0.1f),
            HoldType.Green   => new Color(0.2f,1f,   0.3f),
            HoldType.Blue    => new Color(0.2f,0.5f, 1f),
            HoldType.Purple  => new Color(0.7f,0.2f, 1f),
            HoldType.Black   => new Color(0.4f,0.4f, 0.4f),
            HoldType.Rainbow => Color.white,
            _                => Color.white,
        };

        /// <summary>保留追加時のハンドラ。虹保留ならバイブレーションループを開始する。</summary>
        private void OnHoldSystemAdded(int index, HoldType holdType)
        {
            AppSound.PlayHoldAdd();
            if (holdType != HoldType.Rainbow) return;
            _ctx.RainbowVibrationCts?.Cancel();
            _ctx.RainbowVibrationCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            var ct = _ctx.RainbowVibrationCts.Token;
            VibrationLoopAsync(ct).Forget();
            _tengekiButton?.StartShakeAsync(ct).Forget();
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

            // ぷよがなくてスキル不発の場合も TechSkillManager 内でストックに戻る
            if (_skillStockSystem.TryConsumeStock(out var holdType))
                _techSkillManager.StartSkillAsync(holdType, _ctx, _skillStockSystem, destroyCancellationToken).Forget();
        }

        // ─── 保留抽選 ────────────────────────────────────────────

        /// <summary>へそ入賞時の保留種別を抽選する。</summary>
        private HoldType SelectHoldType()
        {
            var stage = _ctx.CurrentStage;

            var denom = stage?.RainbowDenominator ?? _config.RainbowProbabilityDenominator;
            if (denom > 0 && Random.Range(0, denom) == 0)
                return HoldType.Rainbow;

            var blackDenom = stage?.BlackHoldDenominator ?? (int)(1f / Mathf.Max(_config.BlackHoldProbability, 0.0001f));
            if (blackDenom > 0 && Random.Range(0, blackDenom) == 0)
                return HoldType.Black;

            return stage != null
                ? SelectColorByWeight(stage)
                : (HoldType)Random.Range(0, _config.ColorVariant);
        }

        /// <summary>各色の重みに応じて保留色を抽選する。</summary>
        private static HoldType SelectColorByWeight(StageConfig stage)
        {
            int count = stage.ColorCount;
            int[] weights = { stage.WeightRed, stage.WeightYellow, stage.WeightGreen, stage.WeightBlue, stage.WeightPurple };

            int total = 0;
            for (int i = 0; i < count; i++) total += weights[i];
            if (total <= 0) return (HoldType)Random.Range(0, count);

            int r = Random.Range(0, total);
            int cumulative = 0;
            for (int i = 0; i < count; i++)
            {
                cumulative += weights[i];
                if (r < cumulative) return (HoldType)i;
            }
            return HoldType.Red;
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
