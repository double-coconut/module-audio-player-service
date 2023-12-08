using System.Collections.Generic;
using System.IO;
using AudioPlayerService.Runtime.Configs;
using AudioPlayerService.Runtime.Helpers;
using UnityEditor;
using UnityEngine;

namespace AudioPlayerService.Editor
{
    [CustomEditor(typeof(SfxPlayerConfig))]
    [CanEditMultipleObjects]
    public class SfxPlayerEditor : UnityEditor.Editor
    {
        private const string Logging = "LOG_AUDIO";
        internal const string Path = "Assets/AudioPlayer";
        internal const string FolderName = "Resources";

        internal const string FileName = "SfxPlayerConfig.asset";
        // private static Object _configsFolder;

        internal static readonly BuildTargetGroup[] Platforms =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Standalone,
            BuildTargetGroup.WebGL,
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            EditorGUILayout.Space();
            SfxPlayerConfig sfxPlayerConfig = serializedObject.targetObject as SfxPlayerConfig;
            if (sfxPlayerConfig == null)
            {
                Debug.LogError("The SfxPlayerConfig not found!");
                return;
            }

            if (sfxPlayerConfig.MasterMixer == null)
            {
                EditorGUILayout.HelpBox("Please first of all setup an AudioMixer", MessageType.Error);
            }

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                richText = true
            };

            //_configsFolder = EditorGUILayout.ObjectField("Configs Folder",_configsFolder, typeof(Object), false);
            var configsFolder = sfxPlayerConfig.configFolder;
            if (configsFolder != null)
            {
                string path = AssetDatabase.GetAssetPath(configsFolder);
                if (!Directory.Exists(path))
                {
                    EditorGUILayout.HelpBox("This is not a folder!", MessageType.Error);
                }
                else
                {
                    string[] files = Directory.GetFiles(path, "*.asset");
                    if (files?.Length > 0)
                    {
                        if (GUILayout.Button(new GUIContent("Fill <b><color=orange>Sound Configs</color></b>",
                                "Press this button to Fill all sound configs from given folder!"), buttonStyle))
                        {
                            List<SfxConfig> configs = new List<SfxConfig>(files.Length);
                            foreach (string file in files)
                            {
                                SfxConfig config = AssetDatabase.LoadAssetAtPath<SfxConfig>(file);
                                if (config != null)
                                {
                                    configs.Add(config);
                                }
                            }

                            if (configs.Count > 0)
                            {
                                sfxPlayerConfig.AddOrUpdateConfigs(configs);
                                EditorUtility.SetDirty(sfxPlayerConfig);
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("There is not any config file at this folder!", MessageType.Error);
                    }
                }
            }


            if (sfxPlayerConfig.SoundsConfigs?.Length > 0)
            {
                if (GUILayout.Button(new GUIContent("Generate <b><color=yellow>Sounds.cs</color></b>",
                            "Press this button to setup all configs and generate Sounds.cs to have names of the clips ready to use!"),
                        buttonStyle))
                {
                    GenerateSoundsList();
                }
            }

            bool loggingEnabled = sfxPlayerConfig.showLogs;
            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent(loggingEnabled ? "Disable Logs" : "Enable Logs")))
            {
                sfxPlayerConfig.showLogs = !loggingEnabled;
                EnableLogging(sfxPlayerConfig.showLogs);
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateSoundsList()
        {
            SfxPlayerConfig sfxPlayerConfig = serializedObject.targetObject as SfxPlayerConfig;

            if (sfxPlayerConfig == null) return;
            List<string> fields = new List<string>(sfxPlayerConfig.SoundsConfigs.Length);
            List<string> names = new List<string>(sfxPlayerConfig.SoundsConfigs.Length);

            for (int i = 0; i < sfxPlayerConfig.SoundsConfigs.Length; i++)
            {
                fields.Add(sfxPlayerConfig.SoundsConfigs[i].name);
                names.Add(sfxPlayerConfig.SoundsConfigs[i].name);
            }

            FieldsGenerator.GenerateFields("AudioPlayer", "Sounds",
                System.IO.Path.Combine(Path, "Scripts/"),
                fields, names);
            foreach (BuildTargetGroup buildTarget in Platforms)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, "AUDIO_PLAYER");
            }
        }

        private void EnableLogging(bool showLogs)
        {
            PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out string[] symbols);

            if (showLogs)
            {
                List<string> symbolsList = new List<string>(symbols.Length + 1) { Logging };
                symbolsList.AddRange(symbols);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbolsList.ToArray());
            }
            else
            {
                List<string> symbolsList = new List<string>(symbols.Length - 1);
                symbolsList.AddRange(symbols);
                symbolsList.Remove(Logging);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbolsList.ToArray());
            }
        }

        // [MenuItem("Custom Tools/Audio/Open SFX Player")]
        // private static void CreatePresenter()
        // {
        //     if (!AssetDatabase.IsValidFolder(System.IO.Path.Combine(Path, FolderName)))
        //     {
        //         AssetDatabase.CreateFolder(Path, FolderName);
        //     }
        //
        //     SfxPlayerConfig asset =
        //         AssetDatabase.LoadAssetAtPath<SfxPlayerConfig>(System.IO.Path.Combine(Path, FolderName, FileName));
        //     if (asset == null)
        //     {
        //         asset = CreateInstance<SfxPlayerConfig>();
        //         AssetDatabase.CreateAsset(asset, System.IO.Path.Combine(Path, FolderName, FileName));
        //         AssetDatabase.SaveAssets();
        //
        //         FieldsGenerator.GenerateFields("AudioPlayer", "Sounds",
        //             System.IO.Path.Combine(Path, "Scripts/"), new List<string>(0), new List<string>(0));
        //         foreach (BuildTargetGroup buildTarget in Platforms)
        //         {
        //             PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, "AUDIO_PLAYER");
        //         }
        //     }
        //
        //     EditorUtility.FocusProjectWindow();
        //     Selection.activeObject = asset;
        // }
    }
}