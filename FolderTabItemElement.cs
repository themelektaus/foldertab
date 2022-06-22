using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FolderTab.Editor
{
    public class FolderTabItemElement : VisualElement
    {
        public Texture icon
        {
            get => iconElement.style.backgroundImage.value.texture;
            set => iconElement.style.backgroundImage = value as Texture2D;
        }

        public string text
        {
            get => label.text;
            set => label.text = value;
        }

        ItemInfo _itemInfo;
        public ItemInfo itemInfo
        {
            get => _itemInfo;
            set
            {
                _itemInfo = value;

                if (_itemInfo == null)
                    return;

                var asset = _itemInfo.asset;
                if (asset)
                    text = asset.name;

                if (_itemInfo.type == ItemInfo.Type.Unknown)
                    return;

                if (_itemInfo.type == ItemInfo.Type.Folder)
                {
                    icon = Utils.GetIcon("d_Folder") as Texture2D;
                    return;
                }

                if (asset)
                    icon = EditorGUIUtility.ObjectContent(asset, asset.GetType()).image as Texture2D;
            }
        }

        public event EventCallback<MouseDownEvent> onLeftClick;
        public event EventCallback<MouseDownEvent> onRightClick;

        VisualElement iconElement;
        Label label;

        public FolderTabItemElement() : this(null) { }
        public FolderTabItemElement(FolderTabWindow window)
        {
            Add(Utils.LoadVisualTreeAsset(this));

            AddToClassList("unknown");

            iconElement = this.Q<VisualElement>(className: "item__icon");
            label = this.Q<Label>(className: "item__label");

            RegisterCallback<MouseDownEvent>(e =>
            {
                if (window)
                {
                    e.StopPropagation();
                    if (window.HideObjectPanel())
                        window.Refresh();
                }

                if (e.button == 0)
                {
                    if (window)
                        window.StartDragAndDrop(itemInfo.asset, e.mousePosition);
                    onLeftClick?.Invoke(e);
                }

                if (e.button == 1)
                    onRightClick?.Invoke(e);
            });
        }

        public void PerformLeftClick()
        {
            onLeftClick?.Invoke(null);
        }
    }
}