using UnityEngine;
using UnityEngine.Audio;

namespace Services.AudioPlayer.Configs
{
    [CreateAssetMenu(fileName = "Sound", menuName = "Effects/SFX Config")]
    public class SfxConfig : ScriptableObject
    {
        [Space, Header("Sound Parameters")] [SerializeField]
        private AudioClip clip;

        [SerializeField] private bool getRandomClip = false;
        [SerializeField] private AudioClip[] clips;


        [SerializeField] private AudioMixerGroup mixerGroup;

        [Range(0, 2f), SerializeField] private float volume = 1f;
        [Range(-3f, 3f), SerializeField] private float pitch = 1f;

        [SerializeField] private bool loop;
        [SerializeField] private int maxConcurrentSources = -1;

        [Range(-1f, 1f), SerializeField] private float stereoPan = 0;
        [Range(0, 1f), SerializeField] private float specialBlend = 0;
        [Range(0, 1.1f), SerializeField] private float reverbZoneMix = 0;

        [Header("3D Sound Settings")] [Range(0, 5f), SerializeField]
        private float dopplerLevel = 1f;

        [Range(0, 360), SerializeField] private int spread = 0;
        [SerializeField] private AudioRolloffMode volumeRolloff = AudioRolloffMode.Logarithmic;
        [Range(0.01f, 1000f), SerializeField] private float radiusMin = 1f;
        [Range(0.01f, 1000f), SerializeField] private float radiusMax = 100f;


        public AudioClip Clip => getRandomClip && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : clip;
        public string ClipName => Clip != null ? clip.name : "empty";
        public AudioMixerGroup MixerGroup => mixerGroup;
        public float Volume => volume;
        public float Pitch => pitch;
        public bool Loop => loop;
        public int MaxConcurrentSources => maxConcurrentSources;
        public float StereoPan => stereoPan;
        public float SpecialBlend => specialBlend;
        public float ReverbZoneMix => reverbZoneMix;
        public float DopplerLevel => dopplerLevel;
        public int Spread => spread;
        public AudioRolloffMode VolumeRolloff => volumeRolloff;
        public float RadiusMin => radiusMin;
        public float RadiusMax => radiusMax;


        public void Play()
        {
            SfxPlayer.Play(this);
        }

        public void Stop()
        {
            SfxPlayer.Stop(this);
        }
    }
}