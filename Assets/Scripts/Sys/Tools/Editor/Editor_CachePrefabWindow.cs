#nullable enable
using System.Collections.Generic;
using Cysharp.Text;
using UnityEngine;
using UnityEditor;

namespace GameSys
{
    /// <summary>
    /// Prefabの参照を記録するEditorWindow
    /// よく編集するPrefabをProjectビューで探す手間を減らす意図
    /// </summary>
    public class Editor_CachePrefabWindow : EditorWindow
    {
        const   string      EDITOR_PREFS_KEY        = "CachePrefabPath";
        
        private static  readonly    GUILayoutOption     INFO_BUTTON_WIDTH   = GUILayout.Width( 60 );
        
        private class PrefabInfo
        {
            public  string      assetPath       = string.Empty;
            public  string      name            = string.Empty;
            public  GameObject? prefabObj       = null;

            public PrefabInfo( GameObject? prefab )
            {
                prefabObj = prefab;
                if( prefab != null )
                {
                    assetPath = AssetDatabase.GetAssetPath( prefab );
                }
                name      = prefab?.name ?? "None";
            }

            public bool DrawGUI()
            {
                var isDelete = false;
                using var horizontal = new EditorGUILayout.HorizontalScope( "box" );
                {
                    EditorGUILayout.LabelField( name );
                    if( GUILayout.Button( "選択", INFO_BUTTON_WIDTH  ) )
                    {
                        Selection.activeObject = prefabObj;
                    }
                    
                    if( GUILayout.Button( "編集", INFO_BUTTON_WIDTH ) )
                    {
                        AssetDatabase.OpenAsset( prefabObj );
                    }
                    
                    GUI.backgroundColor = Color.red;
                    if( GUILayout.Button( "削除", INFO_BUTTON_WIDTH ) )
                    {
                        isDelete = true;
                    }
                    GUI.backgroundColor = Color.white;
                }
                
                return isDelete;
            }
        }
        
        private     List<PrefabInfo>        m_PrefabInfos   = new ();
        private     UnityEngine.Object?     m_SelectingObj  = null;
        private     Vector2                 m_ScrollPos     = Vector2.zero;
        
        ///---------------------------------------------------
        /// <summary>Window表示</summary>
        ///---------------------------------------------------
        [MenuItem("Daifugo/CachePrefab")]
        public static void CreateWindow()
        {
            var window = GetWindow<Editor_CachePrefabWindow>();
            window.titleContent = new GUIContent("CachePrefab");
        }

        public void OnEnable()
        {
            _LoadInfo();
        }

        /// <summary>
        /// 描画
        /// </summary>
        private void OnGUI()
        {
            Utility.EditorGUI_DrawOpenFileButton();
            EditorGUILayout.Space( 5 );
            
            _CheckSelecting();
            
            if( m_SelectingObj != null && m_SelectingObj is GameObject gobj )
            {
                GUI.enabled = _CheckExistList( gobj ) == false;
                GUI.backgroundColor = Color.green;
                if( GUILayout.Button( "追加する" ) )
                {
                    _AddInfo( gobj );
                    _SaveInfo();
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox( "ProjectビューでPrefabを選択することで記録できます", MessageType.Info );
            }
            
            EditorGUILayout.Space( 10 );
            
            // リスト表示
            _DrawInfoList();
        }
        
        private void Update()
        {
            Repaint();
        }
        
        /// <summary>
        /// 選択切替時処理
        /// </summary>
        private void _CheckSelecting()
        {
            var activeObj = Selection.activeObject;
            
            // 同じならば処理しない
            if( m_SelectingObj == activeObj )
            {
                return;
            }
            
            if( activeObj != null )
            {
                // nullでない場合 Assetかどうかを調べる(Hierarchyのもので反応させない)
                var path = AssetDatabase.GetAssetPath( activeObj );
                if( string.IsNullOrEmpty( path ) )
                {
                    m_SelectingObj = null;
                    return;
                }
                
                // Prefabのみに絞る
                if( activeObj is GameObject == false )
                {
                    m_SelectingObj = null;
                    return;
                }
            }
            
            m_SelectingObj = activeObj;
        }
        
        /// <summary>
        /// リスト表示
        /// </summary>
        private void _DrawInfoList()
        {
            if( m_PrefabInfos.Count == 0 )
            {
                EditorGUILayout.HelpBox( "リストは空です", MessageType.Info );
                return;
            }
            
            var deleteIdx = -1;
            using( var scroll = new EditorGUILayout.ScrollViewScope( m_ScrollPos ) )
            {
                m_ScrollPos = scroll.scrollPosition;
                
                for( int i = 0; i < m_PrefabInfos.Count; ++i )
                {
                    var info = m_PrefabInfos[i];
                    if( info.DrawGUI() )
                    {
                        deleteIdx = i;
                    }
                }
            }

            if( deleteIdx > -1 )
            {
                _RemoveInfo( deleteIdx );
                _SaveInfo();
            }
        }
        
        /// <summary>
        /// リストに追加 
        /// </summary>
        private void _AddInfo( GameObject prefab )
        {
            var info = new PrefabInfo( prefab );
            m_PrefabInfos.Add( info );
        }
        
        private void _RemoveInfo( int idx )
        {
            if( idx < 0 && m_PrefabInfos.Count <= idx )
            {
                return;
            }
            
            m_PrefabInfos.RemoveAt( idx );
        }
        
        /// <summary>
        /// すでに追加されているかチェック
        /// </summary>
        private bool _CheckExistList( GameObject prefab )
        {
            var path = AssetDatabase.GetAssetPath( prefab );
            for( int i = 0; i < m_PrefabInfos.Count; ++i )
            {
                var info = m_PrefabInfos[i];
                if( info.assetPath == path )
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 情報をEditorPlayerPrefsに記録
        /// prefabのpathをカンマ区切りで
        /// </summary>
        private void _SaveInfo()
        {
            if( m_PrefabInfos.Count == 0 )
            {
                if( EditorPrefs.HasKey( EDITOR_PREFS_KEY ) )
                {
                    EditorPrefs.DeleteKey( EDITOR_PREFS_KEY );
                }
                return;
            }
            
            using var strBuf = ZString.CreateStringBuilder();
            {
                for( int i = 0; i < m_PrefabInfos.Count; ++i )
                {
                    var info = m_PrefabInfos[i];
                    strBuf.Append( info.assetPath );

                    if( i < m_PrefabInfos.Count - 1 )
                    {
                        strBuf.Append( ',' );
                    }
                }
                
                EditorPrefs.SetString( EDITOR_PREFS_KEY, strBuf.ToString() );
            }
        }

        private void _LoadInfo()
        {
            if( EditorPrefs.HasKey( EDITOR_PREFS_KEY ) == false )
            {
                return;
            }
            
            string data = EditorPrefs.GetString( EDITOR_PREFS_KEY );
            if( string.IsNullOrEmpty( data ) )
            {
                return;
            }
            
            var paths = data.Split( ',' );
            for( int i = 0; i < paths.Length; ++i )
            {
                var path = paths[i];
                if( string.IsNullOrEmpty( path ) )
                {
                    continue;
                }
                
                var gobj = AssetDatabase.LoadAssetAtPath<GameObject>( path );
                _AddInfo( gobj );
            }
        }
    }
}

#nullable disable