#nullable enable
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// システム管理
    /// WebGLビルドは、画面サイズ変更処理を行う
    /// </summary>
    public class SystemManager : BehaviourSingleton<SystemManager>
    {
        [SerializeField]
        private     Transform?      m_UIParent  = null;
        
        private     ViewManager?    m_ViewMng   = null;
        
        #if UNITY_WEBGL
        private     int             m_CacheScreenW  = 0;
        private     bool            m_IsAwaked      = false;
        #endif // UNITY_WEBGL
        
        public  ViewManager     ViewMng
        {
            get
            {
                if( m_ViewMng == null )
                {
                    m_ViewMng = new ViewManager();
                    m_ViewMng.Initialize( m_UIParent, true );
                }
                return m_ViewMng;
            }
        }
        
        /// <summary>
        /// インスタンスのセット
        /// </summary>
        protected override void _SetInstance()
        {
            s_Instance = this;
        }
        
        protected override void _OnAwake()
        {
            GameScreen.CalcScreenSize();
            
            #if DEBUG_BUILD
            var gobj = new GameObject( "DebugLog" );
            gobj.transform.SetParent( transform );
            DebugLog.Create( gobj );
            #endif //DEBUG_BUILD
            
            m_ViewMng = new ViewManager();
            m_ViewMng.Initialize( m_UIParent, true );
            
            #if UNITY_WEBGL
            m_IsAwaked = true;
            m_CacheScreenW = Screen.width;
            #endif // UNITY_WEBGL
        }
        
        #if UNITY_WEBGL
        private void Update()
        {
            if( m_IsAwaked == false )
            {
                return;
            }

            if( m_CacheScreenW != Screen.width )
            {
                _OnWindowResize();
                m_CacheScreenW = Screen.width;
            }
        }
        
        /// <summary>
        /// WebGL版でResize対応が必要
        /// </summary>
        private void _OnWindowResize()
        {
            GameScreen.CalcScreenSize();
            ViewManager.OnWindowResize();
        }
        #endif // UNITY_WEBGL
    }
}
#nullable disable
