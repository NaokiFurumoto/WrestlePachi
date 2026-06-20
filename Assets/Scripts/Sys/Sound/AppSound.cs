#nullable enable
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameSys
{
    /// <summary>
    /// ゲーム固有サウンドのファサード。
    ///
    /// SE / BGM ともに AppSoundData のフィールド名をキーとして AudioSource を管理する。
    /// Awake 時にリフレクションで自動生成し、以降はキー文字列で個別制御可能。
    ///
    /// SE  ("se"  始まり): PlayOneShot で再生。重ね再生可。
    /// BGM ("bgm" 始まり): loop=true、フェードイン/アウトで切り替え。
    /// </summary>
    public sealed class AppSound : BehaviourSingleton<AppSound>
    {
        [SerializeField] private AppSoundData? _data;

        private readonly Dictionary<string, AudioSource> _seSources  = new();
        private readonly Dictionary<string, AudioSource> _bgmSources = new();
        private AudioSource?             _currentBGM;
        private string                   _currentBGMKey = string.Empty;
        private CancellationTokenSource? _bgmCts;

        protected override void _SetInstance() => s_Instance = this;

        protected override void _OnAwake()
        {
            base._OnAwake();
            BuildAudioSources();
        }

        // AppSoundData の "se" / "bgm" で始まる AudioClip フィールドをフィールド名キーで管理する
        // SE は [SE] GO、BGM は [BGM] GO にまとめてアタッチ（GameObject 数を最小化）
        private void BuildAudioSources()
        {
            if( _data == null ) return;

            var seParent  = new GameObject( "[SE]"  );
            var bgmParent = new GameObject( "[BGM]" );
            seParent .transform.SetParent( transform );
            bgmParent.transform.SetParent( transform );

            foreach( var field in typeof(AppSoundData).GetFields( BindingFlags.Public | BindingFlags.Instance ) )
            {
                if( field.FieldType != typeof(AudioClip) ) continue;

                var isBGM = field.Name.StartsWith( "bgm", System.StringComparison.Ordinal );
                var isSE  = field.Name.StartsWith( "se",  System.StringComparison.Ordinal );
                if( !isBGM && !isSE ) continue;

                var clip = field.GetValue( _data ) as AudioClip;
                if( clip == null ) continue;

                var src         = ( isBGM ? bgmParent : seParent ).AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.clip        = clip;

                if( isBGM )
                {
                    src.loop   = true;
                    src.volume = 0f;
                    _bgmSources[field.Name] = src;
                }
                else
                {
                    _seSources[field.Name] = src;
                }
            }
        }

        // ─── SE ヘルパー ───────────────────────────────────────────────────
        private static void PlaySE( string key, float volume = 1f )
        {
            if( !isValid ) return;
            if( Instance._seSources.TryGetValue( key, out var src ) && src.clip != null )
                src.PlayOneShot( src.clip, volume * App.GameSettings.VolumeSE );
        }

        // ─── BGM ヘルパー ──────────────────────────────────────────────────
        private static void PlayBGM( string key, float fadeOut = 0.5f )
        {
            if( !isValid ) return;
            Instance.PlayBGMInternal( key, fadeOut );
        }

        // ─── BGM 内部処理 ──────────────────────────────────────────────────
        private void PlayBGMInternal( string key, float fadeOutDuration )
        {
            if( _currentBGMKey == key ) return;  // 同じ BGM は再リクエストしない
            _bgmCts?.Cancel();
            _bgmCts?.Dispose();
            _bgmCts = CancellationTokenSource.CreateLinkedTokenSource( destroyCancellationToken );
            CrossFadeBGMAsync( key, fadeOutDuration, _bgmCts.Token ).Forget();
        }

        private async UniTaskVoid CrossFadeBGMAsync( string key, float fadeOutDuration, CancellationToken ct )
        {
            if( _currentBGM != null && _currentBGM.isPlaying )
            {
                await FadeVolumeAsync( _currentBGM, 0f, fadeOutDuration, ct );
                if( ct.IsCancellationRequested ) return;
                _currentBGM.Stop();
            }
            _currentBGM    = null;
            _currentBGMKey = string.Empty;

            if( !_bgmSources.TryGetValue( key, out var newSrc ) ) return;

            newSrc.volume  = 0f;
            newSrc.Play();
            _currentBGM    = newSrc;
            _currentBGMKey = key;
            await FadeVolumeAsync( newSrc, App.GameSettings.VolumeBGM, 0.5f, ct );
        }

        private void FadeOutBGMInternal( float duration )
        {
            _bgmCts?.Cancel();
            _bgmCts?.Dispose();
            _bgmCts = CancellationTokenSource.CreateLinkedTokenSource( destroyCancellationToken );
            FadeOutCurrentAsync( duration, _bgmCts.Token ).Forget();
        }

        private async UniTaskVoid FadeOutCurrentAsync( float duration, CancellationToken ct )
        {
            if( _currentBGM == null || !_currentBGM.isPlaying ) return;
            await FadeVolumeAsync( _currentBGM, 0f, duration, ct );
            if( ct.IsCancellationRequested ) return;
            _currentBGM.Stop();
            _currentBGM    = null;
            _currentBGMKey = string.Empty;
        }

        private static async UniTask FadeVolumeAsync( AudioSource src, float target, float duration, CancellationToken ct )
        {
            if( duration <= 0f ) { src.volume = target; return; }
            var start   = src.volume;
            var elapsed = 0f;
            while( elapsed < duration )
            {
                if( ct.IsCancellationRequested ) return;
                elapsed   += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp( start, target, Mathf.Clamp01( elapsed / duration ) );
                await UniTask.Yield();
            }
            if( !ct.IsCancellationRequested ) src.volume = target;
        }

        // ─── BGM API ───────────────────────────────────────────────────────
        public static void PlayBGMGame()     => PlayBGM( "bgmGame" );
        public static void PlayBGMTense()    => PlayBGM( "bgmTense" );
        public static void PlayBGMClear()    => PlayBGM( "bgmClear" );
        public static void PlayBGMGameOver() => PlayBGM( "bgmGameOver" );

        public static void FadeOutBGM( float duration = 2f )
        {
            if( !isValid ) return;
            Instance.FadeOutBGMInternal( duration );
        }

        public static void SetBGMVolume( float volume )
        {
            if( !isValid ) return;
            if( Instance._currentBGM != null )
                Instance._currentBGM.volume = volume;
        }

        // ─── SE API - UI ───────────────────────────────────────────────────
        public static void PlayBtnOk()      => PlaySE( "seBtnOk" );
        public static void PlayBtnCancel()  => PlaySE( "seBtnCancel" );
        public static void PlayOpenWindow() => PlaySE( "seOpenWindow" );
        public static void PlayTap()        => PlaySE( "seTap" );

        // ─── SE API - ゲーム進行 ────────────────────────────────────────────
        public static void PlayGong()      => PlaySE( "seGong" );
        public static void PlayClearGame() => PlaySE( "seGameClear" );
        public static void PlayGameOver()  => PlaySE( "seGameOver" );
        public static void PlayTimer()     => PlaySE( "seTimer" );

        // ─── SE API - ぷよ操作 ──────────────────────────────────────────────
        public static void PlayPuyoMove()   => PlaySE( "sePuyoMove" );
        public static void PlayPuyoRotate() => PlaySE( "sePuyoRotate" );
        public static void PlayPuyoLand()   => PlaySE( "sePuyoLand" );
        public static void PlayOjamaLand()  => PlaySE( "seOjamaLand" );
        public static void PlayPuyoFlash()  => PlaySE( "sePuyoFlash" );
        public static void PlayPuyoClear()  => PlaySE( "sePuyoClear" );
        public static void PlayChain()      => PlaySE( "seChain" );

        // ─── SE API - パチンコ ──────────────────────────────────────────────
        public static void PlayHesoIn()                        => PlaySE( "seHesoIn" );
        public static void PlayBallLaunch( float volume = 1f ) => PlaySE( "seBallLaunch",  volume );
        public static void PlayBallNailHit( float volume = 1f ) => PlaySE( "seBallNailHit", volume );
        public static void PlayHoldAdd()                       => PlaySE( "seHoldAdd" );

        // ─── SE API - スキル・戦闘 ──────────────────────────────────────────
        public static void PlayCutIn()       => PlaySE( "seCutIn" );
        public static void PlayDamageHit()   => PlaySE( "seDamageHit" );
        public static void PlayEnemyDefeat() => PlaySE( "seEnemyDefeat" );
        public static void PlayBom()         => PlaySE( "seBom" );
    }
}
#nullable disable
