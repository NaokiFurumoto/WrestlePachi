#nullable enable
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// シーン管理
    /// </summary>
    public class SceneManager : BehaviourSingleton<SceneManager>
    {
        #region メンバ
        private     SceneBase?          m_CurrentScene      = null;
        private     bool                m_IsBusy            = false;
        #endregion メンバ
        
        #region プロパティ
        
        public SceneBase?       CurrentScene        => m_CurrentScene;
        public bool             IsBusy
        {
            get
            {
                if( m_IsBusy )
                {
                    return true;
                }

                if( ViewManager.IsBusySystem() )
                {
                    return true;
                }
                
                if( m_CurrentScene != null )
                {
                    return ViewManager.IsBusy();
                }
                
                return false;
            }
        }
        
        #endregion プロパティ
        
        /// <summary>
        /// インスタンスのセット
        /// </summary>
        protected override void _SetInstance()
        {
            s_Instance = this;
        }
        
        protected override void _OnAwake()
        {
            m_CurrentScene = _FindSceneBase();
            if( m_CurrentScene != null )
            {
                //StartCoroutine( m_CurrentScene.Intialize( null ) );
                m_CurrentScene.Intialize(null).Forget();
            }
        }

        /// <summary>
        /// シーン遷移
        /// </summary>
        public void TransitScene( string sceneName, SceneData? data = null )
        {
            if( IsBusy )
            {
                return;
            }

            StartCoroutine( _TransitScene( sceneName, data ) );
        }

        private IEnumerator _TransitScene( string sceneName, SceneData? data = null )
        {
            m_IsBusy = true;
            
            // フェードON
            SceneFade.Instance.FadeIn();

            while( SceneFade.Instance.IsProcessing )
            {
                yield return null;
            }
            
            if( m_CurrentScene != null )
            {
                m_CurrentScene.Release();
            }
            m_CurrentScene = null;
            
            // シーン遷移
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync( sceneName );
            
            // シーン遷移後初期化
            m_CurrentScene = _FindSceneBase();
            if( m_CurrentScene != null )
            {
                yield return m_CurrentScene.Intialize( data );
            }
            
            yield return null;
            
            // フェードOFF
            SceneFade.Instance.FadeOut();
            while( SceneFade.Instance.IsProcessing )
            {
                yield return null;
            }
            
            if( m_CurrentScene != null )
            {
                m_CurrentScene.EndSceneFadeOut();
            }
            
            m_IsBusy = false;
        }

        /// <summary>
        /// SceneBase取得
        /// </summary>
        private SceneBase? _FindSceneBase()
        {
            return GameObject.FindFirstObjectByType<SceneBase>();
        }
    }
}
#nullable disable
