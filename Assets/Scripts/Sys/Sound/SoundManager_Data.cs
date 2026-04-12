#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// サウンド管理
    /// </summary>
    public partial class SoundManager : BehaviourSingleton<SoundManager>
    {
        private enum Type
        {
            None,
            
            BGM,
            SE,
            VOICE,
        }
        
        /// <summary>
        /// 再生データ
        /// </summary>
        private class PlayData
        {
            protected enum Phase
            {
                Play,
                Stopping,
                Stopped,
            }
            
            private     int             m_Id                = 0;
            private     Type            m_Type              = Type.None;
            private     string          m_Key               = string.Empty;
            protected   AudioSource?    m_AudioSrc          = null;
            protected   Phase           m_Phase             = Phase.Stopped;
            
            private     float           m_Elapse            = 0f;
            private     float           m_FadeTime          = 0f;
            private     bool            m_IsBGMVolumeDown   = false;
            
            protected   float           m_Volume            = 1f;
            protected   float           m_DataVolume        = 1f;
            protected   float           m_MasterVolume      = 1f;
            protected   float           m_FadeRate          = 1f;
            
            public  int         Id                  => m_Id;
            public  Type        Type                => m_Type;
            public  string      Key                 => m_Key;
            
            public  bool        IsBGMVolumeDown     => m_IsBGMVolumeDown;
            
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public PlayData()
            {
            }
            
            public void SetParam( int id, Type type, AudioSource? src )
            {
                m_Id = id;
                m_Type  = type;
                m_AudioSrc = src;
            }
            
            public void SetData( string key, float volume, float dataVolume, float masterVolume )
            {
                m_Key = key;
                m_Volume = volume;
                m_DataVolume = dataVolume;
                m_FadeRate = 1.0f;
                
                SetMasterVolume( masterVolume );
            }

            public void SetBgmVolumeDown( bool flag )
            {
                m_IsBGMVolumeDown = flag;
            }
            
            public void Play( AudioClip clip, bool isLoop )
            {
                if( m_Type == Type.BGM )
                {
                    return;
                }
                
                _Play( clip, isLoop );
            }
            
            protected void _Play( AudioClip clip, bool isLoop )
            {
                if( m_AudioSrc == null )
                {
                    return;
                }
                
                m_AudioSrc.clip = clip;
                m_AudioSrc.loop = isLoop;
                
                _UpdateVolume();
                
                if( m_AudioSrc != null )
                {
                    m_AudioSrc.Play();
                }
                
                m_Phase = Phase.Play;
            }
            
            public async UniTask Stop( float fadeTime = 0f )
            {
                if( m_Phase != Phase.Play )
                {
                    return;
                }
                
                m_Phase = Phase.Stopping;
                
                if( fadeTime > 0 )
                {
                    m_FadeTime =
                    m_Elapse   = fadeTime;
                
                    await _Fadeout();
                }
                
                _StopAudioSource();
                
                m_Phase = Phase.Stopped;
            }

            protected void _StopAudioSource()
            {
                if( m_AudioSrc != null )
                {
                    m_AudioSrc.Stop();
                    m_AudioSrc.clip = null;
                }
            }
            
            private async UniTask _Fadeout()
            {
                await  UniTask.Yield();
                
                while( true )
                {
                    m_Elapse -= Time.deltaTime;
                    m_FadeRate = Mathf.Clamp01( m_Elapse / m_FadeTime );
                    _UpdateVolume();

                    if( m_Elapse <= 0 )
                    {
                        break;
                    }
                    
                    await  UniTask.Yield();
                }
            }
            
            public void SetMasterVolume( float masterVol )
            {
                m_MasterVolume = masterVol;
                _UpdateVolume();
            }

            protected void _UpdateVolume()
            {
                if( m_AudioSrc != null )
                {
                    m_AudioSrc.volume = m_MasterVolume * m_Volume * m_DataVolume * m_FadeRate;
                }
            }

            public bool IsPlaying()
            {
                if( m_Phase != Phase.Play )
                {
                    return false;
                }

                if( m_AudioSrc != null )
                {
                    return m_AudioSrc.isPlaying;
                }
                
                return false; 
            }
        }
        
        /// <summary>
        /// 再生データ(BGM)
        /// </summary>
        private class PlayDataBGM : PlayData
        {
            /***
             * AudioSourceをintro用、Loop用の2つ用意
             * 
             * Update(), LateUpdate()のタイミングで
             * AudioSource の IsPlaying() == false を判定して切り替えたが、
             * どちらの場合も、音が途切れてしまった
             * 
             * なので、introがある場合は、introの再生と同時にLoopを delay再生している
             * ->
             * WebGLビルドでdelayが効かない・・・
             * UniTaskでintroの時間遅延させても、途切れが発生する・・・
             * wavデータのsmplチャンクでループポイントを指定できるとのことなので
             * introとloopファイルを結合して、ループポイントを設定する形に変更する
             *
             * 参考：https://github.com/kraiHD/UniWaveLoop
             ***/
            
            // 基底のAudioSrcはイントロ用
            // こちらはLoop用
            private     BGMClump?       m_BGMClump      = null;
            
            /// <summary>
            /// コンストラクタ
            /// </summary>
            public PlayDataBGM() : base()
            {
            }
            
            public void SetParamBGM( int id, AudioSource? src )
            {
                SetParam( id, SoundManager.Type.BGM, src );
                
                // ループ設定は最初に行っておく
                if( m_AudioSrc != null )
                {
                    m_AudioSrc.loop = true;
                }
            }
            
            #region BGM用
            public void SetBGMClump( BGMClump clump )
            {
                m_BGMClump = clump;
                m_DataVolume = clump.volume;
            }
            
            public void ReleaseBGMClump()
            {
                m_BGMClump = null;
            }
            #endregion BGM用
            
            public void PlayBGM()
            {
                if( m_BGMClump == null )
                {
                    return;
                }
                
                var clip = m_BGMClump.clip.clip;
                
                if( clip != null && m_AudioSrc != null )
                {
                    m_AudioSrc.clip = clip;
                }
                
                _UpdateVolume();
                
                if( m_AudioSrc != null )
                {
                    m_AudioSrc.Play();
                }
                
                m_Phase = Phase.Play;
            }
        }
    }
}
#nullable disable
