using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FolderTab.Editor
{
    [EditorWindowTitle(title = "Folder Tab")]
    public class FolderTabWindow : EditorWindow
    {
        const float DRAG_DISTANCE = 7;
        const int CHUNK_SIZE = 50;



        [SerializeField]
        VisualTreeAsset m_VisualTreeAsset;

        [SerializeField]
        FolderTabObject folderTabObject;
        SerializedObject serializedObject;



        readonly List<FolderTabItemElement> itemElements = new();

        ScrollView itemElementsContainer;
        int itemElementsLoaded;

        ToolbarMenu assetMenu;
        Button loadMoreButton;



        ToolbarButton backButton;
        ToolbarButton refreshButton;
        ToolbarButton configButton;
        ToolbarButton deleteButton;

        VisualElement objectPanel;


        readonly List<ItemInfo> itemInfos = new();


        public struct Selection
        {
            public static Selection nothing => new() { index = -1 };

            public FolderTabItemElement itemElement;
            public int index;
        }
        Selection selection = Selection.nothing;



        Object draggingAsset;
        Vector2 draggingStartPosition;
        Object droppableAsset;



        VisualElement root => rootVisualElement;



        public void CreateGUI()
        {
            VisualElement tree = m_VisualTreeAsset.Instantiate();
            root.Add(tree);
            tree.StretchToParentSize();

            itemElementsContainer = root.Q<ScrollView>("Items");

            assetMenu = root.Q<ToolbarMenu>("AssetMenu");

            loadMoreButton = root.Q<Button>("LoadMoreButton");
            loadMoreButton.AddToClassList("hidden");
            loadMoreButton.clicked += () => AddItemElements(scrollDown: true);

            backButton = root.Q<ToolbarButton>("BackButton");
            backButton.clicked += BackButton_clicked;

            refreshButton = root.Q<ToolbarButton>("RefreshButton");
            refreshButton.clicked += Refresh;

            configButton = root.Q<ToolbarButton>("ConfigButton");
            configButton.clicked += ConfigButton_clicked;

            deleteButton = root.Q<ToolbarButton>("DeleteButton");
            deleteButton.clicked += DeleteButton_clicked;

            objectPanel = root.Q("ObjectPanel");
            objectPanel.RegisterCallback<MouseDownEvent>(e => e.StopPropagation());

            Refresh();

            root.RegisterCallback<MouseDownEvent>(e =>
            {
                SetSelection(-1);

                if (HideObjectPanel())
                {
                    Refresh();
                    return;
                }

                RefreshSelectionElement();
            });

            root.RegisterCallback<MouseMoveEvent>(e =>
            {
                if (!draggingAsset || droppableAsset)
                    return;

                if (Vector2.Distance(draggingStartPosition, e.mousePosition) < DRAG_DISTANCE)
                    return;

                droppableAsset = draggingAsset;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { droppableAsset };
                DragAndDrop.StartDrag("Dragging " + droppableAsset.name);
            });

            root.RegisterCallback<MouseUpEvent>(e =>
            {
                if (droppableAsset)
                    DragAndDrop.AcceptDrag();

                draggingAsset = null;
                droppableAsset = null;
            });

            root.focusable = true;
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnKeyDown(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    if (selection.index == -1)
                    {
                        var first = itemElements.Where(x => x.itemInfo.type == ItemInfo.Type.File).FirstOrDefault();
                        if (first is null)
                            return;

                        SetSelection(first.itemInfo);
                        RefreshSelectionElement();
                        return;
                    }
                    SetSelection(Mathf.Max(0, selection.index - 1));
                    RefreshSelectionElement();
                    return;

                case KeyCode.DownArrow:
                    if (selection.index == -1)
                    {
                        var second = itemElements.Where(x => x.itemInfo.type == ItemInfo.Type.File).Take(2).LastOrDefault();
                        if (second is null)
                            return;

                        SetSelection(second.itemInfo);
                        RefreshSelectionElement();
                        return;
                    }
                    SetSelection(Mathf.Min(selection.index + 1, itemInfos.Count - 1));
                    RefreshSelectionElement();
                    return;
            }
        }



        public bool HideObjectPanel()
        {
            if (!objectPanel.ClassListContains("show"))
                return false;

            objectPanel.RemoveFromClassList("show");
            return true;
        }



        void BackButton_clicked()
        {
            if (!folderTabObject)
                return;

            folderTabObject.GotoParentPath();
            Refresh();
        }

        void ConfigButton_clicked()
        {
            if (HideObjectPanel())
            {
                Refresh();
                return;
            }

            objectPanel.AddToClassList("show");
        }

        void DeleteButton_clicked()
        {
            if (
                EditorUtility.DisplayDialog(
                    "Warning",
                    "Do you want to remove this path from folder tabs? " +
                    "Don't worry - This action does NOT delete " +
                    "any files or folders in your project.",
                    "Yes", "No"
                )
            )
            {
                Delete();
            }
        }



        public void StartDragAndDrop(Object asset, Vector2 startPosition)
        {
            draggingAsset = asset;
            draggingStartPosition = startPosition;
            droppableAsset = null;
        }



        void Load(FolderTabObject folderTabObject)
        {
            this.folderTabObject = folderTabObject;
            Refresh();
        }



        void Delete()
        {
            var path = AssetDatabase.GetAssetPath(folderTabObject);

            if (!string.IsNullOrEmpty(path))
                AssetDatabase.DeleteAsset(path);

            Refresh();
        }

        public void Refresh()
        {
            HideObjectPanel();

            titleContent = new GUIContent(
                folderTabObject ? folderTabObject.title : "(None)",
                Utils.GetIcon("Folder")
            );

            RefreshItemInfos();

            ClearItemElements();
            AddItemElements(scrollDown: false);

            RefreshSelectionElement();

            RefreshTopbarAssetMenu();
            RefreshToolbarButtons();

            Bind();
        }

        void Bind()
        {
            objectPanel.Unbind();

            //--------------------------------------------------
            // MyTODO: Is this needed?                
            //--------------------------------------------------
            // if (serializedObject != null)
            // {
            //     serializedObject.Update();
            //     serializedObject = null;
            // }
            //--------------------------------------------------

            if (!folderTabObject)
                return;

            serializedObject = new SerializedObject(folderTabObject);
            objectPanel.Bind(serializedObject);
        }



        void RefreshItemInfos()
        {
            itemInfos.Clear();

            if (!folderTabObject)
                return;

            if (!folderTabObject.AssetPathExists())
                return;

            foreach (var folder in folderTabObject.GetFolders())
            {
                itemInfos.Add(new ItemInfo(folderTabObject)
                {
                    type = ItemInfo.Type.Folder,
                    path = folder
                });
            }

            foreach (var file in folderTabObject.GetFiles())
            {
                itemInfos.Add(new ItemInfo(folderTabObject)
                {
                    type = ItemInfo.Type.File,
                    path = file
                });
            }
        }

        void ClearItemElements()
        {
            itemElements.Clear();
            itemElementsContainer.Clear();
            itemElementsLoaded = 0;
        }

        void AddItemElements(bool scrollDown)
        {
            if (!folderTabObject)
                return;

            FolderTabItemElement itemElement;

            if (!folderTabObject.AssetPathExists())
                return;

            for (int i = 0; i < CHUNK_SIZE && itemElementsLoaded < itemInfos.Count; i++, itemElementsLoaded++)
            {
                var itemInfo = itemInfos[itemElementsLoaded];

                if (!itemInfo.asset)
                    continue;

                bool isFolder = itemInfo.type == ItemInfo.Type.Folder;
                if (isFolder && folderTabObject.flattenRecursively)
                    continue;

                itemElement = new FolderTabItemElement(isFolder ? null : this)
                {
                    itemInfo = itemInfo
                };

                itemElement.onLeftClick += e =>
                {
                    if (itemInfo.type == ItemInfo.Type.Folder)
                    {
                        SetSelection(-1);
                        folderTabObject.GotoChildPath(itemInfo.asset.name);
                        Refresh();
                        return;
                    }

                    if (itemInfo.type != ItemInfo.Type.File)
                        return;

                    SetSelection(itemInfo);
                    RefreshSelectionElement();

                    switch (folderTabObject.defaultAction)
                    {
                        case FolderTabObject.DefaultAction.Open:
                            AssetDatabase.OpenAsset(itemInfo.asset);
                            break;

                        case FolderTabObject.DefaultAction.Ping:
                            EditorGUIUtility.PingObject(itemInfo.asset);
                            break;

                        case FolderTabObject.DefaultAction.Select:
                            UnityEditor.Selection.activeObject = itemInfo.asset;
                            break;
                    }
                };

                itemElement.onRightClick += e =>
                {
                    if (itemInfo.type != ItemInfo.Type.File)
                        return;

                    SetSelection(itemInfo);
                    RefreshSelectionElement();

                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Open"), false, x => AssetDatabase.OpenAsset(x as Object), itemInfo.asset);
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Ping"), false, x => EditorGUIUtility.PingObject(x as Object), itemInfo.asset);
                    menu.AddItem(new GUIContent("Select"), false, x => UnityEditor.Selection.activeObject = x as Object, itemInfo.asset);
                    menu.ShowAsContext();
                };

                itemElements.Add(itemElement);
                itemElementsContainer.Add(itemElement);
            }

            if (scrollDown)
                itemElementsContainer.scrollOffset = new(0, int.MaxValue);

            if (itemElementsLoaded < itemInfos.Count)
            {
                loadMoreButton.RemoveFromClassList("hidden");
                return;
            }

            loadMoreButton.AddToClassList("hidden");
        }



        void SetSelection(int index)
        {
            if (index < 0 || index >= itemInfos.Count)
            {
                SetSelection(null);
                return;
            }

            var itemInfo = itemInfos[index];
            if (itemInfo.type == ItemInfo.Type.Folder)
                return;

            SetSelection(itemInfos[index]);
            selection.itemElement.PerformLeftClick();
        }

        void SetSelection(ItemInfo itemInfo)
        {
            if (itemInfo is null)
            {
                selection = Selection.nothing;
                return;
            }

            var itemElement = itemElements.FirstOrDefault(x => x.itemInfo == itemInfo);

            selection = new Selection
            {
                itemElement = itemElement,
                index = itemElements.IndexOf(itemElement)
            };
        }

        void RefreshSelectionElement()
        {
            itemElements.ForEach(x => x.RemoveFromClassList("active"));
            var itemElement = itemElements.FirstOrDefault(x => x == selection.itemElement);
            if (itemElement is not null)
                itemElement.AddToClassList("active");
        }



        void RefreshTopbarAssetMenu()
        {
            assetMenu.text = "(None)";

            var menu = assetMenu.menu;

            var menuItemCount = menu.MenuItems().Count;
            for (int i = 0; i < menuItemCount; i++)
                menu.RemoveItemAt(0);

            var status = folderTabObject ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Checked;
            menu.AppendAction("(None)", x => Load(null), status);
            menu.AppendSeparator();

            var guids = AssetDatabase.FindAssets($"t:{typeof(FolderTabObject).FullName}");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<FolderTabObject>(assetPath);

                var active = asset == folderTabObject;
                status = active ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;

                var actionName = "";
                var rootMenu = asset.menu.Trim('/');
                var menuItemName = asset.rootPath.Length == 0 ? "Assets" : asset.rootPath.Replace("/", " > ");

                if (string.IsNullOrWhiteSpace(rootMenu))
                {
                    actionName += menuItemName;
                }
                else
                {
                    actionName = rootMenu;
                    if (!asset.fixedMenu)
                        actionName += "/" + menuItemName;
                }

                menu.AppendAction(actionName, x => Load(asset), status);

                if (!active)
                    continue;

                if (asset.subPath.Length > 0)
                {
                    if (asset.rootPath.Length == 0)
                    {
                        assetMenu.text = "Assets/" + asset.subPath;
                        continue;
                    }

                    var rootPathParts = asset.rootPath.Split('/');
                    if (rootPathParts.Length == 1)
                    {
                        assetMenu.text = rootPathParts[0] + "/" + asset.subPath;
                        continue;
                    }

                    assetMenu.text = "../" + rootPathParts[^1] + "/" + asset.subPath;
                    continue;
                }

                assetMenu.text = asset.rootPath.Length == 0 ? "Assets" : asset.rootPath;
            }
        }

        void RefreshToolbarButtons()
        {
            backButton.SetEnabled(folderTabObject && folderTabObject.subPath.Length > 0);
            configButton.SetEnabled(folderTabObject);
            deleteButton.SetEnabled(folderTabObject);
        }



        public override IEnumerable<System.Type> GetExtraPaneTypes()
        {
            yield return typeof(FolderTabWindow);
        }



        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int _)
        {
            var @object = EditorUtility.InstanceIDToObject(instanceID);
            if (@object is FolderTabObject folderTabObject)
            {
                CreateWindow<FolderTabWindow>(typeof(FolderTabWindow))
                    .Load(folderTabObject);
            }
            return false;
        }

        [MenuItem("Assets/Open Folder Tab", priority = 1989)]
        static void OpenFolderTab(MenuCommand _)
        {
            var window = CreateWindow<FolderTabWindow>(typeof(FolderTabWindow));

            if (!AddToFolderTab_Validate())
                return;

            var folderTabObject = GetAssetInfo().folderTabObject;
            if (folderTabObject)
                window.Load(folderTabObject);
        }

        [MenuItem("Assets/Add to Folder Tab", validate = true)]
        static bool AddToFolderTab_Validate(MenuCommand _ = null)
        {
            if (UnityEditor.Selection.objects.Length != 1)
                return false;

            if (UnityEditor.Selection.activeObject is not DefaultAsset)
                return false;

            return true;
        }

        [MenuItem("Assets/Add to Folder Tab", priority = 1990)]
        static void AddToFolderTab(MenuCommand _)
        {
            var (userDataPath, rootPath, folderTabObject) = GetAssetInfo();
            if (folderTabObject != null)
                return;

            var newAsset = CreateInstance<FolderTabObject>();

            var title = System.IO.Path.GetFileNameWithoutExtension(rootPath);
            newAsset.title = title.Length == 0 ? "Assets" : title;
            newAsset.rootPath = rootPath;

            var assetName = rootPath.Length == 0 ? "Assets" : rootPath.Replace("/", " → ");
            AssetDatabase.CreateAsset(newAsset, $"{userDataPath}/{assetName}.asset");

            CreateWindow<FolderTabWindow>(typeof(FolderTabWindow))
                .Load(newAsset);
        }



        static (string userDataPath, string rootPath, FolderTabObject folderTabObject) GetAssetInfo()
        {
            var assetPath = AssetDatabase.GetAssetPath(UnityEditor.Selection.activeObject);
            var assetRootPath = assetPath[6..].Trim('/');

            var assetsPath = "Assets";

            foreach (var file in System.IO.Directory.GetFiles(assetsPath, "*.asset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<FolderTabObject>(file);
                if (!asset || asset.rootPath.Trim('/') == assetRootPath)
                    return (assetsPath, assetRootPath, asset);
            }

            return (assetsPath, assetRootPath, null);
        }



        //--------------------------------------------------------------------------------
        // Lock button
        //--------------------------------------------------------------------------------
        // static GUIStyle lockButtonStyle;
        // [SerializeField] bool locked;
        // void ShowButton(Rect position)
        // {
        //     if (lockButtonStyle == null)
        //         lockButtonStyle = "IN LockButton";
        //     locked = GUI.Toggle(position, locked, GUIContent.none, lockButtonStyle);
        // }
        //--------------------------------------------------------------------------------
    }
}