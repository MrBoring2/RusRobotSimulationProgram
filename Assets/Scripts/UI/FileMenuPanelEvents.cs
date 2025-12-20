using Assets.Scripts.Models;
using Assets.Scripts.SystemManager;
using Assets.Scripts.UI;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class FileMenuPanelEvents : MonoBehaviour
{
    private VisualElement root;
    public TooltipEvents tooltipEvents;
    public HierarchyPanelEvents hierarchyPanelEvents;
    public ISaveLoadProvider saveLoadProvider;
    public GameObjectManager _gameObjectManager;
    private string savePath = "";
    private void Start()
    {
        saveLoadProvider = new BinarySaveLoadProvider();
        root = GetComponent<UIDocument>().rootVisualElement;
        var newBtn = root.Q<Button>("new-file-button");
        var saveBtn = root.Q<Button>("save-button");
        var loadBtn = root.Q<Button>("load-button");
        tooltipEvents.RegisterTooltip(saveBtn, "Сохранить");
        tooltipEvents.RegisterTooltip(loadBtn, "Загрузить");
        tooltipEvents.RegisterTooltip(newBtn, "Новая сцена");
        saveBtn.RegisterCallback<ClickEvent>(evt =>
        {
            SaveScene();
        });
        loadBtn.RegisterCallback<ClickEvent>(evt =>
        {
            LoadScene();
        });
        newBtn.RegisterCallback<ClickEvent>(evt =>
        {
            ClearScene();
        });
    }

    private void LoadScene()
    {
        var extentionsList = new[]
        {
            new ExtensionFilter("Файл RusRobot", "rusbot")
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extentionsList, false, OnSceneFileSelected);
    }

    private void OnSceneFileSelected(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return;

        UndoRedoSystem.Instance.BeginExternalOperation();

        try
        {
            ClearScene();

            var loaded = saveLoadProvider.Load(paths[0]);
            if (loaded == null)
            {
                Debug.LogError("Ошибка загрузки сцены");
                return;
            }
            savePath = paths[0];
            hierarchyPanelEvents.LoadHierarchy();
            foreach (var data in loaded.objectsData)
            {
                SpawnRestoredObject(data);
            }
        }
        finally
        {
            // ✅ Undo ВСЕГДА вернётся
            UndoRedoSystem.Instance.EndExternalOperation();
        }
    }

    private void SaveScene()
    {
        if(!string.IsNullOrEmpty(savePath))
        {
            if(File.Exists(savePath))
                saveLoadProvider.Save(savePath, hierarchyPanelEvents.Items
                  .Select(p => p.Reference)
                  .ToList());
            return;
        }
        var extentionsList = new[]
        {
            new ExtensionFilter("Файл RusRobot", "rusbot")
        };
        StandaloneFileBrowser.SaveFilePanelAsync("Выберите место для сохранения", "", "", extentionsList, (string path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;
            savePath = path;
            saveLoadProvider.Save(path, hierarchyPanelEvents.Items
                .Select(p => p.Reference)
                .ToList());
        });
    }
    private IPropertyProvider AddProvider(GameObject obj, string type)
    {
        return type switch
        {
            nameof(PrimitivePropertyProvider) => obj.AddComponent<PrimitivePropertyProvider>(),
            nameof(RobotPropertyProvider) => obj.AddComponent<RobotPropertyProvider>(),
            _ => null
        };
    }
    private void SpawnRestoredObject(ObjectInfo data)
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>($"Prefabs/Primitive");
        var prefab = Resources.Load<GameObject>(data.SourcePath);
        var instance = _gameObjectManager.CreateObject(prefab, Vector3.zero);
        var provider = AddProvider(instance, data.ProviderData.ProviderType);
        provider?.RestoreCustomState(data.ProviderData);
        instance.name = data.Name;
        instance.tag = "SceneObject";
        instance.transform.position = data.Position.ToVector3();
        instance.transform.rotation = data.Rotation.ToQuaternion();
        instance.transform.localScale = data.Scale.ToVector3();

        var m = instance.AddComponent<SceneObjectMarker>();
        m.type = data.ObjectType;
        m.sourcePath = data.SourcePath;
    }

    private void ClearScene()
    {
        var itemsToDelete = new List<GameObject>(hierarchyPanelEvents.Items.Select(item => item.Reference));

        foreach (var gameObject in itemsToDelete)
        {
            _gameObjectManager.DeleteObject(gameObject);
        }
    }

}
