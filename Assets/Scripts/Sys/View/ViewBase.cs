#nullable enable
using System.Collections;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// View基底（DOTween前提・アニメは派生側に任せる）
    /// </summary>
    [RequireComponent(typeof(ViewCanvasOrder))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ViewBase : MonoBehaviour
    {
        /// <summary>
        /// View用データ
        /// </summary>
        public class ViewData
        {
        }

        enum OpenPhase
        {
            Opening,
            Opened,
            Closing,
            Closed,
        }
        
        [SerializeField]
        private     CanvasGroup?        m_CanvasGrp     = null;
        
        [SerializeField]
        private     ViewCanvasOrder?    m_CanvasOrder   = null;

        [SerializeField] private ViewAnimation? _viewAnim;

        private     bool                m_IsSystemView  = false;
        private     ViewData?           m_Data          = null;
        
        private     OpenPhase           m_OpenPhase     = OpenPhase.Closed;
        
        private     int                 m_LayerId       = 0;                        // 表示優先のためのID

        #region プロパティ

        public bool IsBusy => IsOpening || IsClosing;
        public bool IsOpening => m_OpenPhase == OpenPhase.Opening;
        public bool IsOpened => m_OpenPhase == OpenPhase.Opened;
        public bool IsClosing => m_OpenPhase == OpenPhase.Closing;
        public bool IsClosed => m_OpenPhase == OpenPhase.Closed;
        public int LayerId => m_LayerId;
        protected ViewData? Data => m_Data;

        #endregion プロパティ

        /// <summary>
        /// 生成時
        /// </summary>
        private void Awake()
        {
            if (m_CanvasGrp == null)
                m_CanvasGrp = GetComponent<CanvasGroup>();

            if (m_CanvasGrp != null)
            {
                m_CanvasGrp.alpha = 0f;
                m_CanvasGrp.interactable = false;
                m_CanvasGrp.blocksRaycasts = false;
            }

            if(_viewAnim == null)
            {
                _viewAnim = GetComponent<ViewAnimation>();
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public async UniTask Initialize( ViewData? data = null )
        {
            m_Data = data;
            
            OnInitialize();
            await OnInitializeAsync();
        }

        public void SetSystemView()
        {
            m_IsSystemView = true;
        }

        //===========================================
        // 初期化時処理
        //===========================================
        protected virtual void OnInitialize()
        {
        }
        
        protected virtual async UniTask OnInitializeAsync()
        {
            await UniTask.Yield();
        }

        //===========================================
        // Openアニメ
        //===========================================
        public async UniTask OpenAsync()
        {
            if (IsBusy || IsOpened)
                return;

            m_OpenPhase = OpenPhase.Opening;

            gameObject.SetActive(true);

            if (m_CanvasGrp != null)
            {
                m_CanvasGrp.alpha = 0f;
                m_CanvasGrp.interactable = false;
                m_CanvasGrp.blocksRaycasts = false;
            }

            await OnOpenAsync(); // ★ 派生側でアニメを書く

            if (m_CanvasGrp != null)
            {
                m_CanvasGrp.interactable = true;
                m_CanvasGrp.blocksRaycasts = true;
            }

            m_OpenPhase = OpenPhase.Opened;
        }

        protected virtual async UniTask OnOpenAsync()
        {
            if (_viewAnim != null)
            {
                await _viewAnim.PlayOpenAsync();
            }
            else
            {
                // アニメーション設定がない場合のデフォルト挙動
                if (m_CanvasGrp != null) m_CanvasGrp.alpha = 1f;
                await UniTask.Yield();
            }
        }

        //===========================================
        // Closeアニメ
        //===========================================
        public async UniTask CloseAsync()
        {
            if (IsBusy || IsClosed)
                return;

            m_OpenPhase = OpenPhase.Closing;

            if (m_CanvasGrp != null)
            {
                m_CanvasGrp.interactable = false;
                m_CanvasGrp.blocksRaycasts = false;
            }

            await OnCloseAsync(); // ★ 派生側でアニメを書く

            gameObject.SetActive(false);

            m_OpenPhase = OpenPhase.Closed;
        }

        protected virtual async UniTask OnCloseAsync()
        {
            if (_viewAnim != null)
            {
                await _viewAnim.PlayCloseAsync();
            }
            else
            {
                if (m_CanvasGrp != null) m_CanvasGrp.alpha = 0f;
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public void Release()
        {
            OnRelease();
        }

        //===========================================
        // 破棄時処理
        //===========================================
        protected virtual void OnRelease()
        {
        
        }

        /// <summary>
        /// 自分を破棄
        /// </summary>
        public void Pop()
        {
            if( m_IsSystemView )
            {
                ViewManager.PopSystemView( this );
            }
            else
            {
                ViewManager.PopView( this );
            }
        }
        
        public async UniTask PopAsync()
        {
            if( m_IsSystemView )
            {
                await ViewManager.PopSystemViewAsync( this );
            }
            else
            {
                await ViewManager.PopViewAsync( this );
            }
        }

        /// <summary>
        /// ボタン押下時のイベント受信
        /// </summary>
        public virtual void ReceiveButtonEvent( ButtonEvent.EventInfo btnEv )
        {
            
        }
        
        /// <summary>
        /// SortingOrderセット
        /// </summary>
        public void SetSortingOrder( int layer, bool isSystem = false )
        {
            m_LayerId = layer;
            
            if( m_CanvasOrder != null )
            {
                m_CanvasOrder.SetSortOrder( layer, isSystem );
            }
        }
        
        public void OnWindowResize()
        {
            if( m_CanvasOrder == null )
            {
                return;
            }
            
            m_CanvasOrder.OnWindowResize();
        }
        
        #if UNITY_EDITOR
        public void SetupOnEditor()
        {
            m_CanvasGrp = GetComponent<CanvasGroup>();
            m_CanvasOrder = GetComponent<ViewCanvasOrder>();
        }
        #endif // UNITY_EDITOR
    }
    
    // =========================================================================
    // =========================================================================
    /// <summary>
    /// エディタ
    /// </summary>
    #if UNITY_EDITOR
    [CustomEditor(typeof(ViewBase), true)]
    public class Editor_ViewBase : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var view = target as ViewBase;
            if( GUILayout.Button( "ViewBase セットアップ" ) )
            {
                if( view != null )
                {
                    view.SetupOnEditor();
                    EditorUtility.SetDirty( target );
                }
            }
        }
    }
        
    #endif //UNITY_EDITOR
}
#nullable disable

