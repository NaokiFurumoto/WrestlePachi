#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App
{
    /// <summary>
    /// 敵スキル「時計」の演出（HUD 上）。
    /// いたずらネズミが画面の左外から走ってきてタイマーの時計アイコンの上に乗り、数字に横からかじりつく。
    /// 1回かじるごとに onBite が呼ばれ（呼び出し側が残り時間を1秒削る）、数字が赤く光って揺れ、欠片が飛ぶ。
    /// かじり終えると「-N」が浮かび、ネズミは向きを変えて左へ走り去る。Prefab は使わず実行時に作る。
    /// 一時停止中は止まるよう、時間は通常の（timeScale の効く）時間で進める。
    /// </summary>
    public static class ClockMouseEffect
    {
        /// <summary>ネズミの絵の中の前歯の位置（左下0・右上1の割合）。ここを数字の左端に当てる</summary>
        private static readonly Vector2 TeethPoint = new(0.79f, 0.32f);
        /// <summary>ネズミの大きさ（タイマー枠の高さに対する割合）</summary>
        private const float MouseSizeByTimerHeight = 1.05f;
        /// <summary>走ってくる・去る時間と、かじる間隔（秒）</summary>
        private const float RunInTime  = 0.35f;
        private const float RunOutTime = 0.4f;
        private const float BiteInterval = 0.2f;
        /// <summary>代わりの絵（ネズミの絵が未設定のとき）の色</summary>
        private static readonly Color FallbackColor = new(0.6f, 0.6f, 0.68f);
        private static readonly Color BiteFlashColor = new(1f, 0.25f, 0.2f);

        /// <summary>前歯（絵の中の teethPoint）が target に重なるときの、ネズミの中心位置</summary>
        public static Vector2 MouseCenterFor(Vector2 target, Vector2 size, Vector2 teethPoint)
            => target - Vector2.Scale(teethPoint - new Vector2(0.5f, 0.5f), size);

        /// <summary>
        /// ネズミを走らせてタイマーを bites 回かじらせる。onBite はかじるたびに呼ぶ。全部終わったら完了。
        /// </summary>
        public static async UniTask PlayAsync(TMP_Text timerText, Sprite? sprite, int bites, Action onBite, CancellationToken ct)
        {
            var parent = timerText.rectTransform.parent as RectTransform;
            if (parent == null)
            {
                for (var i = 0; i < bites; i++) onBite();
                return;
            }

            // かじる場所：数字の左端の高さの真ん中（タイマー枠の座標で）
            timerText.ForceMeshUpdate();
            var bounds    = timerText.textBounds;
            var biteLocal = (Vector2)parent.InverseTransformPoint(
                timerText.rectTransform.TransformPoint(new Vector3(bounds.min.x, bounds.center.y, 0f)));

            var height = parent.rect.height * MouseSizeByTimerHeight;
            var aspect = sprite != null ? sprite.rect.width / sprite.rect.height : 1f;
            var size   = new Vector2(height * aspect, height);
            var rest   = MouseCenterFor(biteLocal, size, TeethPoint);

            var go = new GameObject("ClockMouse", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.SetAsLastSibling();
            rt.sizeDelta = size;
            rt.pivot     = new Vector2(0.5f, 0f); // 足元を軸に傾ける・弾ませる
            var restFoot = rest - new Vector2(0f, size.y * 0.5f);
            var image    = go.GetComponent<Image>();
            image.sprite        = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (sprite == null) image.color = FallbackColor;

            try
            {
                // 左の画面外から、ぴょんぴょん跳ねながら走ってくる
                var start = restFoot + new Vector2(-parent.rect.width * 1.6f, 0f);
                rt.localPosition = new Vector3(start.x, start.y, 0f);
                await WaitTween(rt.DOLocalJump(restFoot, size.y * 0.25f, 2, RunInTime).SetEase(Ease.Linear)
                        .SetLink(go), ct);

                // かじる：体を前へ傾けて突き出し、戻る。当たった瞬間に1秒削る
                for (var i = 0; i < bites; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var lunge = DOTween.Sequence()
                        .Append(rt.DOLocalMove(restFoot + new Vector2(size.x * 0.06f, -size.y * 0.02f), BiteInterval * 0.4f).SetEase(Ease.InQuad))
                        .Join(rt.DOLocalRotate(new Vector3(0f, 0f, -10f), BiteInterval * 0.4f))
                        .Join(rt.DOScale(new Vector3(1.06f, 0.94f, 1f), BiteInterval * 0.4f))
                        .SetLink(go);
                    await WaitTween(lunge, ct);

                    onBite();
                    PunchDigits(timerText);
                    SpawnCrumbs(parent, biteLocal, size.y);

                    var back = DOTween.Sequence()
                        .Append(rt.DOLocalMove(restFoot, BiteInterval * 0.6f).SetEase(Ease.OutQuad))
                        .Join(rt.DOLocalRotate(Vector3.zero, BiteInterval * 0.6f))
                        .Join(rt.DOScale(Vector3.one, BiteInterval * 0.6f))
                        .SetLink(go);
                    await WaitTween(back, ct);
                }

                ShowMinus(timerText, bites);

                // 満足して向きを変え、左へ走り去る
                rt.localScale = new Vector3(-1f, 1f, 1f);
                await UniTask.Delay(120, cancellationToken: ct);
                var exitPos = restFoot + new Vector2(-parent.rect.width * 1.6f, 0f);
                await WaitTween(rt.DOLocalJump(exitPos, size.y * 0.25f, 2, RunOutTime).SetEase(Ease.Linear)
                        .SetLink(go), ct);
            }
            finally
            {
                if (go != null) UnityEngine.Object.Destroy(go);
                if (timerText != null)
                {
                    timerText.rectTransform.DOKill(complete: true);
                    timerText.DOKill(complete: true);
                }
            }
        }

        /// <summary>tween が終わる（または消される）まで待つ。一時停止中は待ち続ける</summary>
        private static UniTask WaitTween(Tween tween, CancellationToken ct)
            => UniTask.WaitWhile(() => tween.IsActive() && !tween.IsComplete(), cancellationToken: ct);

        /// <summary>かじられた数字が赤く光ってぶるっと揺れる</summary>
        private static void PunchDigits(TMP_Text timerText)
        {
            var rt = timerText.rectTransform;
            rt.DOKill(complete: true);
            timerText.DOKill(complete: true);
            rt.DOPunchScale(Vector3.one * 0.25f, 0.18f, 8, 0.5f).SetLink(timerText.gameObject);
            rt.DOShakeAnchorPos(0.18f, 6f, 20).SetLink(timerText.gameObject);
            var baseColor = timerText.color;
            timerText.color = BiteFlashColor;
            timerText.DOColor(baseColor, 0.25f).SetLink(timerText.gameObject);
        }

        /// <summary>かじった場所から小さな欠片が3つ飛び散る</summary>
        private static void SpawnCrumbs(RectTransform parent, Vector2 at, float mouseHeight)
        {
            for (var i = 0; i < 3; i++)
            {
                var go = new GameObject("ClockCrumb", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                rt.SetAsLastSibling();
                var s = mouseHeight * UnityEngine.Random.Range(0.05f, 0.08f);
                rt.sizeDelta     = new Vector2(s, s);
                rt.localPosition = at;
                rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 90f));
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = i == 0 ? new Color(1f, 0.85f, 0.3f) : new Color(0.95f, 0.95f, 0.95f);

                var to = at + new Vector2(UnityEngine.Random.Range(-0.1f, 0.35f), UnityEngine.Random.Range(-0.45f, -0.2f)) * mouseHeight;
                DOTween.Sequence()
                    .Append(rt.DOLocalJump(to, mouseHeight * 0.2f, 1, 0.4f))
                    .Join(rt.DOLocalRotate(new Vector3(0f, 0f, 360f), 0.4f, RotateMode.FastBeyond360))
                    .Insert(0.2f, img.DOFade(0f, 0.2f))
                    .OnComplete(() => UnityEngine.Object.Destroy(go))
                    .SetLink(go);
            }
        }

        /// <summary>数字の上に赤い「-N」を弾ませて浮かべ、消す</summary>
        private static void ShowMinus(TMP_Text timerText, int seconds)
        {
            var popup = UnityEngine.Object.Instantiate(timerText, timerText.transform.parent);
            popup.name                 = "ClockMinusPopup";
            popup.text                 = $"-{seconds}";
            popup.raycastTarget        = false;
            popup.enableVertexGradient = false;
            popup.color                = BiteFlashColor;
            popup.alignment            = TextAlignmentOptions.Center;

            var rt = popup.rectTransform;
            // 下へ浮かぶので、下にある敵HPの枠の裏に隠れないよう、画面のいちばん手前（ルートのキャンバス直下）へ移す
            var root = timerText.canvas != null ? timerText.canvas.rootCanvas.transform : null;
            if (root != null)
            {
                rt.SetParent(root, true);
                rt.SetAsLastSibling();
            }
            // 移した先では元の親の縮小（0.7倍など）が localScale に入るので、大きさと動く距離はそれを基準にする
            var k     = rt.localScale.x;
            var step  = rt.rect.height * k;
            var start = rt.anchoredPosition + new Vector2(0f, -step * 0.2f);
            rt.anchoredPosition = start;
            rt.localScale       = Vector3.one * (0.5f * k);
            DOTween.Sequence()
                .Append(rt.DOScale(2.0f * k, 0.12f).SetEase(Ease.OutBack))
                .Append(rt.DOScale(1.6f * k, 0.08f))
                .Join(rt.DOAnchorPos(start + new Vector2(0f, -step * 0.9f), 0.8f).SetEase(Ease.OutCubic))
                .Insert(0.55f, popup.DOFade(0f, 0.3f))
                .OnComplete(() => UnityEngine.Object.Destroy(popup.gameObject))
                .SetLink(popup.gameObject);
        }
    }
}
#nullable disable
