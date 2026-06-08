using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Skills
{
    /// <summary>
    /// SkillCutInView 上のちびキャラアニメーションを制御するコンポーネント。
    /// AnimationEvent（OnAttackFinished）でアニメーション完了を通知する。
    /// </summary>
    public sealed class ChibiCharacter : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        private UniTaskCompletionSource _attackTcs;

        /// <summary>スキルごとの AnimatorController に差し替える。</summary>
        /// <summary>スキルごとの AnimatorController に差し替える。</summary>
        public void SetController(RuntimeAnimatorController controller)
        {
            _animator.runtimeAnimatorController = controller;
        }

        /// <summary>
        /// 攻撃アニメーションを再生し、OnAttackFinished イベントで完了を検知する。
        /// フォールバックとして最大3秒待機。
        /// </summary>
        public async UniTask PlayAttackAsync(CancellationToken ct)
        {
            _attackTcs = new UniTaskCompletionSource();
            _animator.SetTrigger("Attack");
            await UniTask.WhenAny(
                _attackTcs.Task,
                UniTask.Delay(3000, cancellationToken: ct)
            );
        }

        /// <summary>Animation の最終フレームに設定する AnimationEvent。</summary>
        public void OnAttackFinished() => _attackTcs?.TrySetResult();
    }
}
