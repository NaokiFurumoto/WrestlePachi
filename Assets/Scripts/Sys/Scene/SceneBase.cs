#nullable enable
using System.Collections;
using System.Collections.Generic;
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
        protected   Transform?      m_ResidentUIParent  = null;
        
        [SerializeField]
        protected   ViewBase?       m_View      = null;

        [SerializeField]
        protected   ViewBase?[]     m_ResidentViews = new ViewBase?[0];
        
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
            
            var residentViews = GetResidentViews();
            for( int i = 0; i < residentViews.Count; ++i )
            {
                await OpenResidentView( residentViews[i], i );
            }
        }

        private List<ViewBase> GetResidentViews()
        {
            var views = new List<ViewBase>();
            AddResidentView( views, m_View );

            if( m_ResidentUIParent != null )
            {
                var childViews = m_ResidentUIParent.GetComponentsInChildren<ViewBase>( true );
                for( int i = 0; i < childViews.Length; ++i )
                {
                    AddResidentView( views, childViews[i] );
                }
            }

            for( int i = 0; m_ResidentViews != null && i < m_ResidentViews.Length; ++i )
            {
                AddResidentView( views, m_ResidentViews[i] );
            }

            return views;
        }

        private static void AddResidentView( List<ViewBase> views, ViewBase? view )
        {
            if( view == null || views.Contains( view ) )
            {
                return;
            }

            views.Add( view );
        }

        private async UniTask OpenResidentView( ViewBase? view, int layer )
        {
            if( view == null || m_ViewMng == null )
            {
                return;
            }

            view.SetSortingOrder( layer );
            await view.Initialize( GetResidentViewData( view ) );
            await view.OpenAsync();
            m_ViewMng.SetPlacedView( view );
        }

        protected virtual ViewBase.ViewData? GetResidentViewData( ViewBase view )
        {
            return null;
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
            var residentViews = GetResidentViews();
            for( int i = 0; i < residentViews.Count; ++i )
            {
                residentViews[i].Release();
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
