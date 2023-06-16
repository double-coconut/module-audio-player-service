using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace AudioPlayerService.Runtime.Helpers
{
    public class FieldsGenerator
    {
        public static void GenerateFields(string namespaceName, string name, string path, List<string> fields,
            List<string> names)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
            {
                Debug.LogError("Your Enum name is wrong or empty");
                return;
            }

            if (fields == null)
            {
                Debug.LogError("Your fields array is null");
                return;
            }

            if (fields.Count == 0)
            {
                Debug.LogError("Your fields array is empty");
            }

            name = name.First().ToString().ToUpper() + name.Substring(1);

            string[] uniqueFieldNames = new string[fields.Count];
            Array.Copy(fields.ToArray(), uniqueFieldNames, fields.Count);

            int n = uniqueFieldNames.Length;

            string[] charsToRemove = new string[]
            {
                "@", ",", ".", ";", "'", "/", "\'", "!", "@", "#", "$", "%", "%", "^", "&", "*", "(", ")", "+", "=",
                "{", "}", "[", "]", "`", "~", "±", "§", ":", "?"
            };
            for (int i = 0; i < n; i++)
            {

                string uName = uniqueFieldNames[i];
                if(Regex.IsMatch(uName[0].ToString(), @"^\d$"))
                {
                    StringBuilder nameBuilder = new StringBuilder();
                    bool previousNumber = true;
                    for (int ch = 0; ch < uName.Length; ch++)
                    {
                        if (previousNumber && Regex.IsMatch(uName[ch].ToString(), @"^\d$"))
                        {
                            continue;
                        }

                        previousNumber = false;
                        nameBuilder.Append(uName[ch]);
                    }

                    uniqueFieldNames[i] = nameBuilder.ToString();
                }
                
                for (int j = 0; j < charsToRemove.Length; j++)
                {
                    if (uniqueFieldNames[i].Contains(charsToRemove[j]))
                    {
                        uniqueFieldNames[i] = uniqueFieldNames[i].Replace(charsToRemove[j], String.Empty);
                    }
                }

                if (uniqueFieldNames[i].Contains(" "))
                {
                    uniqueFieldNames[i] = uniqueFieldNames[i].Replace(" ", "_");
                }

                if (uniqueFieldNames[i].Contains("-"))
                {
                    uniqueFieldNames[i] = uniqueFieldNames[i].Replace("-", "_");
                }
               
                int index = 0;
                for (int j = 0; j < n; j++)
                {
                    if (i != j && uniqueFieldNames[i] == uniqueFieldNames[j])
                    {
                        index++;
                        uniqueFieldNames[j] += "_" + index;
                    }
                }
            }
            

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                namespaceName = path.Replace("/", ".");
            }

            path += name + ".cs";


            using (StreamWriter streamWriter = new StreamWriter(path))
            {
                streamWriter.WriteLine($"namespace {namespaceName}");
                streamWriter.WriteLine("{");
                streamWriter.WriteLine("\t[System.Serializable]");
                streamWriter.WriteLine($"\tpublic class {name}");
                streamWriter.WriteLine("\t{");
                for (int i = 0; i < uniqueFieldNames.Length; i++)
                {
                    string value = names.Count < i || string.IsNullOrEmpty(names[i]) ? uniqueFieldNames[i] : names[i];
                    streamWriter.WriteLine($"\t\tpublic const string {uniqueFieldNames[i]} = \"{value}\";");
                }

                streamWriter.WriteLine("\t}");
                streamWriter.WriteLine("}");
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif