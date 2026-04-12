#nullable enable
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// Image拡張
    /// テクスチャが無い場合は透明にする
    /// 参照を持たずに画像Preview確認できる
    /// </summary>
    [AddComponentMenu("UI/Image(透明)")]
    public class ImageTransparent : Image
    {
        [SerializeField]
        private     string          m_EditorPreview     = string.Empty;
        
        [SerializeField]
        private     SpriteClump?    m_SpriteClump       = null;
        
        #if UNITY_EDITOR
        
        protected override void Awake()
        {
            if( BuildPipeline.isBuildingPlayer )
            {
                m_EditorPreview = string.Empty;
                return;
            }
            
            base.Awake();
        }
        
        public override Texture? mainTexture
        {
            get
            {
                if( _IsEnablePreview() && sprite == null )
                {
                    return AssetDatabase.LoadAssetAtPath( m_EditorPreview, typeof( Texture ) ) as Texture;
                }
                return base.mainTexture;
            }
        }
        
        private bool _IsEnablePreview()
        {
            return Application.isPlaying == false && string.IsNullOrEmpty( m_EditorPreview ) == false;
        }
        
        #endif // UNITY_EDITOR
        
        protected override void OnPopulateMesh( VertexHelper vh )
        {
            if( sprite != null
                #if UNITY_EDITOR
                || _IsEnablePreview()
                #endif
              )
            {
                base.OnPopulateMesh( vh );
            }
            else
            {
                vh.Clear();
            }
        }

        protected override void OnDestroy()
        {
            if( m_SpriteClump != null )
            {
                m_SpriteClump.ReleaseDict();
            }
            
            base.OnDestroy();
        }

        /// <summary>
        /// SpriteClumpから設定
        /// </summary>
        public void SetSpriteFromClump( string key )
        {
            if( m_SpriteClump == null )
            {
                return;
            }
            
            sprite = m_SpriteClump.GetSprite( key );
        }
        
        public void SetSpriteFromClumpByIdx( int idx )
        {
            if( m_SpriteClump == null )
            {
                return;
            }
            
            sprite = m_SpriteClump.GetSprite( idx );
        }
        
        #if UNITY_EDITOR
        
        [CustomEditor( typeof( ImageTransparent ) )]
        public class Editor_ImageTransparent : UnityEditor.UI.ImageEditor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                
                var tmp = target as ImageTransparent;
                if( tmp == null )
                {
                    return;
                }
                
                EditorGUILayout.Space( 10 );
                
                var sprite = EditorGUILayout.ObjectField( "Preview参照:", null, typeof( Sprite ), false, GUILayout.Height( 17 ) ) as Sprite;
                if( sprite != null )
                {
                    tmp.m_EditorPreview = AssetDatabase.GetAssetPath( sprite );
                }
                tmp.m_EditorPreview = EditorGUILayout.TextField( "Preview Path", tmp.m_EditorPreview );
                
                GUI.enabled = tmp.sprite != null;
                if( GUILayout.Button( "今の画像をセット + クリア" ) )
                {
                    tmp.m_EditorPreview = AssetDatabase.GetAssetPath( tmp.sprite );
                    tmp.sprite = null;
                }
                GUI.enabled = true;
                
                EditorGUILayout.Space( 10 );
                
                // SpriteClump
                tmp.m_SpriteClump = EditorGUILayout.ObjectField( "Clump: ", tmp.m_SpriteClump, typeof( SpriteClump ), false ) as SpriteClump;
                GUI.enabled = tmp.m_SpriteClump != null;
                if( GUILayout.Button( "Clumpの先頭の画像をセット" ) )
                {
                    tmp.SetSpriteFromClumpByIdx( 0 );
                }
                GUI.enabled = false;
                
                if( GUI.changed )
                {
                    if( tmp.enabled )
                    {
                        tmp.enabled = false;
                        tmp.enabled = true;
                    }
                }
            }
        }
        
        #endif // UNITY_EDITOR
    }
}

#nullable disable
