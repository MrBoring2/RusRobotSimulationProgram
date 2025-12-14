using Assets.Scripts.Models;
using SFB;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TopMenuEvents : MonoBehaviour
{
    private VisualElement root;
    public HierarchyPanelEvents hierarchyPanelEvents;
    public ISaveLoadProvider saveLoadProvider;
    private void Start()
    {
        saveLoadProvider = new BinarySaveLoadProvider();
        root = GetComponent<UIDocument>().rootVisualElement;
        var saveBtn = root.Q<Button>("save-button");
        var loadBtn = root.Q<Button>("load-button");
        saveBtn.RegisterCallback<ClickEvent>(evt =>
        {
            SaveScene();
        });
        loadBtn.RegisterCallback<ClickEvent>(evt =>
        {
            LoadScene();
        });
    }

    private async void LoadScene()
    {
        var extentionsList = new[]
        {
            new ExtensionFilter("Файл RusRobot", "rusbot")
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extentionsList, false, (string[] path) =>
        {
            var loaded = saveLoadProvider.Load(path[0]);
            if (loaded != null)
                ClearScene();
            foreach (var item in loaded.objectsData)
            {
                SpawnRestoredObject(item);
            }
            hierarchyPanelEvents.LoadHierarchy();
        });
    }

    private void SaveScene()
    {
        var extentionsList = new[]
        {
            new ExtensionFilter("Файл RusRobot", "rusbot")
        };
        StandaloneFileBrowser.SaveFilePanelAsync("Выберите место для сохранения", "", "", extentionsList, (string path) =>
        {
            saveLoadProvider.Save(path, hierarchyPanelEvents.Items.Select(p => p.Reference).ToList());
        });
    }

    private void SpawnRestoredObject(ObjectInfo data)
    {
        GameObject obj = null;

        switch (data.ObjectType)
        {
            case ObjectType.Primitive:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube); // или из sourcePath
                obj.AddComponent<PrimitivePropertyProvider>();
                break;

            case ObjectType.Static:
                // Загрузить obj/FBX в рантайме
                //obj = RuntimeOBJLoader.LoadOBJ(data.sourcePath);
                break;

            case ObjectType.Dynamic:
                obj = Instantiate(Resources.Load<GameObject>(data.SourcePath));
                break;
            case ObjectType.Robot:
                obj = Instantiate(Resources.Load<GameObject>(data.SourcePath));
                break;
        }

        obj.name = data.Name;
        obj.tag = "SceneObject";
        obj.transform.position = data.Position.ToVector3();
        obj.transform.rotation = data.Rotation.ToQuaternion();
        obj.transform.localScale = data.Scale.ToVector3();


        var m = obj.AddComponent<SceneObjectMarker>();
        m.type = data.ObjectType;
        m.sourcePath = data.SourcePath;
    }

    private void ClearScene()
    {
        foreach (var obj in hierarchyPanelEvents.Items)
            DestroyImmediate(obj.Reference);
    }

}
