#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Text;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// デバッグログ
    /// 画面左上ダブルタップで開く
    /// </summary>
    #if !DEBUG_BUILD
    public class DebugLog
    #else
    public class DebugLog : MonoBehaviour
    #endif
    {
        #if DEBUG_BUILD
        
        private class Info
        {
            public enum TYPE
            {
                NONE,
                LOG,
                WARNING,
                ERROR,
                
                Length,
            }
            
            private static readonly     Color[]     TYPE_COLOR_TBL  = new Color[(int)TYPE.Length]
            {
                Color.white,
                Color.white,
                Color.yellow,
                Color.red,
            };
            
            private static  readonly    GUILayoutOption     BUTTON_WIDTH_OPT        = GUILayout.Width( 60 );
            private static  readonly    GUILayoutOption     DETAIL_BUTTON_WIDTH_OPT = GUILayout.Width( 60 );
            private static  readonly    GUILayoutOption     LOG_HEIGHT_OPT          = GUILayout.Height( 32 );
            
            private static readonly     GUILayoutOption[]   LOG_COUNT_OPT   = new GUILayoutOption[]
            {
                GUILayout.Width( 30 ),
                LOG_HEIGHT_OPT
            };
            
            public      TYPE        type        = TYPE.NONE;
            public      string      log         = "";
            public      string      org         = "";
            public      string      trace       = "";
            public      int         count       = 0;

            public void Clear()
            {
                type    = TYPE.NONE;
                log     = "";
                org     = "";
                trace   = "";
                count   = 0;
            }

            /// <summary>
            /// 描画
            /// </summary>
            public bool DrawGUI()
            {
                bool isTapDetail = false;
                using( var hScope = new GUILayout.HorizontalScope() )
                {
                    if( GUILayout.Button( "クリア", BUTTON_WIDTH_OPT ) )
                    {
                        Clear();
                    }
                
                    if( GUILayout.Button( "コピー", BUTTON_WIDTH_OPT ) )
                    {
                        GUIUtility.systemCopyBuffer = org;
                    }
                    
                    GUIStyle lblStyle = GUI.skin.label;
                    Color preColor = lblStyle.normal.textColor;

                    lblStyle.normal.textColor = TYPE_COLOR_TBL[(int)type];
                    GUILayout.Label( log, LOG_HEIGHT_OPT );
                    lblStyle.normal.textColor = preColor;
                
                    GUILayout.Label( ZString.Format( "({0})", count ), LOG_COUNT_OPT );
                    if( GUILayout.Button( "詳細", DETAIL_BUTTON_WIDTH_OPT ) )
                    {
                        isTapDetail = true;
                    }
                }
                
                return isTapDetail;
            }

            /// <summary>
            /// 詳細描画
            /// </summary>
            public void DrawGUIDetail( bool isTrace )
            {
                string str = org;
                if( isTrace )
                {
                    if( string.IsNullOrEmpty( trace ) == false )
                    {
                        str = trace;
                    }
                    else
                    {
                        str = "";
                    }
                }
                
                GUILayout.Label( str, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
            }
        }

        private class CustomGUIStyleScope : System.IDisposable
        {
            private     int defaultLabelFontSize;
            private     int defaultButtonFontSize;

            public CustomGUIStyleScope()
            {
                defaultLabelFontSize = GUI.skin.label.fontSize;
                GUI.skin.label.fontSize = 22;
                
                defaultLabelFontSize = GUI.skin.button.fontSize;
                GUI.skin.button.fontSize = 16;
            }

            public void Dispose()
            {
                GUI.skin.label.fontSize = defaultLabelFontSize;
                GUI.skin.button.fontSize = defaultLabelFontSize;
            }
        }
        
        private enum TabType {
            All = 0,
            Log,
            Warning,
            Error,
            
            Length
        }
        
        private readonly string[] TAB_NAME = new string[(int)TabType.Length]{
            "ALL",
            "ログ",
            "警告",
            "エラー",
        };

        const   float           DOUBLE_TAP_TIME     = 0.4f;
        const   int             BUFFER_SIZE         = 512;

        private readonly        GUILayoutOption[]   LIST_BUTTON_OPTION  = new GUILayoutOption[]
        {
            GUILayout.Width( 100 ),
            GUILayout.Height( 32 ),
        };
        
        private static readonly     GUILayoutOption DETAIL_HEIGHT_OPT   = GUILayout.Height( 300 );
        
        private     Info[]      m_InfoTbl           = new Info[BUFFER_SIZE];
        private     int         m_HeadIdx           = 0;
        private     int         m_TailIdx           = 0;
        private     Vector2     m_ScrollPos         = Vector2.zero;
        private     Rect        m_WindowRect;
        private     int         m_DrawNum           = 0;
        private     TabType     m_SelectTab         = TabType.All;
        private     Info?       m_DetailInfo        = null;
        private     Vector2     m_DetailScrollPos   = Vector2.zero;
        private     bool        m_IsTrace           = false;
        private     Vector3     m_Scale             = Vector3.one;
        private     int         m_WindowId          = 0;
        private     List<Info>  m_DrawInfos         = new List<Info>( BUFFER_SIZE / 2 );
        
        // タップ判定
        private     float       m_Elapse        = 0.0f;
        private     Rect        m_TapRange      = Rect.zero;
        private     Vector2     m_TapPos        = Vector2.zero;
        private     bool        m_IsPreTapped   = false;
        private     bool        m_IsTapped      = false;
        
        private     bool        m_IsOpened      = false;

        private     static  DebugLog?   s_Instance      = null;
        
        public      static  bool    IsOpened
        {
            get
            {
                if( s_Instance != null )
                {
                    return s_Instance.m_IsOpened;
                }
                return false;
            }
        }

        public static void Create( GameObject gobj )
        {
            if( s_Instance == null )
            {
                s_Instance = gobj.AddComponent<DebugLog>();
            }
        }
        
        private void Awake()
        {
            m_TapRange = new Rect( 0, 0, Screen.width * 0.3f, Screen.height * 0.3f );

            for( int i = 0; i < m_InfoTbl.Length; ++i )
            {
                m_InfoTbl[i] = new Info();
            }
            
            m_HeadIdx   =
            m_TailIdx   = 0;
            m_WindowRect = new Rect( 30, 30, 1100, 600 );
            Application.logMessageReceived += _HandleLogMessage;
        }

        private void Update()
        {
            if( m_IsOpened == false )
            {
                if( CheckDoubleTap() )
                {
                    m_IsOpened = true;
                }
            }
        }

        #region ログ
        
        /// <summary>
        /// ログ出力
        /// DEBUG_BUILDのelse以下にRELEASE_BUILD用の宣言があるので追加する
        /// </summary>
        public static void Log( string log )
        {
            if( s_Instance != null )
            {
                s_Instance._SetUnityLog( log, Info.TYPE.LOG );
            }
        }

        public static void Log( object message )
        {
            Log( message.ToString() );
        }

        public static void LogWarning( string log )
        {
            if( s_Instance != null )
            {
                s_Instance._SetUnityLog( log, Info.TYPE.WARNING );
            }
        }

        public static void LogError( string log )
        {
            if( s_Instance != null )
            {
                s_Instance._SetUnityLog( log, Info.TYPE.ERROR );
            }
        }

        public static void LogException( Exception exception )
        {
            if( s_Instance != null )
            {
                s_Instance._SetUnityLog( exception.ToString(), Info.TYPE.ERROR );
            }
        }
        
        private void _HandleLogMessage(string log, string trace, LogType type)
        {
            if( string.IsNullOrEmpty( log ) )
            {
                return;
            }

            var infoType = Info.TYPE.LOG;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    infoType = Info.TYPE.ERROR;
                    break;
                case LogType.Warning:
                    infoType = Info.TYPE.WARNING;
                    break;

            }
            _AddLog( log, trace, infoType, false );
        }

        private void _SetUnityLog( string log, Info.TYPE type )
        {
            switch( type )
            {
                case Info.TYPE.WARNING:
                    {
                        Debug.LogWarning( log );
                    }
                    break;
                
                case Info.TYPE.ERROR:
                    {
                        Debug.LogError( log );
                    }
                    break;
                
                default:
                    {
                        Debug.Log( log );
                    }
                    break;
            }
        }

        /// <summary>
        /// ログ追加
        /// </summary>
        private void _AddLog( string log, string trace, Info.TYPE type, bool unityLog = true )
        {
            if( m_InfoTbl[0] == null )
            {
                return;
            }
            
            string[]    addTexts    = log.Split( '\n' );
            string[]?   addTraces   = null;
            if( string.IsNullOrEmpty( trace ) == false )
            {
                addTraces = trace.Split( '\n' );
            }
            
            var lastInfo = GetLastLog();
            if( lastInfo != null )
            {
                var isMatch = false;
                if( log.Length == lastInfo.log.Length )
                {
                    isMatch = lastInfo.org == log;
                }

                if( isMatch )
                {
                    if( lastInfo.trace != null && ( ( trace != null ) ? trace.Length == lastInfo.trace.Length : false ) )
                    {
                        isMatch = lastInfo.trace == trace;
                    }
                }

                if( isMatch )
                {
                    ++lastInfo.count;
                    return;
                }
            }
            
            var addInfo = m_InfoTbl[m_TailIdx];
            {
                addInfo.type = type;
                addInfo.count = 1;
                addInfo.org = log;
                addInfo.log = log;
                addInfo.trace = trace != null ? trace : "";
            }
            
            ++m_TailIdx;
            if( m_TailIdx >= BUFFER_SIZE )
            {
                m_TailIdx = 0;
            }

            if( m_HeadIdx == m_TailIdx )
            {
                ++m_HeadIdx;
                if( m_HeadIdx >= BUFFER_SIZE )
                {
                    m_HeadIdx = 0;
                }
            }
            
            m_InfoTbl[m_TailIdx].type = Info.TYPE.NONE;
        }
        
        //--------------------------
        // 最終ログを取得
        //--------------------------
        private Info? GetLastLog()
        {
            int index = m_TailIdx - 1;
            if( index < 0 )
            {
                index = m_InfoTbl.Length - 1;
            }

            if( m_InfoTbl[index].type == Info.TYPE.NONE )
            {
                return null;
            }
            
            return m_InfoTbl[index];
        }

        /// <summary>
        /// クリア
        /// </summary>
        private void Clear()
        {
            m_HeadIdx =
            m_TailIdx = 0;
            m_DetailInfo = null;
            for( int i = 0; i < m_InfoTbl.Length; ++i )
            {
                m_InfoTbl[i].Clear();
            }
        }
        
        #endregion ログ
        
        #region GUI描画

        public void OnGUI()
        {
            if( m_IsOpened == false )
            {
                return;
            }
            
            _AutoResizeWindow();
            _DrawGUI();
        }

        private void _AutoResizeWindow()
        {
            var scl = GameScreen.UiCenterScale;
            if( scl == 0.0f )
            {
                return;
            }
            
            m_Scale.x =
            m_Scale.y = scl;
            GUI.matrix = Matrix4x4.TRS( Vector3.zero, Quaternion.identity, m_Scale );
        }

        private void _DrawGUI()
        {
            m_WindowRect = GUILayout.Window( m_WindowId, m_WindowRect, _DrawWindow, "DebugLog");
        }

        private void _DrawWindow( int id  )
        {
            using var customScope = new CustomGUIStyleScope();
            
            GUILayout.Label( "" );
            
            var rect = new Rect( 0, 20, m_WindowRect.width, m_WindowRect.height - 20 );
            GUILayout.BeginArea( rect );
            {
                // 1段目
                using( new GUILayout.HorizontalScope( "box" ) )
                {
                    var select = (TabType)GUILayout.SelectionGrid( (int)m_SelectTab, TAB_NAME, (int)TabType.Length, GUILayout.ExpandWidth( true ) );
                    if( m_SelectTab != select )
                    {
                        m_SelectTab = select;
                    }
                }
                
                // ログ表示
                using( var scroll = new GUILayout.ScrollViewScope( m_ScrollPos ) )
                {
                    _GetDrawLogList( m_SelectTab );
                    m_ScrollPos = scroll.scrollPosition;

                    for( int i = 0; i < m_DrawInfos.Count; ++i )
                    {
                        if( m_DrawInfos[i].DrawGUI() )
                        {
                            m_DetailInfo = m_DrawInfos[i];
                        }
                    }

                    if( m_DrawInfos.Count > m_DrawNum )
                    {
                        m_DrawNum = m_DrawInfos.Count;
                        m_ScrollPos.y = Mathf.Infinity;
                    }
                }
                
                // ログ詳細描画
                if( m_DetailInfo != null )
                {
                    using( var bg = new GUILayout.VerticalScope( "box", DETAIL_HEIGHT_OPT ) )
                    {
                        using( new GUILayout.HorizontalScope( "box" ) )
                        {
                            if( GUILayout.Button( m_IsTrace ? "ログ" : "履歴" ) )
                            {
                                m_IsTrace = !m_IsTrace;
                            }
                        
                            if( GUILayout.Button( "詳細を閉じる" ) )
                            {
                                m_DetailInfo = null;
                            }
                        }
                        
                        using( var scroll = new GUILayout.ScrollViewScope( m_DetailScrollPos, GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) ) )
                        {
                            m_DetailScrollPos = scroll.scrollPosition;
                            m_DetailInfo?.DrawGUIDetail( m_IsTrace );
                        }
                    }
                }
                
                // 下部ボタン
                using( new GUILayout.HorizontalScope() )
                {
                    GUILayout.FlexibleSpace();

                    if( GUILayout.Button( "クリア", LIST_BUTTON_OPTION ) )
                    {
                        m_DetailInfo = null;
                        Clear();
                    }
                    
                    if( GUILayout.Button( "閉じる", LIST_BUTTON_OPTION ) )
                    {
                        m_SelectTab = TabType.All;
                        m_IsOpened = false;
                    }
                }
                
                GUILayout.Space( 5 );
            }
            GUILayout.EndArea();
            
            GUI.DragWindow();
        }

        private void _GetDrawLogList( TabType tabType )
        {
            m_DrawInfos.Clear();
            
            int idx = m_HeadIdx;
            while( true )
            {
                var info = m_InfoTbl.SafeGetAtNullable( idx++ );
                if( info == null )
                {
                    break;
                }

                if( info.type == Info.TYPE.NONE )
                {
                    continue;
                }
                    
                var isAdd = false;
                switch( tabType )
                {
                    case TabType.All:
                        isAdd = true;
                        break;
                        
                    case TabType.Log:
                        isAdd = info.type == Info.TYPE.LOG;
                        break;
                        
                    case TabType.Warning:
                        isAdd = info.type == Info.TYPE.WARNING;
                        break;
                        
                    case TabType.Error:
                        isAdd = info.type == Info.TYPE.ERROR;
                        break;
                }

                if( isAdd )
                {
                    m_DrawInfos.Add( info );
                }
            }
        }

        #endregion GUI描画
        
        #region タップ判定
        
        private bool CheckDoubleTap()
        {
            var isTapped = _IsTapped();
            
            if( m_IsTapped == false )
            {
                if( isTapped )
                {
                    m_IsTapped = true;
                    m_Elapse = 0f;
                }
            }
            else
            {
                m_Elapse += Time.deltaTime;
                if( m_Elapse < ( DOUBLE_TAP_TIME * Time.timeScale ) )
                {
                    if( isTapped )
                    {
                        m_IsTapped = false;
                        m_Elapse = 0f;

                        if( m_TapPos.x < m_TapRange.width && ( GameScreen.Height - m_TapPos.y ) < m_TapRange.height )
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    m_IsTapped = false;
                    m_Elapse = 0f;
                }
            }
            
            return false;
        }
        
        private bool _IsTapped()
        {
            bool result = false;
            
            #if UNITY_EDITOR || UNITY_STANDALONE_WIN
            var isTap = Input.GetMouseButton( 0 );
            m_TapPos = Input.mousePosition;
#else
            var isTap = Input.touchCount > 0;
            if (isTap){
                m_TapPos = Input.GetTouch( 0 ).position;
            }
#endif
            result = m_IsPreTapped == false && isTap;
            m_IsPreTapped = isTap;
            
            return result;
        }
        
        #endregion タップ判定
        
        #else //DEBUG_BUILD
        #region ログ

        public static void Log( string log ){}
        public static void LogWarning( string log ) {}
        public static void LogError( string log ) {}

        #endregion ログ
        #endif //DEBUG_BUILD
        
        /// <summary>
        /// デバッグログを任意で追加（File/Func/Line自動付与バージョン）
        /// </summary>
        static public void LogWarningCaller( string log,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "" )
        {
            LogWarning( LogCaller( log, filePath, lineNumber, memberName ) );
        }

        static public void LogErrorCaller( string log,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "" )
        {
            LogError( LogCaller( log, filePath, lineNumber, memberName ) );
        }

        static public void LogNullError(
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "" )
        {
            LogError( LogCaller( "Error! null...", filePath, lineNumber, memberName ) );
        }

        //--------------------------
        //    File/Func/Line付のログ生成
        //--------------------------
        static private string LogCaller(string log,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string memberName = "")
        {
            using var sb = ZString.CreateStringBuilder();
            var slash = filePath.LastIndexOf('\\');
            var file = string.Empty;
            if( slash > 0 )
            {
                ++slash;
                file = filePath.Substring( slash, filePath.Length - slash );
            }
            sb.AppendFormat( "{0}({1})::{2}(): {3}", file, lineNumber, memberName, log );

            return sb.ToString();
        }
    }
}
#nullable disable
