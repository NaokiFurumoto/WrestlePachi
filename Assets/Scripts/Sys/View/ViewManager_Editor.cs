#nullable enable
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameSys
{
    #if UNITY_EDITOR
    
    /// <summary>
    /// Editor使用部分
    /// </summary>
    
    public partial class ViewHandle
    {
        /// <summary>
        /// ロード
        /// </summary>
        public async UniTask Editor_Load( string prefabPath, ViewBase.ViewData? data, Transform? parentTrans, int layer, bool isSystem = false )
        {
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>( prefabPath );
            if( obj == null )
            {
                Debug.LogWarning( $"該当のprefabがありません {prefabPath}" );
                return;
            }
            
            m_State = State.Loaded;
            GameObject viewObj = GameObject.Instantiate( obj, parentTrans );
            // viewObj.transform.SetParent( parentTrans );
            
            m_View = viewObj.GetComponent<ViewBase>();
            if( m_View != null )
            {
                m_View.SetSortingOrder( layer, isSystem );
                if( isSystem )
                {
                    m_View.SetSystemView();
                }
                await m_View.Initialize( data );
            }
        }
    }
    
    public partial class ViewManager
    {
        /// <summary>
        /// Addressableに含まれないViewのロードを行う(AssetDatabase使用)
        /// </summary>
        public static void Editor_PushView<T>( string prefabPath, ViewBase.ViewData? data = null ) where T : ViewBase
        {
            if( s_Instance != null )
            {
                s_Instance._Editor_PushView<T>( prefabPath, data ).Forget();
            }
        }
        
        private async UniTask<ViewHandle?> _Editor_PushView<T>( string prefabPath, ViewBase.ViewData? data ) where T : ViewBase
        {
            if( string.IsNullOrEmpty( prefabPath ) )
            {
                return null;
            }
            
            await UniTask.WaitWhile( () => m_Processing );

            m_Processing = true;
            
            int layerId = GetMaxLayerId();
            
            var handle = new ViewHandle();
            m_ViewHandles.Add( handle );
            
            await handle.Editor_Load( prefabPath, data, m_ParentTrans, layerId + 1, m_IsSystem );
            
            await handle.Open();
            
            m_Processing = false;
            return handle;
        }
    }
    
    #endif //UNITY_EDITOR
}

#nullable disable
