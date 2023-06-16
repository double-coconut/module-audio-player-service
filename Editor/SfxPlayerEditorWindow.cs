using System.Collections.Generic;
using System.IO;
using AudioPlayerService.Runtime.Configs;
using AudioPlayerService.Runtime.Helpers;
using UnityEditor;
using UnityEngine;

namespace AudioPlayerService.Editor
{
    public class SfxPlayerEditorWindow : EditorWindow
    {
       
        
        [MenuItem("Custom Tools/Audio/Open SFX Player")]
        private static void ShowWindow()
        {
           
            GenerateConfig();

            SfxPlayerEditorWindow window = GetWindow<SfxPlayerEditorWindow>();
            window.titleContent = new GUIContent("SFX Player");
            window.Show();
        }

        private void OnGUI()
        {
            SfxPlayerConfig asset =
                AssetDatabase.LoadAssetAtPath<SfxPlayerConfig>(Path.Combine(SfxPlayerEditor.Path,
                    SfxPlayerEditor.FolderName, SfxPlayerEditor.FileName));
            UnityEditor.Editor editor = null;
            if (asset != null)
            {
                UnityEditor.Editor.CreateCachedEditor(asset, null, ref editor);
            }
            if ((editor != null ? editor.target : null) != null)
            {
                editor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("SfxPlayerConfig not found!", MessageType.Error);
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent("Generate Config",
                        "Press this button to Generate SFX Player Config!")))
                {
                    GenerateConfig();
                }
            }
        }

       

        private static void GenerateConfig()
        {
            if (!AssetDatabase.IsValidFolder(Path.Combine(SfxPlayerEditor.Path, SfxPlayerEditor.FolderName)))
            {
                AssetDatabase.CreateFolder(SfxPlayerEditor.Path, SfxPlayerEditor.FolderName);
            }

            SfxPlayerConfig asset =
                AssetDatabase.LoadAssetAtPath<SfxPlayerConfig>(Path.Combine(SfxPlayerEditor.Path,
                    SfxPlayerEditor.FolderName, SfxPlayerEditor.FileName));
            if (asset == null)
            {
                asset = CreateInstance<SfxPlayerConfig>();
                AssetDatabase.CreateAsset(asset,
                    Path.Combine(SfxPlayerEditor.Path, SfxPlayerEditor.FolderName, SfxPlayerEditor.FileName));
                AssetDatabase.SaveAssets();

                FieldsGenerator.GenerateFields("AudioPlayer", "Sounds",
                    Path.Combine(SfxPlayerEditor.Path, "Scripts/"), new List<string>(0), new List<string>(0));
                foreach (BuildTargetGroup buildTarget in SfxPlayerEditor.Platforms)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, "AUDIO_PLAYER");
                }
            }
        }
    }
}