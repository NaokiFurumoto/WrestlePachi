#nullable enable
using GameSys;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
#endif // UNITY_EDITOR

/// <summary>
/// ボタンイベント処理
/// 機能としては最小減とし、Holdなどは必要になったときに検討する
/// </summary>
public class ButtonEvent : EventTrigger
{
    public  const   string  BACK_EVENT_KEY      = "_BACK";      // Andrdoidバックイベント専用のキー
    private const   float   _DRAG_DISTANCE      = 10f;          // ドラッグと認識する距離
    private const   float   DRAG_DISTANCE_SQR   = _DRAG_DISTANCE * _DRAG_DISTANCE;    // 平方根
    
    /// <summary>
    /// イベント情報
    /// </summary>
    public class EventInfo
    {
        private     string          m_EventKey      = string.Empty;
        private     Component?      m_RefComponent  = null;
        private     Vector2         m_TouchPos      = Vector2.zero;
        
        public      string          EventKey        => m_EventKey;
        public      Component?      RefComponent    => m_RefComponent;
        public      ref Vector2     TouchPos        => ref m_TouchPos;

        /// <summary>
        /// イベントデータセット
        /// </summary>
        public void Set( string key, Component? comp = null )
        {
            m_EventKey = key;
            m_RefComponent = comp;
        }

        public void SetBackKey()
        {
            Set( BACK_EVENT_KEY );
        }

        public void SetTouchPos( float posX, float posY )
        {
            m_TouchPos.x = posX;
            m_TouchPos.y = posY;
        }
    }
    
    #region メンバ
    
    // NOTE ここは、上のEventInfoにまとめる
    [SerializeField]
    private     string          m_EventKey      = string.Empty;
    
    [SerializeField]
    private     Component?      m_RefComponent  = null;
    
    [SerializeField]
    private     GameObject?     m_SyncDragEvent = null;
    
    [SerializeField]
    private     string          m_CommonSEKey   = string.Empty;
    
    private     ViewBase?       m_ParentView    = null;
    
    private     Selectable?     m_Selectable    = null;
    
    private     IBeginDragHandler?                  m_BeginDragHandler                  = null;
    private     IDragHandler?                       m_DragHandler                       = null;
    private     IEndDragHandler?                    m_EndDragHandler                    = null;
    private     IInitializePotentialDragHandler?    m_InitializePotentialDragHandler    = null;
    private     IScrollHandler?                     m_ScrollHandler                     = null;
    
    private     EventInfo                           m_EvInfo                            = new ();
    private     Vector2                             m_StartPos                          = Vector2.zero;
    
    // フラグ
    private     bool                                m_IsDragged                         = false;
    #endregion メンバ

    #region プロパティ

    private     bool        IsInteractable
    {
        get => ( m_Selectable != null ) ? m_Selectable.IsInteractable() : true; 
    }
    
    #endregion プロパティ

    private void Awake()
    {
        m_EvInfo.Set( m_EventKey, m_RefComponent );
        
        m_ParentView = GetComponentInParent<ViewBase>();
        
        m_Selectable = GetComponent<Button>();
        if( m_Selectable == null )
        {
            m_Selectable = GetComponent<Toggle>();
        }
    }
    
    private void _SendEvent()
    {
        if( m_ParentView == null )
        {
            return;
        }
        
        // イベント送信するかチェック
        if( CheckSendEvent() == false )
        {
            return;
        }
        
        if( IsInteractable == false )
        {
            return;
        }
        
        // SE指定があれば、SE再生
        if( string.IsNullOrEmpty( m_CommonSEKey ) == false )
        {
            //SoundManager.PlayCommonSE( m_CommonSEKey );TODO
        }
        
        m_ParentView.ReceiveButtonEvent( m_EvInfo );
    }
    
    /// <summary>
    /// イベント送信するかチェック
    /// </summary>
    public static bool CheckSendEvent()
    {
        // シーン遷移,View遷移中は処理しない
        if( SceneManager.Instance.IsBusy )
        {
            return false;
        }
        
        if( ViewManager.IsBusy() || ViewManager.IsBusySystem() )
        {
            return false;
        }
        
        // 通信中は処理しないTODO
        //if( NetworkManager.IsBusy )
        //{
        //    return false;
        //}
        
        #if DEBUG_BUILD
        if( DebugLog.IsOpened )
        {
            return false;
        }
        #endif // DEBUG_BUILD
        
        return true;
    }
    
