#nullable enable
using Cysharp.Threading.Tasks;
//using GApp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// Viewハンドル
    /// </summary>
    public partial class ViewHandle
    {
        private enum State
        {
            None,
            Load,
            LoadError,
            Loaded,
        }
        
        private     ViewBase?                               m_View      = null;
        private     State                                   m_State     = State.None;
       // private     ResourceManager.LoadInfo<GameObject>?   m_LoadInfo  = null;

        #region プロパティ
        
        public      bool        IsBusyView
        {
            get
            {
                if( m_View != null )
                {
                    return m_View.IsBusy;
                }
                return false;
            }
        }
        
        public      ViewBase?   View        => m_View;
        
        public      bool        IsLoaded    => m_State == State.Loaded;
        public      bool        IsLoaError  => m_State == State.LoadError;

        #endregion プロパティ

        /// <summary>
        /// Viewロード処理（Resources + PrefabManager使用）
        /// </summary>
        public async UniTask Load(
            string key,
            ViewBase.ViewData? data,
            Transform? parentTrans,
            int layer,
            bool isSystem = false)
        {

            if(string.IsNullOrEmpty(key)) return;

            m_State = State.Load;
            // Prefab取得
            var prefab = await PrefabManager.Instance.LoadAsync(key);

            if (prefab == null)
            {
                Debug.LogError($"Viewロード失敗: {key}");
                m_State = State.LoadError;
                return;
            }

            // インスタンス生成
            var viewObj = GameObject.Instantiate(prefab, parentTrans);

            m_View = viewObj.GetComponent<ViewBase>();

            if (m_View == null)
            {
                Debug.LogError($"ViewBaseが付いていません: {key}");
                m_State = State.LoadError;
                return;
            }

            // SortOrder設定
            m_View.SetSortingOrder(layer, isSystem);

            if (isSystem)
                m_View.SetSystemView();

            // 初期化
            await m_View.Initialize(data);

            m_State = State.Loaded;
        }

        private void _Unload()
        {
            //if( m_LoadInfo != null )
            //{
            //    ResourceManager.Instance.ReleaseAsset( m_LoadInfo );
            //    m_LoadInfo = null;
            //}
        }

        public void Release()
        {
            if( m_View != null )
            {
                m_View.Release();
                GameObject.Destroy( m_View.gameObject );
                m_View = null;
            }
            
            _Unload();
        }

        /// <summary>
        /// 開く
        /// </summary>
        public async UniTask Open()
        {
            if( m_View != null )
            {
                await m_View.OpenAsync();
                await UniTask.WaitUntil( () => m_View.IsOpened );
            }
        }
        
        /// <summary>
        /// 閉じる
        /// </summary>
        public async UniTask Close()
        {
            if( m_View != null )
            {
                await m_View.CloseAsync();
                await UniTask.WaitUntil( () => m_View == null || m_View.IsClosed );
            }
        }
    }
    
    /// <summary>
    /// View管理
    /// </summary>
    public partial class ViewManager
    {
        private     Transform?          m_ParentTrans       = null;
        private     List<ViewHandle>    m_ViewHandles       = new();
        private     bool                m_Processing        = false;
        private     bool                m_IsSystem          = false;
        
        private     List<ViewBase>      m_PlacedViews       = new();
        
        private     static    ViewManager?      s_Instance  = null;

        #region プロパティ

        private      bool           _IsBusy
        {
            get
            {
                if( m_Processing )
                {
                    return true;
                }

                for( int i = 0; i < m_ViewHandles.Count; ++i )
                {
                    if( m_ViewHandles[i].IsBusyView )
                    {
                        return true;
                    }
                }
                
                return false;
            }
        }
        
        public      bool            IsExistView
        {
            get
            {
                // 処理中は存在するものとする
                if( m_Processing )
                {
                    return true;
                }
                
                return m_ViewHandles.Count > 0;
            }
        }

        #endregion プロパティ

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize( Transform? trans, bool isSystem = false )
        {
            m_ParentTrans = trans;
            m_IsSystem = isSystem;
            if( isSystem == false )
            {
                s_Instance = this;
            }
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public void Release()
        {
            for( int i = 0; i < m_ViewHandles.Count; ++i )
            {
                m_ViewHandles[i].Release();
            }
            
            m_ViewHandles.Clear();
            
            m_PlacedViews.Clear();
            s_Instance = null;
        }

        /// <summary>
        /// Viewを追加
        /// 新しい画面（ウィンドウ）を、今ある画面の一番上に重ねて表示する処理
        /// </summary>
        private async UniTask<ViewHandle?> _PushView<T>( string key, ViewBase.ViewData? data ) where T : ViewBase
        {
            if( string.IsNullOrEmpty( key ) )
            {
                return null;
            }

            //もし、前の画面を開いている途中だったら、それが終わるまでここで待機
            await UniTask.WaitWhile( () => m_Processing );

            m_Processing = true;
            
            int layerId = GetMaxLayerId();
            
            var handle = new ViewHandle();
            m_ViewHandles.Add( handle );
            
            await handle.Load( key, data, m_ParentTrans, layerId + 1, m_IsSystem );
            
            await handle.Open();
            
            m_Processing = false;
            return handle;
        }

        /// <summary>
        /// Viewを破棄
        /// </summary>
        private async UniTask _PopView( ViewBase view )
        {
            var handle = m_ViewHandles.Find( x => x.View == view ); 
            if( handle == null )
            {
                return;
            }
            
            await UniTask.WaitWhile( () => m_Processing );
            
            m_Processing = true;
            
            await handle.Close();
            
            handle.Release();
            m_ViewHandles.Remove( handle );
            
            m_Processing = false;
        }

        public void SetPlacedView( ViewBase viewBase )
        {
            if( m_PlacedViews.Contains( viewBase ) )
            {
                return;
            }

            m_PlacedViews.Add( viewBase );
        }

        private int GetMaxLayerId()
        {
            int layerId = 0;
            for( int i = 0; i < m_ViewHandles.Count; ++i )
            {
                var view = m_ViewHandles[i]?.View;
                if( view != null )
                {
                    layerId = Math.Max( layerId, view.LayerId );
                }
            }

            for( int i = 0; i < m_PlacedViews.Count; ++i )
            {
                var view = m_PlacedViews[i];
                if( view != null )
                {
                    layerId = Math.Max( layerId, view.LayerId );
                }
            }

            return layerId;
        }
        
        #region View取得
        
        /// <summary>
        /// Viewの取得(ロード完了して、開いているView)
        /// </summary>
        private T? _GetView<T>() where T : ViewBase
        {
            for( int i = 0; i < m_ViewHandles.Count; ++i )
            {
                var handle = m_ViewHandles[i];
                if( handle == null || handle.View == null )
                {
                    continue;
                }
                
                if( handle.IsLoaded && handle.View.IsOpened )
                {
                    if( handle.View is T view )
                    {
                        return view;
                    }
                }
            }

            for( int i = 0; i < m_PlacedViews.Count; ++i )
            {
                var placedView = m_PlacedViews[i];
                if( placedView is T view )
                {
                    return view;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 最後に開いているViewを取得
        /// </summary>
        private ViewBase? _GetLatestView()
        {
            if( m_ViewHandles.Count == 0 )
            {
                return null;
            }
            
            for( int i = m_ViewHandles.Count - 1; i >= 0; --i )
            {
                var handle = m_ViewHandles[i];
                if( handle == null || handle.View == null )
                {
                    continue;
                }
                
                if( handle.IsLoaded && handle.View.IsOpened )
                {
                    return handle.View;
                }
            }
            
            return null;
        }
        
        public static T? GetView<T>() where T : ViewBase
        {
            if( s_Instance != null )
            {
                return s_Instance._GetView<T>();
            }
            
            return null;
        }
        
        public static ViewBase? GetLatestView()
        {
            if( s_Instance != null )
            {
                return s_Instance._GetLatestView();
            }
            
            return null;
        }

        public static ViewBase? GetLatestSystemView()
        {
            if( SystemManager.isValid == false )
            {
                return null;
            }

            var viewMng = SystemManager.Instance.ViewMng;
            return viewMng?._GetLatestView();
        }
        
        #endregion View取得
        
        #region 呼び出し
        
        /// <summary>
        /// Viewを追加
        /// </summary>
        public static void PushView<T>( string key, ViewBase.ViewData? data = null ) where T : ViewBase
        {
            if( s_Instance != null )
            {
                s_Instance._PushView<T>( key, data ).Forget();
            }
        }
        
        public static async UniTask<ViewHandle?> PushViewAsync<T>( string key, ViewBase.ViewData? data = null ) where T : ViewBase
        {
            if( s_Instance != null )
            {
                return await s_Instance._PushView<T>( key, data );
            }
            
            return null;
        }
        
        // Systemダイアログ(シーンフェードより手前)
        public static void PushSystemView<T>( string key, ViewBase.ViewData? data = null ) where T : ViewBase
        {
            if( SystemManager.isValid == false )
            {
                return;
            }

            var viewMng = SystemManager.Instance.ViewMng;
            viewMng?._PushView<T>( key, data ).Forget();
        }
        
        public static async UniTask<ViewHandle?> PushSystemViewAsync<T>( string key, ViewBase.ViewData? data = null ) where T : ViewBase
        {
            if( SystemManager.isValid == false )
            {
                return null;
            }

            var viewMng = SystemManager.Instance.ViewMng;
            if( viewMng == null )
            {
                return null;
            }

            return await viewMng._PushView<T>( key, data );
        }
        
        /// <summary>
        /// Viewを破棄
        /// </summary>
        public static void PopView( ViewBase? view )
        {
            if( s_Instance != null && view != null )
            {
                s_Instance._PopView( view ).Forget();
            }
        }

        public static void PopView( ViewHandle? handle )
        {
            if( s_Instance != null && handle?.View != null )
            {
                s_Instance._PopView( handle.View ).Forget();
            }
        }
        
        public static async UniTask PopViewAsync( ViewBase? view )
        {
            if( s_Instance != null && view != null )
            {
                await s_Instance._PopView( view );
            }
        }
        
        public static async UniTask PopViewAsync( ViewHandle? handle )
        {
            if( s_Instance != null && handle?.View != null )
            {
                await s_Instance._PopView( handle.View );
            }
        }
        
        public static void PopSystemView( ViewBase? view )
        {
            if( view != null && SystemManager.isValid )
            {
                var viewMng = SystemManager.Instance.ViewMng;
                viewMng?._PopView( view ).Forget();
            }
        }
        
        public static void PopSystemView( ViewHandle? handle )
        {
            if( handle?.View != null && SystemManager.isValid )
            {
                var viewMng = SystemManager.Instance.ViewMng;
                viewMng?._PopView( handle.View ).Forget();
            }
        }
        
        public static async UniTask PopSystemViewAsync( ViewBase? view )
        {
            if( view != null && SystemManager.isValid )
            {
                var viewMng = SystemManager.Instance.ViewMng;
                if( viewMng != null )
                {
                    await viewMng._PopView( view );
                }
            }
        }
        
        public static async UniTask PopSystemViewAsync( ViewHandle? handle )
        {
            if( handle?.View != null && SystemManager.isValid )
            {
                var viewMng = SystemManager.Instance.ViewMng;
                if( viewMng != null )
                {
                    await viewMng._PopView( handle.View );
                }
            }
        }

        /// <summary>
        /// 動作中チェック
        /// </summary>
        public static bool IsBusy()
        {
            return s_Instance?._IsBusy ?? false;
        }

        public static bool IsBusySystem()
        {
            if( SystemManager.isValid == false )
            {
                return false;
            }

            return SystemManager.Instance.ViewMng?._IsBusy ?? false;
        }
        
        #endregion 呼び出し
        
        private void _OnWindowResize()
        {
            for( int i = 0; i < m_PlacedViews.Count; ++i )
            {
                m_PlacedViews[i].OnWindowResize();
            }
            
            for( int i = 0; i < m_ViewHandles.Count; ++i )
            {
                var view = m_ViewHandles[i].View;
                if( view != null )
                {
                    view.OnWindowResize();
                }
            }
        }
        
        public static void OnWindowResize()
        {
            if( SystemManager.isValid )
            {
                SystemManager.Instance.ViewMng?._OnWindowResize();
            }

            if( s_Instance != null )
            {
                s_Instance._OnWindowResize();
            }
        }
    }
}

#nullable disable
