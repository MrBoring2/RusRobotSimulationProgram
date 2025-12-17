using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectsLibraryEvents : MonoBehaviour
{
    public VisualTreeAsset windowUXML;  // Основное окно
    public VisualTreeAsset itemUXML;    // Элемент списка

    private List<string> categories = new List<string> { "Primitive", "Static" };

    public event Action<GameObject> OnObjectSelected;

    private VisualElement root;
    private VisualElement windowRoot;
    private VisualElement list;
    private Dictionary<string, GameObject[]> loadedPrefabs = new Dictionary<string, GameObject[]>();
    public UIBlocker UIBlocker;
    private bool isDragging = false;
    private Vector2 dragOffset;

    void Start()
    {
        // Загружаем префабы
        foreach (var category in categories)
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>($"Prefabs/{category}");
            loadedPrefabs[category] = prefabs;
        }

        // Инициализация UI
        root = GetComponent<UIDocument>().rootVisualElement;
        windowRoot = windowUXML.CloneTree();
        //root.Add(windowRoot);
        // windowRoot.style.display = DisplayStyle.None;

        list = windowRoot.Q<ScrollView>("list");

        // Кнопка отмены
        var button = windowRoot.Q<Button>("cancelBtn");
        button.clicked += () => { windowRoot.style.display = DisplayStyle.None; UIBlocker.RemoveModalWindow(windowRoot); UIBlocker.ResolveUI(); };

        // Включаем перетаскивание
        EnableDrag();

        // Заполняем список префабов
        BuildList();
    }

    private void EnableDrag()
    {
        var rootElement = windowRoot.Q("objects-library-container");

        rootElement.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                isDragging = true;
                dragOffset = evt.mousePosition - rootElement.layout.position;
            }
        });

        rootElement.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (isDragging)
            {
                rootElement.style.left = evt.mousePosition.x - dragOffset.x;
                rootElement.style.top = evt.mousePosition.y - dragOffset.y;
            }
        });

        rootElement.RegisterCallback<MouseUpEvent>(evt => isDragging = false);
    }

    public void Show()
    {
        if (windowRoot.parent == null)
            root.Q("overlay").Add(windowRoot);
        // windowRoot.tabIndex = 1000;
        windowRoot.style.display = DisplayStyle.Flex;

        // задаем стартовую позицию по центру экрана
        windowRoot.RegisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
        UIBlocker.AddNewModalWindow(windowRoot);
    }

    private void OnWindowSizeChanged(GeometryChangedEvent evt)
    {
        var rootElement = windowRoot.Q("objects-library-container");
        float windowWidth = rootElement.resolvedStyle.width;
        float windowHeight = rootElement.resolvedStyle.height;
        float screenWidth = root.resolvedStyle.width;
        float screenHeight = root.resolvedStyle.height;

        // Центрируем окно
        windowRoot.style.left = (screenWidth - windowWidth) / 2;
        windowRoot.style.top = (screenHeight - windowHeight) / 2;
        windowRoot.UnregisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
    }
    void BuildList()
    {
        list.Clear();

        foreach (var category in loadedPrefabs)
        {

            string categoryName = category.Key;
            GameObject[] prefabs = category.Value;

            // Добавляем заголовок категории
            Label catLabel = new Label(categoryName)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 }
            };
            list.Add(catLabel);

            // Создаем элементы для каждого префаба
            foreach (var prefab in prefabs)
            {
                var prefabItem = itemUXML.CloneTree();
                var previewImage = prefabItem.Q<Image>("preview");
                var titleLabel = prefabItem.Q<Label>("title");

                // Загружаем предпросмотр (RenderTexture или модель)

                // previewImage.image =  GeneratePreview(prefab);
                previewImage.image = GeneratePreview(prefab);
                titleLabel.text = prefab.name;

                // Обработчик выбора префаба
                prefabItem.RegisterCallback<ClickEvent>(evt =>
                {
                    var objInstance = Instantiate(prefab);
                    objInstance.name = prefab.name;
                    OnObjectSelected?.Invoke(objInstance);
                    windowRoot.style.display = DisplayStyle.None;
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

        // Создаем RenderTexture с параметром "dontSave"
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 8,
            hideFlags = HideFlags.DontSave // Важно для предотвращения утечек
        };

        cam.targetTexture = rt;

        var obj = Instantiate(prefab);
        obj.transform.position = Vector3.zero;

        // Помещаем объект в отдельный слой для предпросмотра
        int previewLayer = 31; // Используем последний слой
        SetLayerRecursively(obj, previewLayer);
        cam.cullingMask = 1 << previewLayer;

        var bounds = CalculateBounds(obj);

        // Настройка камеры
        float distance = bounds.size.magnitude * 1.5f;
        Vector3 offset = new Vector3(0.5f, 0.3f, -1f);
        offset.Normalize();
        cam.transform.position = bounds.center + offset * distance;
        cam.transform.LookAt(bounds.center);

        // Очищаем RenderTexture перед рендером
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;

        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        // Очищаем RenderTexture после использования
        RenderTexture.active = null;
        cam.targetTexture = null;

        // Уничтожаем объекты
        DestroyImmediate(obj);
        DestroyImmediate(camGO);
        DestroyImmediate(rt); // Уничтожаем RenderTexture

        return tex;
    }

    // Вспомогательный метод для установки слоя рекурсивно
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
