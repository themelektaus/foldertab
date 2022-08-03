using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FolderTab.Editor
{
    public static class Utils
    {
        static readonly Dictionary<string, VisualTreeAsset> visualTreeAssets = new();

        // MyTODO: Needs a "better" solution
        public static TemplateContainer LoadVisualTreeAsset(object obj)
        {
            var name = obj.GetType().Name;
            if (!visualTreeAssets.ContainsKey(name))
            {
                var assetGUID = AssetDatabase.FindAssets("t:visualtreeasset " + name)[0];
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
                visualTreeAssets[name] = visualTreeAsset;
            }
            return visualTreeAssets[name].CloneTree();
        }

        public static string FixPath(string path) =>
            path.Replace('\\', '/').Trim('/');



        static readonly Dictionary<string, Texture> icons = new();
        
        public static Texture GetIcon(string name)
        {
            if (!icons.ContainsKey(name))
            {
                var path = $"Assets/FolderTab/Icons/{name} Icon.png";
                if (System.IO.File.Exists(path))
                    icons.Add(name, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                else
                    icons.Add(name, EditorGUIUtility.IconContent($"{name} Icon").image);
            }
            return icons[name];
        }
    }
}