    #region Selectorイベント
    
    /// <summary>
    /// 押された時(1度のみ)
    /// </summary>
    public override void OnPointerDown( PointerEventData eventData )
    {
    }
    
    /// <summary>
    /// 離された時(1度のみ)
    /// </summary>
    public override void OnPointerUp( PointerEventData eventData )
    {
    }
    
    /// <summary>
    /// クリックされたとき 
    /// </summary>
    public override void OnPointerClick( PointerEventData eventData )
    {
        // ドラッグが発生していたら処理しない
        if( m_IsDragged )
        {
            return;
        }
        
        m_EvInfo.SetTouchPos( eventData.position.x, eventData.position.y );
        
        _SendEvent();
    }
    
    /// <summary>
    /// ドラッグ開始時(1度)
    /// </summary>
    public override void OnBeginDrag( PointerEventData eventData )
    {
        m_StartPos = eventData.position;
        m_IsDragged = false;
        
        if( m_BeginDragHandler == null )
        {
            m_BeginDragHandler = _GetDragEvent<IBeginDragHandler>();
        }
        
        if( m_BeginDragHandler != null )
        {
            m_BeginDragHandler.OnBeginDrag( eventData );
        }
    }
    
    /// <summary>
    /// ドラッグ中
    /// </summary>
    public override void OnDrag( PointerEventData eventData )
    {
        if( m_IsDragged == false )
        {
            var sqrDist = ( eventData.position - m_StartPos ).sqrMagnitude;
            if( sqrDist > DRAG_DISTANCE_SQR )
            {
                m_IsDragged = true;
            }
        }
        
        if( m_DragHandler == null )
        {
            m_DragHandler = _GetDragEvent<IDragHandler>();
        }
        
        if( m_DragHandler != null )
        {
            m_DragHandler.OnDrag( eventData );
        }
    }
    
    /// <summary>
    /// ドラッグ終了時(1度)
    /// </summary>
    public override void OnEndDrag( PointerEventData eventData )
    {
        m_IsDragged = false;
        
        if( m_EndDragHandler == null )
        {
            m_EndDragHandler = _GetDragEvent<IEndDragHandler>();
        }
        
        if( m_EndDragHandler != null )
        {
            m_EndDragHandler.OnEndDrag( eventData );
        }
    }
    
    /// <summary>
    /// ドラッグ対象が見つかった時
    /// </summary>
    public override void OnInitializePotentialDrag( PointerEventData eventData )
    {
        if( m_InitializePotentialDragHandler == null )
        {
            m_InitializePotentialDragHandler = _GetDragEvent<IInitializePotentialDragHandler>();
        }
        
        if( m_InitializePotentialDragHandler != null )
        {
            m_InitializePotentialDragHandler.OnInitializePotentialDrag( eventData );
        }
    }
    
    /// <summary>
    /// スクロールのイベント発生時
    /// </summary>
    public override void OnScroll( PointerEventData eventData )
    {
        if( m_ScrollHandler == null )
        {
            m_ScrollHandler = _GetDragEvent<IScrollHandler>();
        }
        
        if( m_ScrollHandler != null )
        {
            m_ScrollHandler.OnScroll( eventData );
        }
    }

    #endregion Selectorイベント
    
