#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// View生成時のSortOrderの調整
    /// </summary>
    public class ViewCanvasOrder : MonoBehaviour
    {
        [SerializeField]
        private     int             m_OrderOffset   = 0;
        
        [SerializeField]
        private     Canvas?[]       m_CanvasTbl     = new Canvas[0];
        
        private     CanvasScalerEx?[]   m_CacheScalerEx = new CanvasScalerEx?[0];
        
        /// <summary>
        /// SortOrder
        /// </summary>
        public void SetSortOrder( int layer, bool isSystem = false )
        {
            int baseOrder = layer * SortOrderConst.SORTING_ORDER_PER_VIEW;
            if( isSystem )
            {
                baseOrder += SortOrderConst.SORTING_ORDER_SCENE_FADE;
            }
            
            int cnt = 0;
            for( int i = 0; i < m_CanvasTbl.Length; ++i )
            {
                var canvas = m_CanvasTbl[i];
                if( canvas == null )
                {
                    continue;
                }
                
                canvas.sortingOrder = baseOrder + m_OrderOffset + cnt;
                ++cnt;
                
            }
        }
        
        public void OnWindowResize()
        {
            if( m_CacheScalerEx.Length != m_CanvasTbl.Length )
            {
                m_CacheScalerEx = new CanvasScalerEx[m_CanvasTbl.Length];
                for( int i = 0; i < m_CanvasTbl.Length; ++i )
                {
                    var canvas = m_CanvasTbl[i];
                    if( canvas == null )
                    {
                        continue;
                    }
                    m_CacheScalerEx[i] = canvas.GetComponent<CanvasScalerEx>();
                }
            }
            
            for( int i = 0; i < m_CacheScalerEx.Length; ++i )
            {
                var scalerEx = m_CacheScalerEx[i];
                if( scalerEx == null )
                {
                    continue;
                }
                
                scalerEx.SetParameter();
            }
        }
        
        #region Editor
        #if UNITY_EDITOR
    
        [CustomEditor( typeof(ViewCanvasOrder) )]
        public class Editor_ViewCanvasOrder : Editor
        {
            public override void OnInspectorGUI()
            {
                GUI.enabled = false;
                base.OnInspectorGUI();
                GUI.enabled = true;

                var tmp = target as ViewCanvasOrder;
                if( tmp == null )
                {
                    return;
                }
            
                if( GUILayout.Button( "セットアップ" ) )
                {
                    tmp.m_CanvasTbl = tmp.GetComponentsInChildren<Canvas>(true);
                    EditorUtility.SetDirty( tmp );
                }
            }
        }
    
        #endif // UNITY_EDITOR
        #endregion Editor
    }
}

#nullable disable
