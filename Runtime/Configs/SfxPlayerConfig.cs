using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace AudioPlayerService.Runtime.Configs
{
    public class SfxPlayerConfig : ScriptableObject
    {
        [Tooltip("This Audio mixer is used as the base mixer of the playing sources.")] [SerializeField]
        private AudioMixer masterMixer;

        [Tooltip("Put SFX Config here if you want to play it.")] [SerializeField]
        private SfxConfig[] soundsConfigs;
#if UNITY_EDITOR
        [Tooltip("Put SFX Configs folder here to auto fill configs.")] [Space, SerializeField]
        public Object configFolder;

        [HideInInspector, SerializeField] public bool showLogs;
#endif


        public AudioMixer MasterMixer => masterMixer;
        public SfxConfig[] SoundsConfigs => soundsConfigs;

        public void AddOrUpdateConfigs(List<SfxConfig> configs)
        {
            List<SfxConfig> newConfigs = new List<SfxConfig>(configs.Count + soundsConfigs.Length);
            newConfigs.AddRange(soundsConfigs);
            foreach (SfxConfig config in configs)
            {
                if (newConfigs.Contains(config)) continue;
                newConfigs.Add(config);
            }

            soundsConfigs = newConfigs.ToArray();
        }
    }
}