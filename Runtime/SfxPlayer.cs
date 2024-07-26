using System;
using System.Collections.Generic;
using AudioPlayerService.Runtime.Configs;
using R3;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace AudioPlayerService.Runtime
{
    public class SfxPlayer : IInitializable, IDisposable
    {
        private static readonly Subject<PlaySfxEvent> PlaySubject = new Subject<PlaySfxEvent>();
        private static readonly Subject<Unit> StopAllSubject = new Subject<Unit>();
        private static readonly Subject<string> StopSubject = new Subject<string>();
        private static readonly Subject<Tuple<string, bool>> SetMuteSubject = new Subject<Tuple<string, bool>>();
        private static readonly Subject<Tuple<string, float>> ChangeVolumeSubject = new Subject<Tuple<string, float>>();

        private static Dictionary<string, SfxConfig> _sounds;
        private static Dictionary<int, Dictionary<int, PlayingSfx>> _playingSfx;

        private static SfxPlayerConfig _config;
        private readonly LinkedList<AudioSource> _sourcesPool;
        private readonly CompositeDisposable _disposable;

        private GameObject _sourcesHolder;

        private Subject<SfxConfig> _playInternalSubject;
        private Subject<SfxConfig> _stopInternalSubject;
        private Subject<Tuple<string, float>> _volumeChangeInternalSubject;


        public Observable<SfxConfig> PlayStream => _playInternalSubject;
        public Observable<SfxConfig> StopStream => _stopInternalSubject;
        public Observable<Tuple<string, float>> VolumeChangeStream => _volumeChangeInternalSubject;


        public SfxPlayer(SfxPlayerConfig config)
        {
            _disposable = new CompositeDisposable();
            _config = config;
            _sourcesPool = new LinkedList<AudioSource>();
            _playingSfx = new Dictionary<int, Dictionary<int, PlayingSfx>>();

            _sounds = new Dictionary<string, SfxConfig>();
            AddSfxPlayerConfigSounds(_config.SoundsConfigs);

            _playInternalSubject = new Subject<SfxConfig>();
            _playInternalSubject.AddTo(_disposable);
            _stopInternalSubject = new Subject<SfxConfig>();
            _stopInternalSubject.AddTo(_disposable);
            _volumeChangeInternalSubject = new Subject<Tuple<string, float>>();
            _volumeChangeInternalSubject.AddTo(_disposable);
        }

        /// <summary>
        /// This method calls by Zenject
        /// </summary>
        void IInitializable.Initialize()
        {
            _sourcesHolder = new GameObject("AudioPlayer");
            Object.DontDestroyOnLoad(_sourcesHolder);
            AudioSource source = GetFreeSource();
            _sourcesPool.AddLast(source);

            PlaySubject.Subscribe(PlayInternal).AddTo(_disposable);
            StopAllSubject.Subscribe(_ => StopAllInternal()).AddTo(_disposable);
            StopSubject.Subscribe(StopInternal).AddTo(_disposable);

            SetMuteSubject.Subscribe(tuple =>
            {
                string channel = tuple.Item1;
                bool mute = tuple.Item2;
                SetMuteInternal(channel, mute);
            }).AddTo(_disposable);

            ChangeVolumeSubject.Subscribe(tuple =>
            {
                string channel = tuple.Item1;
                float vol = tuple.Item2;
                ChangeVolumeInternal(channel, vol);
            }).AddTo(_disposable);
        }

        /// <summary>
        /// Adds SFX Configs to the player.
        /// </summary>
        /// <param name="configs">SFX configs to add</param>
        public static void AddSfxPlayerConfigSounds(SfxConfig[] configs)
        {
            foreach (SfxConfig sfxConfig in configs)
            {
                _sounds[sfxConfig.name] = sfxConfig;
            }
        }

        /// <summary>
        /// Plays sound by name of the clip.
        /// </summary>
        /// <param name="config">SFX Config of the clip</param>
        /// <param name="overlaid">If true - the same sound can be played multiple times, if false - stops all the same playing sounds and play the given one.</param>
        /// <param name="onEnd">Callback of finish playing the given audio.</param>
        /// <param name="worldPos">The position of the sound in 3D space, if null </param>
        public static void Play(SfxConfig config, bool overlaid = true, Action onEnd = null, Vector3? worldPos = null)
        {
            Play(config.name, overlaid, onEnd, worldPos);
        }

        /// <summary>
        /// Plays sound by name of the clip.
        /// </summary>
        /// <param name="sound">Name of the clip</param>
        /// <param name="overlaid">If true - the same sound can be played multiple times, if false - stops all the same playing sounds and play the given one.</param>
        /// <param name="onEnd">Callback of finish playing the given audio.</param>
        /// <param name="worldPos">The position of the sound in 3D space, if null </param>
        public static void Play(string sound, bool overlaid = true, Action onEnd = null, Vector3? worldPos = null)
        {
            PlaySubject?.OnNext(new PlaySfxEvent
            {
                Sound = sound,
                Overlaid = overlaid,
                OnEnd = onEnd,
                WorldPos = worldPos,
            });
        }

        /// <summary>
        /// Check if the given sound is playing.
        /// </summary>
        /// <param name="config">The SFX Config of the sound.</param>
        /// <returns>true - if playing, false - if not.</returns>
        public static bool IsPlaying(SfxConfig config)
        {
            return _playingSfx.ContainsKey(config.GetInstanceID());
        }

        /// <summary>
        /// Check if the given sound is playing.
        /// </summary>
        /// <param name="sound">The name of the sound.</param>
        /// <returns>true - if playing, false - if not.</returns>
        public static bool IsPlaying(string sound)
        {
            if (!_sounds.TryGetValue(sound, out SfxConfig config))
            {
                Debug.LogError($"There is not sound with name : {sound}! Please check your configs.");
                return false;
            }

            return IsPlaying(config);
        }

        /// <summary>
        /// Stops all playing sources.
        /// </summary>
        public static void StopAll()
        {
            StopAllSubject?.OnNext(Unit.Default);
        }

        /// <summary>
        /// Stop playing audio by config of the clip.
        /// </summary>
        /// <param name="config">SFX config</param>
        public static void Stop(SfxConfig config)
        {
            Stop(config.ClipName);
        }

        /// <summary>
        /// Stop playing audio by name of the clip.
        /// </summary>
        /// <param name="sound">Name of the clip</param>
        public static void Stop(string sound)
        {
            StopSubject?.OnNext(sound);
        }

        /// <summary>
        /// Mute/Unmute some Channel/Group of audio mixer
        /// </summary>
        /// <param name="channel">Name of the Exposed Parameter that you assigned to some of the Groups</param>
        /// <param name="mute">if true - mute, if false - unmute</param>
        public static void SetMute(string channel, bool mute)
        {
            SetMuteSubject?.OnNext(new Tuple<string, bool>(channel, mute));
        }

        /// <summary>
        /// Modify volume of the audio inside of 0-1 range.
        /// </summary>
        /// <param name="channel">Name of the Exposed Parameter that you assigned to some of the Groups</param>
        /// <param name="volume">Input volume, from 0 to 1(f)</param>
        public static void ChangeVolume(string channel, float volume)
        {
            ChangeVolumeSubject?.OnNext(new Tuple<string, float>(channel, volume));
        }

        /// <summary>
        /// Get current volume of the audio channel inside of 0-1 range.
        /// </summary>
        /// <param name="channel">Name of the Exposed Parameter that you assigned to some of the Groups</param>
        public static float GetVolume(string channel)
        {
            _config.MasterMixer.GetFloat(channel, out float currentValue);
            float normalizedValue = Mathf.InverseLerp(-40f, 0, currentValue);
            return Mathf.Clamp01(normalizedValue);
        }

        /// <summary>
        /// Play any audio source by config.
        /// </summary>
        /// <param name="playEvent">Audio playing event, data of playing sound.</param>
        /// <returns>true - if source played successfully.</returns>
        private void PlayInternal(PlaySfxEvent playEvent)
        {
            if (!_sounds.TryGetValue(playEvent.Sound, out SfxConfig config))
            {
                Debug.LogError($"There is not sound to Play with name : {playEvent.Sound}! Please check your configs.");
                playEvent.OnEnd?.Invoke();
                return;
            }

            PlayInternal(config, playEvent.Overlaid, playEvent.OnEnd, playEvent.WorldPos);
        }

        /// <summary>
        /// Play any audio source by config.
        /// </summary>
        /// <param name="config">SFX config of sound that you need to play</param>
        /// <param name="overlaid">If true - the same sound can be played multiple times, if false - stops all the same playing sounds and play the given one.</param>
        /// <param name="onEnd">Callback of finish playing the given audio.</param>
        /// <param name="playEventWorldPos"></param>
        /// <returns>true - if source played successfully.</returns>
        /// <param name="worldPos">The position of the sound in 3D space, if null </param>
        private void PlayInternal(SfxConfig config, bool overlaid = true, Action onEnd = null, Vector3? worldPos = null)
        {
            if (config == null)
            {
                Debug.LogError("Config is null!");
                return;
            }

            int id = config.GetInstanceID();
            if (!_playingSfx.TryGetValue(id, out Dictionary<int, PlayingSfx> playingSfxes))
            {
                playingSfxes = new Dictionary<int, PlayingSfx>(3);
                _playingSfx[id] = playingSfxes;
            }
            else if (!overlaid)
            {
                StopInternal(config);
                playingSfxes = new Dictionary<int, PlayingSfx>(3);
                _playingSfx[id] = playingSfxes;
            }
            else if (config.MaxConcurrentSources > 0 && playingSfxes.Count >= config.MaxConcurrentSources)
            {
                return;
            }

            AudioSource source = PlaySource(config);
            if (!worldPos.HasValue)
            {
                source.transform.localPosition = Vector3.zero;
            }
            else
            {
                source.transform.position = worldPos.Value;
            }

            int sourceId = Mathf.Abs(source.GetInstanceID());

            PlayingSfx sfx = new PlayingSfx(source, EndPlayHandler(source, sourceId, config, onEnd));

            playingSfxes.Add(sourceId, sfx);
#if LOG_AUDIO
            Debug.Log($"<color=green>Play {config.name} sfx</color>");
#endif
            _playInternalSubject?.OnNext(config);
        }

        /// <summary>
        /// Stops all playing sources.
        /// </summary>
        private void StopAllInternal()
        {
            foreach (KeyValuePair<int, Dictionary<int, PlayingSfx>> sfxes in _playingSfx)
            {
                foreach (KeyValuePair<int, PlayingSfx> sfx in sfxes.Value)
                {
                    sfx.Value.Dispose();
                    ConsumeSource(sfx.Value.Source);
                }

                sfxes.Value.Clear();
            }

            _playingSfx.Clear();
        }

        /// <summary>
        /// Stop playing audio source and put it to the pool.
        /// </summary>
        /// <param name="sound">Name of the playing sound, this can be taken from auto-generated Sounds.cs</param>
        private void StopInternal(string sound)
        {
            if (!_sounds.TryGetValue(sound, out SfxConfig config))
            {
                Debug.LogError($"There is not sound to Stop with name : {sound}! Please check your configs.");
                return;
            }

            StopInternal(config);
        }

        /// <summary>
        /// Stop playing audio source and put it to the pool.
        /// </summary>
        /// <param name="config">Config of source that you want to stop</param>
        /// <param name="sfxId">if you have multiple sources with same config, set Instance Id to stop specific one or -1 to stop all</param>
        private void StopInternal(SfxConfig config, int sfxId = -1)
        {
            int id = config.GetInstanceID();
            if (!_playingSfx.TryGetValue(id, out Dictionary<int, PlayingSfx> sfxs))
            {
                Debug.LogWarning($"There is not any source with config ID : {id}");
                return;
            }

            if (sfxId < 0)
            {
                foreach (KeyValuePair<int, PlayingSfx> sfxPair in sfxs)
                {
                    sfxPair.Value.Dispose();
                    ConsumeSource(sfxPair.Value.Source);
#if LOG_AUDIO
                    Debug.Log($"<color=yellow>Stop {config.name} sfx</color>");
#endif
                    _stopInternalSubject?.OnNext(config);
                }

                sfxs.Clear();
                _playingSfx.Remove(id);
                return;
            }

            if (!sfxs.TryGetValue(sfxId, out PlayingSfx sfx))
            {
                Debug.LogError($"There is not audio source with Instance Id : {sfxId}!");
                return;
            }

            sfx.Dispose();
            ConsumeSource(sfx.Source);
            sfxs.Remove(sfxId);
#if LOG_AUDIO
            Debug.Log($"<color=yellow>Stop {config.name} sfx</color>");
#endif
            _stopInternalSubject?.OnNext(config);
            if (sfxs.Count <= 0)
            {
                _playingSfx.Remove(id);
            }
        }

        /// <summary>
        /// Mute/Unmute some Channel/Group of audio mixer
        /// </summary>
        /// <param name="channel">Name of the Exposed Parameter that you assigned to some of the Groups</param>
        /// <param name="mute">if true - mute, if false - unmute</param>
        private void SetMuteInternal(string channel, bool mute)
        {
            int volume = mute ? -80 : 0;
            _config.MasterMixer.SetFloat(channel, volume);
#if LOG_AUDIO
            Debug.Log($"<color=cyan>Set {channel} channel is muted : {mute}</color>");
#endif
        }

        /// <summary>
        /// Modify volume of the audio inside of 0-1 range.
        /// </summary>
        /// <param name="channel">Name of the Exposed Parameter that you assigned to some of the Groups</param>
        /// <param name="volume">Input volume, from 0 to 1(f)</param>
        private void ChangeVolumeInternal(string channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            float value = Mathf.Lerp(-40f, 0, volume);
            value = value <= -40f ? -80 : value;
            _config.MasterMixer.SetFloat(channel, value);
            _volumeChangeInternalSubject?.OnNext(new Tuple<string, float>(channel, volume));
        }

        /// <summary>
        /// Put used audio source back to pool.
        /// </summary>
        /// <param name="source">Source to consume.</param>
        private void ConsumeSource(AudioSource source)
        {
            StopSource(source);
            _sourcesPool.AddLast(source);
        }

        /// <summary>
        /// Get free source from pool if there is any source, if not add one more to get.
        /// </summary>
        /// <returns>Free source</returns>
        private AudioSource GetFreeSource()
        {
            AudioSource source;
            if (_sourcesPool.Count > 0)
            {
                source = _sourcesPool.First.Value;
                _sourcesPool.RemoveFirst();
                return source;
            }

            GameObject sourceHolder = new GameObject("SourceHolder");
            sourceHolder.transform.SetParent(_sourcesHolder.transform);
            source = sourceHolder.AddComponent<AudioSource>();
            return source;
        }

        /// <summary>
        /// Get free audio source from pool, setup values from config and play.
        /// </summary>
        /// <param name="config">SFX config of that you want to play</param>
        /// <returns>Ready and playing audio source</returns>
        private AudioSource PlaySource(SfxConfig config)
        {
            AudioSource source = GetFreeSource();

            source.clip = config.Clip;
            source.outputAudioMixerGroup = config.MixerGroup;
            source.volume = config.Volume;
            source.pitch = config.Pitch;
            source.loop = config.Loop;

            source.panStereo = config.StereoPan;
            source.spatialBlend = config.SpecialBlend;
            source.reverbZoneMix = config.ReverbZoneMix;

            source.dopplerLevel = config.DopplerLevel;
            source.spread = config.Spread;
            source.rolloffMode = config.VolumeRolloff;
            source.minDistance = config.RadiusMin;
            source.maxDistance = config.RadiusMax;

#if UNITY_EDITOR
            source.gameObject.name = config.name;
#endif
            source.gameObject.SetActive(true);
            source.Play();

            return source;
        }

        /// <summary>
        /// Stops source if it's playing.
        /// </summary>
        /// <param name="source">Source that you need to stop</param>
        /// <returns>if source stopped successfully returns true, if not - false</returns>
        private bool StopSource(AudioSource source)
        {
            if (!source.isPlaying) return false;
            source.gameObject.SetActive(false);
            source.Stop();
            return true;
        }

        /// <summary>
        /// Handles the finish of the playing.
        /// </summary>
        /// <param name="source">Audio source.</param>
        /// <param name="instanceId">Id of the special source.</param>
        /// <param name="config">SFX Config.</param>
        /// <param name="onEnd">End playing callback</param>
        /// <returns></returns>
        private IDisposable EndPlayHandler(AudioSource source, int instanceId, SfxConfig config, Action onEnd)
        {
            Subject<Unit> handler = new Subject<Unit>();
            handler.Subscribe(_ =>
            {
                if (source == null || source.isPlaying) return;
                StopInternal(config, instanceId);
                onEnd?.Invoke();
            }).AddTo(_disposable);
            IDisposable stream = Observable.EveryUpdate(UnityFrameProvider.Update)
                .Subscribe(_ => { handler.OnNext(Unit.Default); });
            stream.AddTo(_disposable);
            return stream;
        }

        /// <summary>
        /// This method calls by Zenject.
        /// </summary>
        void IDisposable.Dispose()
        {
            _disposable?.Dispose();
            if (_sourcesHolder != null)
            {
                Object.Destroy(_sourcesHolder);
            }
        }
    }
}