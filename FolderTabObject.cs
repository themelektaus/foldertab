using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FolderTab.Editor
{
    public class FolderTabObject : ScriptableObject
    {
        public string title = "Folder";
        public string menu = "";
        public bool fixedMenu = true;
        public string rootPath = "";
        public string subPath = "";
        public bool flattenRecursively = false;

        public enum DefaultAction { None, Open, Ping, Select }
        public DefaultAction defaultAction;

        public bool AssetPathExists() =>
            Directory.Exists(GetAssetPath());

        public void GotoParentPath()
        {
            if (subPath.Length == 0)
                return;

            var pathParts = subPath.Split('/');
            ArrayUtility.RemoveAt(ref pathParts, pathParts.Length - 1);
            subPath = string.Join('/', pathParts);
        }

        public void GotoChildPath(string path)
        {
            subPath = Utils.FixPath(Path.Combine(subPath, path));
        }

        public string GetAssetPath() =>
            Utils.FixPath(Path.Combine("Assets", rootPath, subPath));

        public string[] GetFolders()
        {
            var path = GetAssetPath();
            return Directory
                .GetDirectories(path)
                .Select(x => x[path.Length..])
                .ToArray();
        }

        public string[] GetFiles()
        {
            var path = GetAssetPath();
            var searchOptions = flattenRecursively
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;
            return Directory
                .GetFiles(path, "*.*", searchOptions)
                .Where(x => !x.EndsWith(".meta"))
                .Where(x => !x.EndsWith("~"))
                .Select(x => x[path.Length..])
                .ToArray();
        }
    }
}