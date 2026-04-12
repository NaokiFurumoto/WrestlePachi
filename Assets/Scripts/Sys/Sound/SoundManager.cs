#nullable enable
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace GameSys
{
    /// <summary>
    /// サウンド管理
    /// </summary>
    public partial class SoundManager : BehaviourSingleton<SoundManager>
    {
        #region 定数

        private     const   int         PLAY_BGM_COUNT          = 2;
        private     const   int         PLAY_SE_COUNT           = 10;
        private     const   int         PLAY_VOICE_COUNT        = 2;
        
        private     const   float       BGM_VOLUME_DOWN_RATE    = 0.5f;

        #endregion 定数
        
        #region メンバ
        
        [SerializeField]
        private     AudioSource?[]      m_BGMSourceTbl      = new AudioSource[PLAY_BGM_COUNT];
        
        [SerializeField]
        private     AudioSource?[]      m_SESourceTbl       = new AudioSource[PLAY_SE_COUNT];
        
        [SerializeField]
        private     AudioSource?[]      m_VOICESourceTbl    = new AudioSource[PLAY_VOICE_COUNT];
        
        private     List<PlayDataBGM>   m_BGMList       = new List<PlayDataBGM>( PLAY_BGM_COUNT );
        private     List<PlayData>      m_SEList        = new List<PlayData>( PLAY_SE_COUNT );
        private     List<PlayData>      m_VOICEList     = new List<PlayData>( PLAY_VOICE_COUNT );
        
        private     Pool<PlayDataBGM>   m_BGMPool       = new Pool<PlayDataBGM>( PLAY_BGM_COUNT );
        private     Pool<PlayData>      m_SEPool        = new Pool<PlayData>( PLAY_SE_COUNT );
        private     Pool<PlayData>      m_VOICEPool     = new Pool<PlayData>( PLAY_VOICE_COUNT );

        private     List<PlayData>      m_StopList      = new List<PlayData>( 16 );
        private     bool                m_IsInitialized = false;
        
        //private     ResourceManager.LoadInfo<BGMClump>?             m_LoadBGMClump          = null;
        //private     List<ResourceManager.LoadInfo<SoundClump>>      m_SEClumpLoadInfos      = new ();
        //private     List<ResourceManager.LoadInfo<SoundClump>>      m_VoiceClumpLoadInfos   = new ();

        // 音量
        private     float       m_MasterVolume      = 1.0f;
        private     float       m_VolumeBGM         = 1.0f;
        private     float       m_VolumeBGMSub      = 1.0f;     // SE再生時にBGM音量を抑えるときに使用する
        private     float       m_VolumeSE          = 1.0f;
        private     float       m_VolumeVOICE       = 1.0f;
        
        private     string      m_CommonSEKey       = string.Empty;
        
        #endregion メンバ
        
        #region プロパティ
        
        public  float   MasterVolume    =>      m_MasterVolume;
        public  float   VolumeBGM       =>      m_VolumeBGM;
        public  float   VolumeSE        =>      m_VolumeSE;
        public  float   VolumeVOICE     =>      m_VolumeVOICE;
        
        public  bool    IsPlayingBGM    =>      m_BGMList.Count > 0;
        
        public  bool    IsInitialized   =>      m_IsInitialized;
        
        #endregion プロパティ
        
        /// <summary>
        /// インスタンスのセット
        /// </summary>
        protected override void _SetInstance()
        {
            s_Instance = this;
        }

        protected override void _OnAwake()
        {
            base._OnAwake();
            
            m_BGMPool.Initialize( ( i ) =>
            {
                var result = new PlayDataBGM();
                result.SetParamBGM( i, m_BGMSourceTbl[i] );
                return result;
            } );
            
            m_SEPool.Initialize( ( i ) =>
            {
                var result = new PlayData();
                result.SetParam( i, Type.SE, m_SESourceTbl[i] );
                return result;
            } );
            
            m_VOICEPool.Initialize( ( i ) =>
            {
                var result = new PlayData();
                result.SetParam( i, Type.VOICE, m_VOICESourceTbl[i] );
                return result;
            } );
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize( string commonSEKey )
        {
            if( m_IsInitialized )
            {
                return;
            }
            
            m_CommonSEKey = commonSEKey;
            StartCoroutine( _OnInitialize() );
        }

        private IEnumerator _OnInitialize()
        {
            // 共通SEロード
            LoadSE( m_CommonSEKey );
            
            while( IsSELoading() )
            {
                yield return null;
            }
            
            m_IsInitialized = true;
        }

        /// <summary>
        /// 更新
        /// </summary>
        private void Update()
        {
            bool isStopedSE = false;
            bool isBGMVolumeDown = false;
            
            for( int i = m_SEList.Count - 1; i >= 0; --i )
            {
                var data = m_SEList[i];
                if( data.IsPlaying() == false )
                {
                    _StopSE( data );
                    m_SEList.Remove( data );
                    isStopedSE = true;
                    continue;
                }

                if( data.IsBGMVolumeDown )
                {
                    isBGMVolumeDown = true;
                }
            }

            if( isStopedSE && isBGMVolumeDown == false )
            {
                m_VolumeBGMSub = 1.0f;
                _ChangedVolumeBGM();
            }
        }
        
        #region BGM
        
        /// <summary>
        /// BGM再生
        /// </summary>
        public void PlayBGM( string key, float volume = 1f, float fadeoutSec = 0f )
        {
            _PlayBGM( key, volume, fadeoutSec ).Forget();
        }

        private async UniTask _PlayBGM( string key, float volume, float fadeoutSec )
        {
            if( m_BGMList.Count > 0 )
            {
                await _StopBGM( fadeoutSec );
            }
            
            var data = m_BGMPool.Get();
            if( data == null )
            {
                return;
            }
            
            //m_LoadBGMClump = ResourceManager.instance.LoadAsset<BGMClump>( key );
            //if( m_LoadBGMClump == null || m_LoadBGMClump.IsValid == false )
            //{
            //    return;
            //}
            
            //await UniTask.WaitUntil( () => m_LoadBGMClump.IsDone );
            
            //if( m_LoadBGMClump.IsError || m_LoadBGMClump.IsSuccess == false )
            //{
            //    return;
            //}
            
            //data.SetBGMClump( m_LoadBGMClump.Result );
            data.SetData( key, volume, 1.0f, m_MasterVolume * m_VolumeBGM * m_VolumeBGMSub );
            data.PlayBGM();

            m_BGMList.Add( data );
        }

        public void StopBGM( float fadeoutSec = 0f )
        {
            _StopBGM( fadeoutSec ).Forget();
        }
        
        private async UniTask _StopBGM( float fadeoutSec )
        {
            if( m_BGMList.Count == 0 )
            {
                return;
            }
            
            var data = m_BGMList[0];
            if( data.IsPlaying() == false )
            {
                while( m_BGMList.Count > 0 )
                {
                    await UniTask.Yield();
                }
                return;
            }
            
            await data.Stop( fadeoutSec );
            m_BGMList.RemoveAt( 0 );
            data.ReleaseBGMClump();
            m_BGMPool.Return( data );
            
            //if( m_LoadBGMClump != null )
            //{
            //    ResourceManager.instance.ReleaseAsset( m_LoadBGMClump );
            //    m_LoadBGMClump = null;
            //}
        }

        #endregion BGM

        #region SE

        public void LoadSE( string clumpKey )
        {
            //StartCoroutine( _LoadSEAudioClump( clumpKey ) );
        }

        //private IEnumerator _LoadSEAudioClump( string clumpKey )
        //{
        //    if( m_SEClumpLoadInfos.Find( c => c.AdrsKey == clumpKey ) != null )
        //    {
        //        yield break;
        //    }
            
        //    var loadInfo = ResourceManager.instance.LoadAsset<SoundClump>( clumpKey );
        //    if( loadInfo == null || loadInfo.IsValid == false )
        //    {
        //        yield break;
        //    }
            
        //    m_SEClumpLoadInfos.Add( loadInfo );

        //    while( loadInfo.IsDone == false )
        //    {
        //        yield return null;
        //    }
            
        //    if( loadInfo.IsError || loadInfo.IsSuccess == false )
        //    {
        //        m_SEClumpLoadInfos.Remove( loadInfo );
        //        yield break;
        //    }

        //    loadInfo.Result.CreateDict();
        //}

        /// <summary>
        /// SEアンロード
        /// </summary>
        public void UnloadSE( string clumpKey )
        {
            //var loadInfo = _GetSELoadInfo( clumpKey );
            //if( loadInfo == null )
            //{
            //    return;
            //}

            //StopSEAll();    // TODO: ひとまず全部止める
            //m_SEClumpLoadInfos.Remove( loadInfo );
            //loadInfo.Result.ReleaseDict();
            //ResourceManager.instance.ReleaseAsset( loadInfo );
        }
        
        /// <summary>
        /// SEロード中か
        /// </summary>
        public bool IsSELoading()
        {
            //for( int i = 0; i < m_SEClumpLoadInfos.Count; ++i )
            //{
            //    if( m_SEClumpLoadInfos[i].IsDone == false )
            //    {
            //        return true;
            //    }
            //}
            
            return false;
        }

        /// <summary>
        /// SE再生
        /// </summary>
        public int PlaySE( string clumpKey, string key, float volume = 1f, bool isLoop = false, bool isBgmVolueDown = false )
        {
            var data = m_SEPool.Get();
            if( data == null )
            {
                DebugLog.LogWarning( "SEに空きがありません" );
                return -1;
            }
            
            var clipInfo = _GetSEAudioClip( clumpKey, key );
            if( clipInfo == null || clipInfo.clip == null )
            {
                DebugLog.LogWarning( $"指定のクリップが存在しません {clumpKey}, {key}" );
                return -1;
            }
            
            data.SetData( key, volume, clipInfo.volume, m_MasterVolume * m_VolumeSE );
            data.SetBgmVolumeDown( isBgmVolueDown );
            data.Play( clipInfo.clip, isLoop );
            
            m_SEList.Add( data );

            if( isBgmVolueDown )
            {
                m_VolumeBGMSub = BGM_VOLUME_DOWN_RATE;
                _ChangedVolumeBGM();
            }
            
            return data.Id;
        }
        
        /// <summary>
        /// 共通SE 再生
        /// </summary>
        public static void PlayCommonSE( string key )
        {
            Instance._PlayCommonSE( key );
        }
        
        private void _PlayCommonSE( string key, float volume = 1f )
        {
            PlaySE( m_CommonSEKey, key, volume );
        }
        
        /// <summary>
        /// SE停止
        /// </summary>
        public void StopSE( string key )
        {
            PlayData? data = null;
            for( int i = 0; i < m_SEList.Count; ++i )
            {
                var tmp = m_SEList[i];
                if( tmp.Key == key )
                {
                    data = tmp;
                    break;
                }
            }

            if( data != null )
            {
                _StopSE( data );
                m_SEList.Remove( data );
            }
        }

        // PlaySE()の戻り値で指定
        public void StopSE( int id )
        {
            PlayData? data = null;
            for( int i = 0; i < m_SEList.Count; ++i )
            {
                var tmp = m_SEList[i];
                if( tmp.Id == id )
                {
                    data = tmp;
                    break;
                }
            }

            if( data != null )
            {
                _StopSE( data );
                m_SEList.Remove( data );
            }
        }

        public void StopSEAll()
        {
            for( int i = 0; i < m_SEList.Count; ++i )
            {
                _StopSE( m_SEList[i] );
            }
            
            m_SEList.Clear();
        }

        private void _StopSE( PlayData data )
        {
            data.Stop().Forget();
            m_SEPool.Return( data );
        }

        //private ResourceManager.LoadInfo<SoundClump>? _GetSELoadInfo( string clumpKey )
        //{
        //    if( string.IsNullOrEmpty( clumpKey ) )
        //    {
        //        return null;
        //    }
            
        //    for( int i = 0; i < m_SEClumpLoadInfos.Count; ++i )
        //    {
        //        var loadInfo = m_SEClumpLoadInfos[i];
        //        if( loadInfo.AdrsKey == clumpKey )
        //        {
        //            return loadInfo;
        //        }
        //    }
            
        //    return null;
        //}
        
        private SoundClump.ClipInfo? _GetSEAudioClip( string clumpKey, string key )
        {
            //var loadInfo = _GetSELoadInfo( clumpKey );
            //if( loadInfo == null )
            //{
            //    return null;
            //}
            
            //if( loadInfo.Result.Dict.TryGetValue( key, out var info ) )
            //{
            //    return info;
            //}
            
            return null;
        }
        
        #endregion SE
        
        #region VOICE

        /// <summary>
        /// VOICE再生
        /// </summary>
        public void PlayVOICE( string clumpKey, string key, float volume )
        {
            var data = m_VOICEPool.Get();
            if( data == null )
            {
                return;
            }
            
            // TODO ひとまずSEと同じにしているだけ
            //var clipInfo = _GetVoiceAudioClip( clumpKey, key );
            //if( clipInfo == null || clipInfo.clip == null )
            //{
            //    DebugLog.LogWarning( $"指定のクリップが存在しません {clumpKey}, {key}" );
            //    return;
            //}
            
            //data.SetData( key, volume, clipInfo.volume, m_MasterVolume * m_VolumeVOICE );
            //data.Play( clipInfo.clip, false );
            
            m_VOICEList.Add( data );
        }
        
        public void StopVOICE( string key )
        {
            PlayData? data = null;
            for( int i = 0; i < m_VOICEList.Count; ++i )
            {
                var tmp = m_VOICEList[i];
                if( tmp.Key == key )
                {
                    data = tmp;
                    break;
                }
            }

            if( data != null )
            {
                _StopVOICE( data );
                m_VOICEList.Remove( data );
            }
        }

        public void StopVOICEAll()
        {
            for( int i = 0; i < m_VOICEList.Count; ++i )
            {
                _StopVOICE( m_VOICEList[i] );
            }
            
            m_VOICEList.Clear();
        }
        
        private void _StopVOICE( PlayData data )
        {
            data.Stop().Forget();
            m_VOICEPool.Return( data );
        }
        
        //private ResourceManager.LoadInfo<SoundClump>? _GetVoiceLoadInfo( string clumpKey )
        //{
        //    if( string.IsNullOrEmpty( clumpKey ) )
        //    {
        //        return null;
        //    }
            
        //    for( int i = 0; i < m_VoiceClumpLoadInfos.Count; ++i )
        //    {
        //        var loadInfo = m_VoiceClumpLoadInfos[i];
        //        if( loadInfo.AdrsKey == clumpKey )
        //        {
        //            return loadInfo;
        //        }
        //    }
            
        //    return null;
        //}
        
        //private SoundClump.ClipInfo? _GetVoiceAudioClip( string clumpKey, string key )
        //{
        //    var loadInfo = _GetVoiceLoadInfo( clumpKey );
        //    if( loadInfo == null )
        //    {
        //        return null;
        //    }
            
        //    if( loadInfo.Result.Dict.TryGetValue( key, out var info ) )
        //    {
        //        return info;
        //    }
            
        //    return null;
        //}
        
        #endregion VOICE

        #region 音量

        /// <summary>
        /// 音量変更 
        /// </summary>
        public void SetMasterVolume( float volume )
        {
            m_MasterVolume = volume;
            
            _ChangedVolumeBGM();
            _ChangedVolumeSE();
            _ChangedVolumeVOICE();
        }
        
        public void SetVolumeBGM( float volume )
        {
            m_VolumeBGM = volume;
            _ChangedVolumeBGM();
        }
        
        private void _ChangedVolumeBGM()
        {
            float tmpVol = m_MasterVolume * m_VolumeBGM * m_VolumeBGMSub;
            for( int i = 0; i < m_BGMList.Count; ++i )
            {
                m_BGMList[i].SetMasterVolume( tmpVol );
            }
        }
        
        public void SetVolumeSE( float volume )
        {
            m_VolumeSE = volume;
            _ChangedVolumeSE();
        }
        
        private void _ChangedVolumeSE()
        {
            float tmpVol = m_MasterVolume * m_VolumeSE;
            for( int i = 0; i < m_SEList.Count; ++i )
            {
                m_SEList[i].SetMasterVolume( tmpVol );
            }
        }

        public void SetVolumeVOICE( float volume )
        {
            m_VolumeVOICE = volume;
            _ChangedVolumeVOICE();
        }
        
        private void _ChangedVolumeVOICE()
        {
            float tmpVol = m_MasterVolume * m_VolumeVOICE;
            for( int i = 0; i < m_VOICEList.Count; ++i )
            {
                m_VOICEList[i].SetMasterVolume( tmpVol );
            }
        }

        #endregion 音量
        
        #region Editor
        #if UNITY_EDITOR
    
        [CustomEditor( typeof(SoundManager ) )]
        public class Editor_SoundManager : Editor
        {
            public override void OnInspectorGUI()
            {
                GUI.enabled = false;
                base.OnInspectorGUI();
                GUI.enabled = true;

                var tmp = target as SoundManager;
                if( tmp == null )
                {
                    return;
                }
            
                if( GUILayout.Button( "セットアップ" ) )
                {
                    _AddAudioSource( tmp.transform, "BGM",      ref tmp.m_BGMSourceTbl,   PLAY_BGM_COUNT    );
                    _AddAudioSource( tmp.transform, "SE",       ref tmp.m_SESourceTbl,    PLAY_SE_COUNT     );
                    _AddAudioSource( tmp.transform, "VOICE",    ref tmp.m_VOICESourceTbl, PLAY_VOICE_COUNT  );
                }
            }

            private void _AddAudioSource( Transform selfTrans, string name, ref AudioSource?[] tbl , int num )
            {
                var trans = selfTrans.Find( name );
                if( trans == null )
                {
                    var gobj = new GameObject( name );
                    trans = gobj.transform;
                    trans.SetParent( selfTrans );
                }
            
                var tmpTbl = trans.GetComponents<AudioSource>();
                var addNum = num - tmpTbl.Length;
                if( addNum > 0 )
                {
                    for( int i = 0; i < addNum; ++i )
                    {
                        trans.gameObject.AddComponent<AudioSource>();
                    }
                    
                    tmpTbl = trans.GetComponents<AudioSource>();
                }

                foreach( var audioSource in tmpTbl )
                {
                    audioSource.playOnAwake = false;
                }
                
                tbl = tmpTbl;
            }
        }
    
        #endif // UNITY_EDITOR
        #endregion Editor
    }
}
#nullable disable