    /// <summary>
    /// リレー先のハンドラ取得
    /// </summary>
    private T? _GetDragEvent<T>() where T : class, IEventSystemHandler
    {
        if( m_SyncDragEvent == null )
        {
            return null;
        }

        var components = m_SyncDragEvent.GetComponents<Component>();
        for( int i = 0; i < components.Length; ++i )
        {
            var comp = components[i];
            if( comp == null )
            {
                continue;
            }

            // ButtonEvent 自身をリレー先にすると再帰呼び出しになるので除外する
            if( ReferenceEquals( comp, this ) || comp is ButtonEvent )
            {
                continue;
            }

            if( comp is T handler )
            {
                return handler;
            }
        }

        return null;
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(ButtonEvent), true)]
    public class Editor_ButtonEvent : Editor
    {
        const       string              SE_NONE             = "(なし)";
        
        private     GameObject?         m_TmpGameObject     = null;
        private     Component[]         m_Components        = new Component[0];
        private     string[]            m_ComponentNames    = new string[0];
        private     int                 m_SelectedIdx       = -1;
        private     string[]            m_CommonSEKeyTbl    = new string[0];
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var tmp = target as ButtonEvent;
            if( tmp == null )
            {
                return;
            }

            if( m_CommonSEKeyTbl.Length == 0 )
            {
                //_UpdateCommonSEKeyTable();
            }
            
            using( var change = new EditorGUI.ChangeCheckScope() )
            {
                tmp.m_EventKey = EditorGUILayout.TextField( "キー:", tmp.m_EventKey );

                if( m_TmpGameObject == null && tmp.m_RefComponent != null )
                {
                    m_TmpGameObject = tmp.m_RefComponent.gameObject;
                    _UpdatePopupTable( m_TmpGameObject );
                    for( int i = 0; i < m_Components.Length; ++i )
                    {
                        if( m_Components[i] == tmp.m_RefComponent )
                        {
                            m_SelectedIdx = i;
                            break;
                        }
                    }
                }

                {
                    var gobj = EditorGUILayout.ObjectField( "参照先GameObject:", m_TmpGameObject, typeof( GameObject ), true ) as GameObject;
                    if( gobj != m_TmpGameObject )
                    {
                        m_TmpGameObject = gobj;

                        _UpdatePopupTable( m_TmpGameObject );
                    }

                    if( m_Components.Length > 0 )
                    {
                        var idx = EditorGUILayout.Popup( m_SelectedIdx, m_ComponentNames );
                        if( m_SelectedIdx != idx )
                        {
                            m_SelectedIdx = idx;
                            tmp.m_RefComponent = m_Components[ m_SelectedIdx ];
                        }
                    }
                    else
                    {
                        GUI.contentColor = Color.yellow;
                        EditorGUILayout.LabelField( "対象のGameObjectをドラッグしてください" );
                        GUI.contentColor = Color.white;
                    }
                }
                
                tmp.m_SyncDragEvent = EditorGUILayout.ObjectField( "DRAGイベントリレー:", tmp.m_SyncDragEvent, typeof(GameObject), true ) as GameObject;

                EditorGUILayout.Space( 5 );
                
                EditorGUILayout.LabelField( "共通SEキー:" );
                tmp.m_CommonSEKey = EditorExtention.DrawStringArrayPopup( tmp.m_CommonSEKey, m_CommonSEKeyTbl );
                if( tmp.m_CommonSEKey == SE_NONE )
                {
                    tmp.m_CommonSEKey = string.Empty;
                }
                
                EditorGUILayout.Space( 10 );
                
                // ============================================================================
                // 以下確認用の表示
                // ============================================================================
                GUI.enabled = false;
                tmp.m_RefComponent = EditorGUILayout.ObjectField( "参照Component:", tmp.m_RefComponent, typeof(Component), true ) as Component;
                EditorGUILayout.ObjectField( "View:", tmp.m_ParentView, typeof(ViewBase), true );
                {
                    EditorGUILayout.LabelField( $"共通SEキー: {tmp.m_CommonSEKey}" );
                }
                GUI.enabled = true;
                // ============================================================================

                if( change.changed )
                {
                    EditorUtility.SetDirty( target );
                }
            }
        }
        
        private void _UpdatePopupTable( GameObject? gobj )
        {
            if( gobj != null )
            {
                m_Components = gobj.GetComponents<Component>();
                int size = m_Components.Length;
                m_ComponentNames = new string[size];
                
                for( int i = 0; i < size; ++i )
                {
                    m_ComponentNames[i] = m_Components[i].GetType().ToString();
                }
            }
            else
            {
                m_Components = new Component[0];
                m_ComponentNames = new string[0];
                m_SelectedIdx = -1;
            }
        }

        // private void _UpdateCommonSEKeyTable()
        // {
        //     List<string> tmp = new List<string>();
        //     tmp.Add( SE_NONE );
            
        //     var fields = typeof( GApp.SoundConst.SECommon ).GetFields( BindingFlags.Public | BindingFlags.Static );
        //     for( int i = 0; i < fields.Length; ++i )
        //     {
        //         var field = fields[i];
        //         if( field.IsLiteral == false )
        //         {
        //             continue;
        //         }

        //         if( field.FieldType != typeof( string ) )
        //         {
        //             continue;
        //         }
                
        //         tmp.Add( (string)field.GetRawConstantValue() );
        //     }
            
        //     m_CommonSEKeyTbl = tmp.ToArray();
        // }
    }
    #endif
}
#nullable disable
