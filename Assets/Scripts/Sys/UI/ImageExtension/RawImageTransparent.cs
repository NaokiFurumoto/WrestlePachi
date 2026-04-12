#nullable enable
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// RawImage拡張
    /// テクスチャが無い場合は透明にする
    /// 参照を持たずに画像Preview確認できる
    /// </summary>
    [AddComponentMenu("UI/RawImage(透明)")]
    public class RawImageTransparent : RawImage
    {
        private     string      m_EditorPreview     = string.Empty;
        
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
                if( _IsEnablePreview() && texture == null )
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
            if( texture != null
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
        
        #if UNITY_EDITOR
        
        [CustomEditor( typeof( RawImageTransparent ) )]
        public class Editor_RawImageTransparent : UnityEditor.UI.RawImageEditor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                
                var tmp = target as RawImageTransparent;
                if( tmp == null )
                {
                    return;
                }
                
                var tex = EditorGUILayout.ObjectField( "Preview参照:", null, typeof( Texture ), false, GUILayout.Height( 17 ) ) as Texture;
                if( tex != null )
                {
                    tmp.m_EditorPreview = AssetDatabase.GetAssetPath( tex );
                }
                tmp.m_EditorPreview = EditorGUILayout.TextField( "Preview Path", tmp.m_EditorPreview );
                
                GUI.enabled = tmp.texture != null;
                if( GUILayout.Button( "今の画像をセット + クリア" ) )
                {
                    tmp.m_EditorPreview = AssetDatabase.GetAssetPath( tmp.texture );
                    tmp.texture = null;
                }
                GUI.enabled = true;
                
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
