#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameSys
{
    /// <summary>
    /// シーン遷移用フェード
    /// </summary>
    public class SceneFade : BehaviourSingleton<SceneFade>
    {
        private readonly int        FADEIN_KEY_HASH     = Animator.StringToHash( "FadeIn" ); 
        private readonly int        FADEOUT_KEY_HASH    = Animator.StringToHash( "FadeOut" ); 

        
        [SerializeField]
        private     RawImage?           m_Image     = null;
        
        [SerializeField]
        private     Animator?           m_Animator  = null;
        
        [SerializeField]
        private     Canvas?             m_Canvas    = null;
        
        private     bool                m_IsProcessing      = false;
        
        public  bool        IsProcessing        =>      m_IsProcessing;
        
        /// <summary>
        /// インスタンスのセット
        /// </summary>
        protected override void _SetInstance()
        {
            s_Instance = this;
        }

        protected override void _OnAwake()
        {
            if( m_Canvas != null )
            {
                m_Canvas.sortingOrder = SortOrderConst.SORTING_ORDER_SCENE_FADE;
            }
        }

        /// <summary>
        /// フェードイン
        /// </summary>
        public void FadeIn()
        {
            FadeIn( Color.black );
        }
        
        public void FadeIn( Color color )
        {
            if( m_IsProcessing )
            {
                return;
            }
            
            if( m_Image != null )
            {
                m_Image.color = color;
                m_Image.gameObject.SetActive( true );
            }
            
            StartCoroutine( _Fade( FADEIN_KEY_HASH ) );
        }

        /// <summary>
        /// フェードアウト
        /// </summary>
        public void FadeOut()
        {
            if( m_IsProcessing )
            {
                return;
            }
            
            StartCoroutine( _Fade( FADEOUT_KEY_HASH, true ) );
        }

        private IEnumerator _Fade( int hash, bool isImgOff = false )
        {
            m_IsProcessing = true;

            if( m_Animator != null )
            {
                yield return m_Animator.PlayAsyncEnumerator( hash, 0 );
            }

            if( isImgOff )
            {
                if( m_Image != null )
                {
                    m_Image.gameObject.SetActive( false );
                }
            }
            
            m_IsProcessing = false;
        }
    }
}
#nullable disable
