#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// ParamDisplay基底
    /// </summary>
    public class ParamDisplayBase : MonoBehaviour
    {
        [SerializeField]
        Image?                       m_Image             = null;

        [SerializeField]
        TextMeshProUGUI?             m_TextMesh          = null;
        
        [SerializeField]
        GameObject?                  m_ActivateObj       = null;
        
        #region データ適用
        
        protected void _SetSprite( Sprite spr )
        {
            if( m_Image != null )
            {
                m_Image.sprite = spr;
            }
        }

        protected void _SetRarerity(Sprite spr)
        {
            if (m_Image != null)
            {
                m_Image.sprite = spr;
            }
        }

        protected void _SetText( string str )
        {
            if( m_TextMesh != null )
            {
                m_TextMesh.SetText( str );
            }
        }
        
        protected void _SetText( string str, ref Color color )
        {
            if( m_TextMesh != null )
            {
                m_TextMesh.color = color;
                m_TextMesh.SetText( str );
            }
        }
        
        protected void _ActivateGameObject( bool isActivate )
        {
            m_ActivateObj.SetActiveNullable( isActivate );
        }
        
        #endregion データ適用
        
        #if UNITY_EDITOR
        public class Editor_ParamDisplayBase : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
            }
        }
        #endif //UNITY_EDITOR
    }
}

#nullable disable
