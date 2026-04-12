#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using GameSys;
using UnityEditor;
#endif

[CreateAssetMenu( menuName = "ScriptableObjects/SoundClump", order = 1)]
public class SoundClump : ScriptableObject
{
    [Serializable]
    public class ClipInfo
    {
        public  string              key     = string.Empty;
        public  AudioClip?          clip    = null;
        public  float               volume  = 1.0f;
        
        #if UNITY_EDITOR

        static  readonly    private Color       COLOR_1                 = new Color( 0f, 1f, 1f );
        static  readonly    private Color       COLOR_2                 = new Color( 0f, 0f, 1f );
        
        public bool DrawGUI( int idx, SerializedProperty? elemProp )
        {
            bool isDelete = false;

            GUI.backgroundColor = ( idx % 2 == 0 ) ? COLOR_1 : COLOR_2;
            using( var h = new EditorGUILayout.HorizontalScope( "box" ) )
            {
                GUI.backgroundColor = Color.white;

                using( var v = new EditorGUILayout.VerticalScope() )
                {
                    var width = GUILayout.Width( 90 );
                    var capW = GUILayout.Width( 70 );

                    using ( var e = new EditorGUILayout.HorizontalScope() )
                    {
                        EditorGUILayout.LabelField( "key", capW );
                        key = EditorGUILayout.TextField( key, width );
                    }
                    
                    using ( var e = new EditorGUILayout.HorizontalScope() )
                    {
                        EditorGUILayout.LabelField( "clip", capW );
                        clip = EditorGUILayout.ObjectField( clip, typeof(AudioClip), false ) as AudioClip;
                    }
                    
                    using ( var e = new EditorGUILayout.HorizontalScope() )
                    {
                        EditorGUILayout.LabelField( "volume", capW );
                        volume = EditorGUILayout.FloatField( volume );
                    }

                    // GUI.backgroundColor = Color.red;
                    // if( GUILayout.Button( "削除" ) )
                    // {
                    //     isDelete = true;
                    // }
                    // GUI.backgroundColor = Color.white;
                }
            }
            
            return isDelete;
        }
        
        #endif // UNITY_EDITOR
    }
    
    [SerializeField]
    private ClipInfo[]      clipInfoTbl     = new ClipInfo[0];

    private Dictionary<string, ClipInfo>    m_Dict      = new Dictionary<string, ClipInfo>();
    
    public  Dictionary<string, ClipInfo>    Dict        => m_Dict;
    
    /// <summary>
    /// Dictionary作成
    /// </summary>
    public void CreateDict()
    {
        for( int i = 0; i < clipInfoTbl.Length; ++i )
        {
            var info = clipInfoTbl[i];
            m_Dict.Add( info.key, info );
        }
    }

    public void ReleaseDict()
    {
        m_Dict.Clear();
    }

    #region エディタ

    //
    #if UNITY_EDITOR

    [CustomEditor(typeof( SoundClump ) )]
    public class Editor_SoundClump : Editor
    {
        private SoundClump?         m_Target                = null;
        
        private DefaultAsset?       m_TargetFolder          = null;
        private Vector2             m_ScrollPos             = Vector2.zero;

        private SerializedProperty?         m_ClipInfoTblProp       = null;
        
        // ------------------------------------------------------------------
        // 有効化
        // ------------------------------------------------------------------
        private void OnEnable()
        {
            m_Target = target as SoundClump;
            if( m_Target == null )
            {
                return;
            }
            
            m_ClipInfoTblProp = serializedObject.FindProperty( "clipInfoTbl" );
        }

        // ------------------------------------------------------------------
        // 描画
        // ------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            if( m_Target == null )
                return;

            serializedObject.Update();

            if( GUILayout.Button( "コード生成" ) )
            {
                _GenerateCode();
            }
            
            EditorGUILayout.Space( 5 );
            
            m_TargetFolder = EditorGUILayout.ObjectField( "読み込み先フォルダ", m_TargetFolder, typeof(DefaultAsset), false ) as DefaultAsset;

            string dirPath = string.Empty;
            string fullPath = string.Empty;
            bool isEnableDir = true;
            if( m_TargetFolder != null )
            {
                dirPath = AssetDatabase.GetAssetOrScenePath( m_TargetFolder );
                fullPath = Application.dataPath.Substring( 0, Application.dataPath.Length - 6 ) + "\\" + dirPath;

                if( System.IO.Directory.Exists( fullPath ) == false )
                {
                    EditorGUILayout.HelpBox( $"フォルダが存在しません { fullPath }", MessageType.Warning );
                    isEnableDir = false;
                }
            }
            else
            {
                EditorGUILayout.HelpBox( $"フォルダが指定されていません", MessageType.Warning );
                isEnableDir = false;
            }
            
