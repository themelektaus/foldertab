using System.IO;
using UnityEditor;
using UnityEngine;

namespace FolderTab.Editor
{
    public class ItemInfo
    {
        public FolderTabObject folderTabObject { get; }

        public enum Type
        {
            Unknown,
            Folder,
            File
        }

        public Type type;

        string _path;
        public string path
        {
            get => _path;
            set => _path = Utils.FixPath(value);
        }

        Object _asset;
        bool _assetLoaded;
        
        public Object asset
        {
            get
            {
                if (!_assetLoaded)
                {
                    _asset = AssetDatabase.LoadAssetAtPath(GetAssetPath(), typeof(Object));
                    _assetLoaded = true;
                }
                return _asset;
            }
        }

        public ItemInfo(FolderTabObject folderTabObject)
        {
            this.folderTabObject = folderTabObject;
        }

        public string GetAssetPath() =>
            Utils.FixPath(Path.Combine(folderTabObject.GetAssetPath(), path));
    }
}