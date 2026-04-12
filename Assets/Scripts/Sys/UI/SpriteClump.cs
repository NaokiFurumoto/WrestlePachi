#nullable enable
using System;
using System.Collections.Generic;
using GameSys;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Spriteを複数所持して差し替える
/// </summary>
[CreateAssetMenu( menuName = "ScriptableObjects/SpriteClump", order = 2)]
public class SpriteClump : ScriptableObject
{
    [Serializable]
    public class SpriteInfo
    {
        public  string      key         = string.Empty;
        public  Sprite?     sprite      = null;
        
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
                        EditorGUILayout.LabelField( "sprite", capW );
                        sprite = EditorGUILayout.ObjectField( sprite, typeof(Sprite), false ) as Sprite;
                    }
                    
                    GUI.backgroundColor = Color.red;
                    if( GUILayout.Button( "削除" ) )
                    {
                        isDelete = true;
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            
            return isDelete;
        }
        #endif //UNITY_EDITOR
    }
    
    [SerializeField]
    private     SpriteInfo[]        spriteInfoTbl       = new SpriteInfo[0];
    
    private Dictionary<string, SpriteInfo>      m_Dict      = new Dictionary<string, SpriteInfo>();
    
    public  Dictionary<string, SpriteInfo>      Dict        => m_Dict;
    
    /// <summary>
    /// Sprite取得
    /// </summary>
    public Sprite? GetSprite( string key )
    {
        if( m_Dict.Count == 0 && spriteInfoTbl.Length > 0 )
        {
            _CreateDict();
        }
        
        m_Dict.TryGetValue( key, out var spr );
        return spr?.sprite;
    }
    
    public Sprite? GetSprite( int idx )
    {
        return spriteInfoTbl.SafeGetAt( idx, null )?.sprite;
    }
    
    /// <summary>
    /// Dictionary作成
    /// </summary>
    private void _CreateDict()
    {
        for( int i = 0; i < spriteInfoTbl.Length; ++i )
        {
            var info = spriteInfoTbl[i];
            m_Dict.Add( info.key, info );
        }
    }
    
    public void ReleaseDict()
    {
        m_Dict.Clear();
    }
    
    #region エディタ
    
    #if UNITY_EDITOR
    
    [CustomEditor(typeof( SpriteClump ) )]
    public class Editor_SpriteClump : Editor
    {
        private SpriteClump?        m_Target                = null;
        
        private Vector2             m_ScrollPos             = Vector2.zero;

        private SerializedProperty? m_TblProp               = null;
        
        // ------------------------------------------------------------------
        // 有効化
        // ------------------------------------------------------------------
        private void OnEnable()
        {
            m_Target = target as SpriteClump;
            if( m_Target == null )
            {
                return;
            }
            
            m_TblProp = serializedObject.FindProperty( "spriteInfoTbl" );
        }

        // ------------------------------------------------------------------
        // 描画
        // ------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            if( m_Target == null )
                return;
            
            bool isAdd = false; // 追加したフレームではテーブル表示処理をしない(エラーが出るため)
            
            serializedObject.Update();
            
            EditorGUILayout.Space( 5 );
            
            using( var change = new EditorGUI.ChangeCheckScope() )
            {
                GUI.backgroundColor = Color.green;
                if( GUILayout.Button( "追加" ) )
                {
                    var list = new List<SpriteInfo>();
                    list.AddRange( m_Target.spriteInfoTbl );
                    list.Add( new SpriteInfo() );
                    m_Target.spriteInfoTbl = list.ToArray();
                    isAdd = true;
                }
                GUI.backgroundColor = Color.white;
                
                var tbl = m_Target.spriteInfoTbl;
                if( tbl.Length == 0 )
                {
                    EditorGUILayout.LabelField( "リストなし" );
                }
                
                if( isAdd == false )
                {
                    using( var scroll = new EditorGUILayout.ScrollViewScope( m_ScrollPos, GUILayout.Height( 500 ) ) )
                    {
                        m_ScrollPos = scroll.scrollPosition;
                    
                        for( int i = 0; i < tbl.Length; ++i )
                        {
                            var elemProp = m_TblProp?.GetArrayElementAtIndex( i );
                            if( tbl[i].DrawGUI( i, elemProp ) )
                            {
                                var list = new List<SpriteInfo>();
                                list.AddRange( m_Target.spriteInfoTbl );
                                list.RemoveAt( i );
                                m_Target.spriteInfoTbl = list.ToArray();
                                m_TblProp = serializedObject.FindProperty( "spriteInfoTbl" );
                                break;
                            }
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
    }
    
    #endif //UNITY_EDITOR
    #endregion エディタ
}

#nullable disable