            GUI.enabled = isEnableDir;
            GUI.backgroundColor = Color.green;
            if( GUILayout.Button( "読み込み" ) )
            {
                _CreateList( dirPath, fullPath );
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.Space( 10 );
            
            using( var change = new EditorGUI.ChangeCheckScope() )
            {
                var tbl = m_Target.clipInfoTbl;
                if( tbl != null )
                {
                    if( tbl.Length == 0 )
                    {
                        EditorGUILayout.LabelField( "リストなし" );
                    }

                    using( var scroll = new EditorGUILayout.ScrollViewScope( m_ScrollPos, GUILayout.Height( 500 ) ) )
                    {
                        m_ScrollPos = scroll.scrollPosition;
                        
                        for( int i = 0; i < tbl.Length; ++i )
                        {
                            var elemProp = m_ClipInfoTblProp?.GetArrayElementAtIndex( i );
                            tbl[i].DrawGUI( i, elemProp );
                        }
                    }
                }

                if( change.changed )
                {
                    EditorUtility.SetDirty( m_Target );
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// フォルダ読み込み
        /// </summary>
        private void _CreateList( string dirPath, string dirFullPath )
        {
            if( m_Target == null )
            {
                return;
            }
            
            List<ClipInfo> list = new List<ClipInfo>();
            
            var dirInfo = new DirectoryInfo( dirFullPath );
            
            _AddClipInfo( list, dirPath, dirInfo.GetFiles( "*.mp3" ) );
            _AddClipInfo( list, dirPath, dirInfo.GetFiles( "*.wav" ) );
            
            m_Target.clipInfoTbl = list.ToArray();
            EditorUtility.SetDirty( m_Target );
        }

        private void _AddClipInfo( List<ClipInfo> list, string dirPath, FileInfo[] infos )
        {
            foreach( var file in infos )
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>( dirPath + "/" + file.Name );
                if( clip != null )
                {
                    string clipName = clip.name.ToUpper();
                    if( clipName.StartsWith( "SE_" ) )
                    {
                        clipName = clipName.Substring( 3 );
                    }
                    else if( clipName.StartsWith( "SE" ) )
                    {
                        clipName = clipName.Substring( 2 );
                    }
                    
                    var info = new ClipInfo();
                    {
                        info.clip = clip;
                        info.key = clipName;
                        info.volume = 1.0f;
                    }
                    list.Add( info );
                }
            }
        }
        
        /// <summary>
        /// コード生成
        /// </summary>
        private void _GenerateCode()
        {
            var generate = new GenerateCSCode();
            {
                generate.AppendLine( "namespace GApp" );
                generate.AppendLine( "{" );
                generate.AppendLine( "    // サウンド用定数(SE)");
                generate.AppendLine( "    namespace SoundConst");
                generate.AppendLine( "    {" );
                
                var dirInfo = new DirectoryInfo( Application.dataPath + "/" + "ResourcesDLC/Sound/SE" );
                var fileInfos = dirInfo.GetFiles( "*.asset" );
                for( int i = 0; i < fileInfos.Length; ++i )
                {
                    if( i > 0 )
                    {
                        generate.AppendLine( "        " );
                    }
                    
                    var info = fileInfos[i];
                    var fileName = Path.GetFileNameWithoutExtension( info.Name );
                    generate.AppendLine( $"        public static class {fileName}" );
                    generate.AppendLine( "        {" );
                    
                    // generate.AppendLine( $"            public  const   string      CLUMP_KEY           = \"{fileName}\";" );
                    
                    string caption = "SE_";
                    if( fileName.StartsWith( "JINGLE" ) )
                    {
                        caption = "JINGLE_";
                    }
                    
                    var tmp = AssetDatabase.LoadAssetAtPath<SoundClump>( "Assets/ResourcesDLC/Sound/SE/" + info.Name );
                    foreach( var clipInfo in tmp.clipInfoTbl )
                    {
                        var key = clipInfo.key;
                        generate.Append( $"            public  const   string      {caption}{key.ToUpper()}" );
                        for( int j = 0; j < 17 - key.Length; ++j )
                        {
                            generate.Append( " " );
                        }
                        generate.Append( $"= \"{key}\";\n" );
                    }
                    
                    generate.AppendLine( "        }" );
                }
                
                generate.AppendLine( "    }" );
                generate.AppendLine( "}" );
            }
            generate.Generate( "SoundConstSEKey" );
        }
    }
    
    #endif //UNITY_EDITOR
    #endregion エディタ
}
#nullable disable
