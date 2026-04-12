#nullable enable
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// AnimatorのExtension
    /// アニメ完了をトリガーにしたゲーム処理に最適
    /// </summary>
    public static class AnimatorExtension
    {
        public static bool IsEnd( this Animator? animator )
        {
            if( animator == null )
            {
                return true;
            }
            
            var state = animator.GetCurrentAnimatorStateInfo( 0 );
            return state.normalizedTime >= 1.0f;
        }
        
        public static void PlayNullable( this Animator? animator, string animKey, int layer = -1, float normalizeTime = 0f )
        {
            PlayNullable( animator, Animator.StringToHash( animKey ), layer, normalizeTime );
        }
        
        public static void PlayNullable( this Animator? animator, int hash, int layer = -1, float normalizeTime = 0f )
        {
            if( animator != null )
            {
                animator.Play( hash, layer, normalizeTime );
            }
        }
        
        /// <summary>
        /// 再生待ち(UniTask)
        /// </summary>
        public static async UniTask PlayAsyncTask( this Animator? animator, string animKey, int layer = -1, float normalizeTime = 0f )
        {
            await PlayAsyncTask( animator, Animator.StringToHash( animKey ), layer, normalizeTime );
        }
        
        public static async UniTask PlayAsyncTask( this Animator? animator, int hash, int layer = -1, float normalizeTime = 0f )
        {
            if( animator == null )
            {
                return;
            }
            
            animator.Play( hash, layer, normalizeTime );
            await UniTask.Yield();
            
            while( true )
            {
                var info = animator.GetCurrentAnimatorStateInfo( 0 );
                if( info.normalizedTime >= 1.0f )
                {
                    break;
                }
                
                await UniTask.Yield();
            }
        }
        
        /// <summary>
        /// 再生待ち(Coroutine)
        /// </summary>
        public static IEnumerator   PlayAsyncEnumerator( this Animator animator, string animKey, int layer = -1, float normalizeTime = 0f )
        {
            yield return PlayAsyncEnumerator( animator, Animator.StringToHash( animKey ), layer, normalizeTime );
        }
        
        public static IEnumerator   PlayAsyncEnumerator( this Animator animator, int hash, int layer = -1, float normalizeTime = 0f )
        {
            animator.Play( hash, layer, normalizeTime );
            yield return null;
            
            while( true )
            {
                var info = animator.GetCurrentAnimatorStateInfo( 0 );
                if( info.normalizedTime >= 1.0f )
                {
                    break;
                }
                
                yield return null;
            }
        }
    }
}

#nullable disable
