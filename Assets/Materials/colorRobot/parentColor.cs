using UnityEngine;
using System.Collections.Generic;

public class ParentColorController : MonoBehaviour
{
    [Header("Цвет для всех дочерних объектов")]
    public Color OldCol = Color.white;
    private MeshRenderer MR;
    [Header("Настройки")]
    public bool applyOnStart = true;
    public bool applyToChildren = true;
    public bool includeInactive = false;

    private List<MeshRenderer> childRenderers = new List<MeshRenderer>();

    void Start()
    {
        MR = gameObject.GetComponent<MeshRenderer>();
        OldCol = MR.sharedMaterial.color;
        if (applyOnStart)
        {
           
            ApplyColorToAll();
        }
    }
    private void FixedUpdate()
    {
        if (OldCol != MR.sharedMaterial.color)
        {
            ApplyColorToAll();
            OldCol = MR.sharedMaterial.color; ;

        }
    }

    [ContextMenu("Apply Color")]
    public void ApplyColorToAll()
    {
        // Находим все MeshRenderer у дочерних объектов
        if (applyToChildren)
        {
            childRenderers.Clear();
            childRenderers.AddRange(GetComponentsInChildren<MeshRenderer>(includeInactive));
        }
        else
        {
            // Только у текущего объекта
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                childRenderers = new List<MeshRenderer> { renderer };
        }

        // Применяем цвет
        foreach (var renderer in childRenderers)
        {
            renderer.sharedMaterial.color = MR.sharedMaterial.color;
        }

        Debug.Log($"Цвет применен к {childRenderers.Count} объектам");
    }

    // Для изменения цвета в реальном времени в редакторе
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyColorToAll();
    }
#endif
}