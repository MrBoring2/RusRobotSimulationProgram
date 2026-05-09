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
    private EventBus _eventBus;

    private SaveLoadManager _saveLoadManager;
    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _saveLoadManager = ServiceManager.Current.Get<SaveLoadManager>();
        //saveLoadProvider = new BinarySaveLoadProvider();
        root = GetComponent<UIDocument>().rootVisualElement;
        var newBtn = root.Q<Button>("new-file-button");
        var saveBtn = root.Q<Button>("save-button");
        var loadBtn = root.Q<Button>("load-button");
        tooltipEvents.RegisterTooltip(saveBtn, "Сохранить");
        tooltipEvents.RegisterTooltip(loadBtn, "Загрузить");
        tooltipEvents.RegisterTooltip(newBtn, "Новая сцена");
        saveBtn.RegisterCallback<ClickEvent>(evt =>
        {
            _saveLoadManager.SaveScene();
        });
        loadBtn.RegisterCallback<ClickEvent>(evt =>
        {
            _saveLoadManager.LoadScene();
        });
        newBtn.RegisterCallback<ClickEvent>(evt =>
        {
            _saveLoadManager.ClearScene();
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

   

}
