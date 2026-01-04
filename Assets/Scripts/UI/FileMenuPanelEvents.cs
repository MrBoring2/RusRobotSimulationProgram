using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.SystemManager;
using Assets.Scripts.UI;
using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class FileMenuPanelEvents : MonoBehaviour
{
    private VisualElement root;
    public TooltipEvents tooltipEvents;
    //public HierarchyPanelEvents hierarchyPanelEvents;
    public ISaveLoadProvider saveLoadProvider;
    private SceneObjectsManager _sceneObjectManager;
    private string savePath = "";
    private EventBus _eventBus;
    private UndoRedoManager _undoRedoManager;
    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();   
        _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
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

        _undoRedoManager.BeginExternalOperation();

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

            // hierarchyPanelEvents.LoadHierarchy();
            _sceneObjectManager.SpawnRestoredObjects(loaded.objectsData);
            //foreach (var data in loaded.objectsData)
            //{
            //    SpawnRestoredObject(data);
            //}
            _eventBus.Invoke(new LoadObjectsSignal(_sceneObjectManager.GetGameObjectsList()));
        }
        finally
        {
            _undoRedoManager.EndExternalOperation();
        }
    }

    private void SaveScene()
    {
        if (!string.IsNullOrEmpty(savePath))
        {
            if (File.Exists(savePath))
                saveLoadProvider.Save(savePath, _sceneObjectManager.GetGameObjectsList()); //hierarchyPanelEvents.Items);
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
            saveLoadProvider.Save(path, _sceneObjectManager.GetGameObjectsList());//hierarchyPanelEvents.Items);
        });
    }
   
    //private void SpawnRestoredObject(ObjectInfo data)
    //{
    //    var prefab = Resources.Load<GameObject>(data.SourcePath);
    //    var instance = _sceneObjectManager.Create(prefab, Vector3.zero, prefab.GetComponent<SceneObjectMarker>().type);
    //    var provider = GetProvider(instance.Reference, data.ProviderData.ProviderType);
    //    provider?.RestoreCustomState(data.ProviderData);
    //    instance.Reference.name = data.Name;
    //    instance.Reference.tag = "SceneObject";
    //    instance.Reference.transform.position = data.Position.ToVector3();
    //    instance.Reference.transform.rotation = data.Rotation.ToQuaternion();
    //    instance.Reference.transform.localScale = data.Scale.ToVector3();

    //    var m = instance.Reference.AddComponent<SceneObjectMarker>();
    //    m.type = data.ObjectType;
    //    m.sourcePath = data.SourcePath;
    //}

    private void ClearScene()
    {
        //var itemsToDelete = new List<GameObject>(hierarchyPanelEvents.Items.Select(item => item.Reference));
        _sceneObjectManager.ClearScene();
        //foreach (var gameObject in _sceneObjectManager.GetGameObjectsList())
        //{
        //    _sceneObjectManager.Remove(gameObject.Id);
        //}
    }

}
