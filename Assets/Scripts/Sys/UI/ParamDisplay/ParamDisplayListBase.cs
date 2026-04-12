#nullable enable
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// ParamDisplayList基底
    /// </summary>
    public class ParamDisplayListBase<T> : MonoBehaviour where T : ParamDisplayBase
    {
        [SerializeField]
        private     T?[]        m_PDList        = new T[0];
        
        protected   T?[]        pdList          =>  m_PDList;
        
        #if UNITY_EDITOR
        private void _Editor_GatherParamDisplay()
        {
            m_PDList = gameObject.GetComponentsInChildren<T>( true );
        }
        
        public class Editor_ParamDisplayListBase : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                
                serializedObject.Update();
                
                var tmp = target as ParamDisplayListBase<T>;
                if( tmp == null )
                {
                    return;
                }
                
                if( GUILayout.Button( "回収" ) )
                {
                    tmp._Editor_GatherParamDisplay();
                    EditorUtility.SetDirty( target );
                }
            }
        }
        #endif //UNITY_EDITOR
    }
}

#nullable disable
