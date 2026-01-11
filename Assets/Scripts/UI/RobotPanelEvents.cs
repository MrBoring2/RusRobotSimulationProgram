using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using SFB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

public class RobotPanelEvents : MonoBehaviour
{
    public VisualTreeAsset treeAsset;
    public UIBlocker UIBlocker;
    private UIStatusManager StatusManager;
    private VisualElement root;
    private VisualElement tablet;
    private DropdownField fileDropdown;
    private VisualElement windowRoot;
    private TextField TXT;
    private TextField LineNumber;
    private List<string> txts = new();
    private List<string> filePaths = new();
    private int lastIndex = 0;
    private int NumberOfLines = 0;
    private bool isDragging = false;
    private Vector2 dragOffset;

    private void EnableDrag()
    {
        var rootElement = windowRoot.Q("Tablet");

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
    public void Close()
    {
        tablet.style.display = DisplayStyle.None;
        UIBlocker.RemoveModalWindow(windowRoot);
        StatusManager.SetInputMode(false);
    }

    public void OpenFile()
    {
        var extentionsList = new[]
            {
                new ExtensionFilter("Текстовый документ", "txt")
            };
        StandaloneFileBrowser.OpenFilePanelAsync("Выберите файл", "", extentionsList, false, LoadFile);
    }

    private void LoadFile(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return;
        string filename = Path.GetFileName(paths[0]);
        if (!fileDropdown.choices.Contains(filename))
        {
            fileDropdown.choices.Add(filename);
            fileDropdown.index = fileDropdown.choices.Count - 1;
            txts.Add(File.ReadAllText(paths[0]));
            filePaths.Add(paths[0]);
        }
        else
        {
            fileDropdown.index = fileDropdown.choices.IndexOf(filename);
            txts[fileDropdown.index] = File.ReadAllText(paths[0]);
            
        }
        if (fileDropdown.index == lastIndex) { TXT.value = txts[fileDropdown.index]; }

    }

    private void SaveFile()
    {
        if (filePaths.Count > 0)
        {
            File.WriteAllText(filePaths[fileDropdown.index], TXT.value);
        }
        else
        {
            SaveAs();
        }
        
    }

    private void SaveAs()
    {
        var extentionsList = new[]
            {
                new ExtensionFilter("Текстовый документ", "txt")
            };
        StandaloneFileBrowser.SaveFilePanelAsync("Выберите место для сохранения", "", "", extentionsList, (string path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;
            File.WriteAllText(path, TXT.value);
            fileDropdown.choices.Add(Path.GetFileName(path));
            fileDropdown.index = fileDropdown.choices.Count - 1;
            txts.Add(TXT.value);
            filePaths.Add(path);
        });
        
    }

    public void Show()
    {
        if (windowRoot.parent == null)
            root.Q("overlay").Add(windowRoot);
        windowRoot.style.display = DisplayStyle.Flex;

        windowRoot.RegisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
        UIBlocker.AddNewModalWindow(windowRoot);
        StatusManager.SetInputMode(true);
    }

    private void OnWindowSizeChanged(GeometryChangedEvent evt)
    {
        var rootElement = windowRoot.Q("Tablet");
        float windowWidth = rootElement.resolvedStyle.width;
        float windowHeight = rootElement.resolvedStyle.height;
        float screenWidth = root.resolvedStyle.width;
        float screenHeight = root.resolvedStyle.height;

        // Центрируем окно
        windowRoot.style.left = (screenWidth - windowWidth) / 2;
        windowRoot.style.top = (screenHeight - windowHeight) / 2;
        windowRoot.UnregisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);
    }

