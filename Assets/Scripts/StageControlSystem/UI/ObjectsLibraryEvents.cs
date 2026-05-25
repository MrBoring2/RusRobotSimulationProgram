using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.ObjectsLibrary;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ObjectsLibraryEvents : BaseModalWindow
{
    public VisualTreeAsset itemUXML;
    private Dictionary<string, string> categories = new Dictionary<string, string>();
    public event Action<GameObject> OnObjectSelected;
    private VisualElement list;
    private Dictionary<string, GameObject[]> loadedPrefabs = new Dictionary<string, GameObject[]>();
    private string currentParentObjectId = null;
    private GameObject selectedObject;

    protected override void Start()
    {
        base.Start();
    
        categories.Add("Robot", "Манипуляторы");
        categories.Add("PLC", "ПЛК");
        categories.Add("Movement", "Перемещение");
        categories.Add("Workpieces", "Детали");
        categories.Add("Detectors", "Датчики");
        categories.Add("Work", "Рабочие элементы");
        categories.Add("Environment", "Окружение");
        categories.Add("Primitive", "Примитивы");     
        categories.Add("General", "Общее");
        foreach (var category in categories.Keys)
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>($"Prefabs/{category}");
            loadedPrefabs[category] = prefabs;
        }
    }

    protected override void InitializeElements(VisualElement root)
    {
        base.InitializeElements(root);
        
        messageLabel = root.Q<Label>("message-label");
        closeButton = root.Q<Button>("close-button");
        list = windowRoot.Q<ScrollView>("list");
    }
    protected override void RegisterEvents()
    {
        base.RegisterEvents();
    }
    protected override void OnBeforeShow(ModalParameters parameters)
    {
        currentParentObjectId = parameters.Get("currentParentObjectId", "");
        if (messageLabel != null)
        {
            string message = parameters.Get("message", "Библиотека объектов");
            messageLabel.text = message;
        }

        
        BuildList();
    }

    void BuildList()
    {
        list.Clear();

        foreach (var category in loadedPrefabs)
        {

            string categoryName = categories[category.Key];
            GameObject[] prefabs = category.Value;

            Label catLabel = new Label(categoryName)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, color = Color.white }
            };
            list.Add(catLabel);

            foreach (var prefab in prefabs)
            {
                var prefabItem = itemUXML.CloneTree();
                var previewImage = prefabItem.Q<Image>("preview");
                var titleLabel = prefabItem.Q<Label>("title");
                if (prefab.GetComponent<SceneObjectMarker>()?.type == ObjectType.Node)
                {
                    previewImage.image = Resources.Load<Texture2D>("Icons/icon_node");
                    previewImage.scaleMode = ScaleMode.ScaleToFit;
                    previewImage.style.width = new Length(150, LengthUnit.Pixel);
                    previewImage.style.height = new Length(80, LengthUnit.Pixel);
                }
                else
                {
                    previewImage.image = GeneratePreview(prefab);
                    previewImage.style.width = new Length(150, LengthUnit.Pixel);
                    previewImage.style.height = new Length(80, LengthUnit.Pixel);
                    previewImage.scaleMode = ScaleMode.ScaleAndCrop;
                }
              
                // Если нужно растянуть на всю площадь
                //previewImage.style.flexGrow = 1;

                titleLabel.text = prefab.name;

                prefabItem.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.clickCount == 2)
                    {
                        CloseWithValue(prefab);
                        //OnObjectSelected?.Invoke(prefab);
                        windowRoot.style.display = DisplayStyle.None;
                    }
                });

                list.Add(prefabItem);
            }
        }
    }

    Texture2D GeneratePreview(GameObject prefab)
    {
        int size = 512;
        var camGO = new GameObject("PreviewCam");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = false;

        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 8,
            hideFlags = HideFlags.DontSave
        }; 

        cam.targetTexture = rt;

        var obj = Instantiate(prefab);
        obj.transform.position = Vector3.zero;

        int previewLayer = 31;
        SetLayerRecursively(obj, previewLayer);
        cam.cullingMask = 1 << previewLayer;

        var bounds = CalculateBounds(obj);

        float distance = bounds.size.magnitude * 1.5f;
        Vector3 offset = new Vector3(1.5f, 0.3f, -1f);
        offset.Normalize();
        cam.transform.position = bounds.center + offset * distance;
        cam.transform.LookAt(bounds.center);

        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;

        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;

        // Уничтожаем объекты
        DestroyImmediate(obj);
        DestroyImmediate(camGO);
        DestroyImmediate(rt);

        return tex;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    private Bounds CalculateBounds(GameObject go)
    {
        var r = go.GetComponentsInChildren<Renderer>();
        var b = r[0].bounds;
        foreach (var rr in r) b.Encapsulate(rr.bounds);
        return b;
    }
}
