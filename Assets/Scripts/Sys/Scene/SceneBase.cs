#nullable enable
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace  GameSys
{
    /// <summary>
    /// シーン遷移時に受け渡すデータ
    /// 継承してデータを定義する
    /// </summary>
    public class SceneData
    {
    }
    
    /// <summary>
    /// Scene基底
    /// </summary>
    public class SceneBase : MonoBehaviour
    {
        [SerializeField]
        protected   Transform?      m_UIParent  = null;
        
        [SerializeField]
        protected   ViewBase?       m_View      = null;
        
        protected   SceneData?      m_Data      = null;
        
        private     ViewManager?    m_ViewMng   = null;

        #region プロパティ
        
        public      ViewManager?    ViewMng     => m_ViewMng;
        
        #endregion プロパティ

        /// <summary>
        /// 初期化
        /// </summary>
        public async UniTask Intialize( SceneData? data )
        {
            m_Data = data;

            GameScreen.CalcScreenSize();
            m_ViewMng = new ViewManager();
            m_ViewMng.Initialize( m_UIParent );
            
            await OnInitialize();
            
            if( m_View != null )
            {
                await m_View.Initialize();
                await m_View.OpenAsync();
                m_ViewMng.SetPlacedView( m_View );
            }
        }

        //===========================================
        // 初期化時処理
        //===========================================
        protected virtual async UniTask OnInitialize()
        {
            await UniTask.Yield();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public void Release()
        {
            if( m_View != null )
            {
                m_View.Release();
            }
            
            OnRelease();

            if( m_ViewMng != null )
            {
                m_ViewMng.Release();
                m_ViewMng = null;
            }
        }

        //===========================================
        // 破棄時処理
        //===========================================
        protected virtual void OnRelease()
        {
        
        }

        /// <summary>
        /// フェード明け
        /// </summary>
        public void EndSceneFadeOut()
        {
            _OnEndSceneFadeOut();
        }
        
        //===========================================
        // フェード明け時処理
        //===========================================
        protected virtual void _OnEndSceneFadeOut()
        {
        
        }
    }
}
#nullable disable