    private void NumberL()
    {
        LineNumber.value = "";
        for (int i = 1; i <= NumberOfLines; i++) 
        {
            LineNumber.value += $"{i}\n";
        }

    }
    private void RegisterButtons()
    {
        var closeBtn = windowRoot.Q<Button>("close-tablet-button");
        closeBtn.clicked += () => { Close(); };
        var openFileBtn = windowRoot.Q<Button>("open-file-button");
        openFileBtn.clicked += () => { OpenFile(); };
        var saveFileBtn = windowRoot.Q<Button>("save-file-button");
        saveFileBtn.clicked += () => { SaveFile(); };
        var saveAsBtn = windowRoot.Q<Button>("save-as-button");
        saveAsBtn.clicked += () => { SaveAs(); };

        var ptpPointBtn = windowRoot.Q<Button>("ptp-point-button");
        ptpPointBtn.clicked += () => { ptp_point(); };
        var linPointBtn = windowRoot.Q<Button>("lin-point-button");
        linPointBtn.clicked += () => { lin_point(); };
        var ifStatementBtn = windowRoot.Q<Button>("if-button");
        ifStatementBtn.clicked += () => { if_statement(); };
        var waitBtn = windowRoot.Q<Button>("wait-button");
        waitBtn.clicked += () => { wait(); };
        var waitForBtn = windowRoot.Q<Button>("wait-for-button");
        waitForBtn.clicked += () => { wait_for(); };
        var forBtn = windowRoot.Q<Button>("cycle-button");
        forBtn.clicked += () => { for_cycle(); };

        var inBtn = windowRoot.Q<Button>("in-button");
        inBtn.clicked += () => { input(); };
        var outBtn = windowRoot.Q<Button>("out-button");
        outBtn.clicked += () => { output(); };
        var variableBtn = windowRoot.Q<Button>("variable-button");
        variableBtn.clicked += () => { variable(); };
        var subprogramBtn = windowRoot.Q<Button>("subprogram-button");
        subprogramBtn.clicked += () => { subprogram(); };

        //var debugBtn = windowRoot.Q<Button>("debug-button");
        //debugBtn.clicked += () => { Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaa"); };
    }

    private void ptp_point()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "PTP_POINT[]\n" + str.Substring(cur);
        TXT.cursorIndex = cur + 12;
    }
    private void lin_point()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "LIN_POINT[]\n" + str.Substring(cur);
        TXT.cursorIndex = cur + 12;
    }
    private void if_statement()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "IF () {\n\n}" + str.Substring(cur);
        TXT.cursorIndex = cur + 8;
    }
    private void wait()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "WAIT[]\n" + str.Substring(cur);
        TXT.cursorIndex = cur + 7;
    }
    private void wait_for()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "WAIT_FOR[]\n" + str.Substring(cur);
        TXT.cursorIndex = cur + 11;
    }
    private void for_cycle()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "FOR () {\n\n}" + str.Substring(cur);
        TXT.cursorIndex = cur + 9;
    }
    private void input()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "IN[] = " + str.Substring(cur);
        TXT.cursorIndex = cur + 7;
    }
    private void output()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "OUT[] = " + str.Substring(cur);
        TXT.cursorIndex = cur + 8;
    }
    private void variable()
    {

    }
    private void subprogram()
    {
        var str = TXT.value;
        var cur = TXT.cursorIndex;
        TXT.value = str.Substring(0, cur) + "SUBPROGRAM[]" + str.Substring(cur);
        TXT.cursorIndex = cur + 12;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StatusManager = ServiceManager.Current.Get<UIStatusManager>();
        root = GetComponent<UIDocument>().rootVisualElement;
        windowRoot = treeAsset.CloneTree();
        tablet = windowRoot.Q<VisualElement>("Tablet");
        fileDropdown = windowRoot.Q<DropdownField>("FileDropdown");
        TXT = windowRoot.Q<TextField>("TXT");
        LineNumber = windowRoot.Q<TextField>("LineNumber");
        
        TXT.RegisterCallback<ChangeEvent<string>>(a =>
        {
            var ab = a.newValue.Count(b => b == '\n') + 1;
            if (NumberOfLines != ab)
            {
                NumberOfLines = ab;
                NumberL();
            }
        });

        fileDropdown.RegisterCallback<ChangeEvent<string>>(a =>
        {
            if (lastIndex != fileDropdown.index)
            {
                txts[lastIndex] = TXT.value;
                TXT.value = txts[fileDropdown.index];
                lastIndex = fileDropdown.index;
            }
        });

        EnableDrag();
        RegisterButtons();
        Show();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

