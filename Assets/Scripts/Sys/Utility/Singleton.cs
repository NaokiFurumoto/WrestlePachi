#nullable enable
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// シングルトン(MonoBehaviour用)
    /// </summary>
    public abstract class BehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static    T?  s_Instance      = null;

        public static T Instance
        {
            #pragma warning disable CS8603
            get => s_Instance;
            #pragma warning restore CS8603
        }
        
        public static bool isValid => s_Instance != null;

        /// <summary>
        /// インスタンスのセット(各継承先で設定)
        /// </summary>
        protected abstract void _SetInstance();

        private void Awake()
        {
            _SetInstance();

            if( s_Instance != null )
            {
                s_Instance.transform.parent = null;
                DontDestroyOnLoad( s_Instance.gameObject );
            }
            
            _OnAwake();
        }

        protected virtual void _OnAwake()
        {
            
        }

        private void OnDestroy()
        {
            _OnDestroy();
            
            s_Instance = null;
        }

        protected virtual void _OnDestroy()
        {
            
        }
    }

    /// <summary>
    /// シングルトン(非MonoBehaviour用)
    /// </summary>
    public class Singleton<T> where T : class, new()
    {
        private static  T?      s_Instance = null;
        
        public static   T       instance
        {
            #pragma warning disable CS8603
            get => s_Instance;
            #pragma warning restore CS8603
        }
        
        public static bool isValid => s_Instance != null;

        /// <summary>
        /// 生成
        /// </summary>
        public static void Create()
        {
            if( s_Instance != null )
            {
                return;
            }
            
            s_Instance = new T();
        }

        /// <summary>
        /// 破棄
        /// </summary>
        public static void Destroy()
        {
            s_Instance = null;
        }
    }
}

#nullable disable
