using UnityEngine;
using UnityEngine.UIElements;

public class PerspectiveButton : VisualElement
{

    public new class UxmlFactory : UxmlFactory<PerspectiveButton, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private readonly UxmlFloatAttributeDescription _topRatio = new UxmlFloatAttributeDescription
        {
            name = "TopRatio",
            defaultValue = 0.5f
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var trapezoid = ve as PerspectiveButton;
            if (trapezoid != null)
            {
                trapezoid.TopRatio = _topRatio.GetValueFromBag(bag, cc);
            }
        }
    }
    private Color32 _normalTint = new Color32(113, 113, 113, 190);
    private Color32 _hoverTint = new Color32(255, 255, 255, 50);
    private bool _isHovered = false;
    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered != value)
            {
                _isHovered = value;
                MarkDirtyRepaint(); // перерисовать трапецию с новым цветом
            }
        }
    }
    // Верхняя ширина относительно нижней (0..1)
    public float TopRatio
    {
        get => _topRatio;
        set
        {
            var clamped = Mathf.Clamp01(value);
            if (!Mathf.Approximately(_topRatio, clamped))
            {
                _topRatio = clamped;
                MarkDirtyRepaint();
            }
        }
    }
    private float _topRatio = 0.5f;

    private readonly Vertex[] vertices = new Vertex[4];
    private readonly ushort[] indices = { 0, 1, 2, 2, 3, 0 };

    public PerspectiveButton()
    {
        generateVisualContent += GenerateVisualContent;
        // Чтобы элемент имел Layout сразу, полезно включить это,
        // но обычно не обязательно:
        pickingMode = PickingMode.Position;
    }

    private void GenerateVisualContent(MeshGenerationContext mgc)
    {
        // --- Важно: используем layout.width/height (не contentRect) ---
        // layout — это прямоугольник без учёта CSS transform (rotate/scale),
        // поэтому размеры тут стабильны и не меняются при rotate.
        float w = layout.width;
        float h = layout.height;

        // если layout ещё не установлен (например, 0), можно fallback на contentRect
        if (w <= 0f) w = contentRect.width;
        if (h <= 0f) h = contentRect.height;

        // Верхняя ширина и смещение для центрирования
        float topWidth = Mathf.Clamp01(TopRatio) * w;
        float offsetX = (w - topWidth) / 2f;

        // Позиции в локальных координатах элемента (0..w, 0..h)
        // Угол у вершины сохраняется, потому что мы всегда считаем от этих неизменных
        // ширины/высоты элемента (layout).
        // Вершины: 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
        vertices[0].position = new Vector3(0f, h, Vertex.nearZ);                 // bottom-left
        vertices[1].position = new Vector3(offsetX, 0f, Vertex.nearZ);           // top-left
        vertices[2].position = new Vector3(offsetX + topWidth, 0f, Vertex.nearZ);// top-right
        vertices[3].position = new Vector3(w, h, Vertex.nearZ);                 // bottom-right

        // Цветы — можно убрать или сделать свойство
        Color32 fillColor = _isHovered ? _hoverTint : _normalTint;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i].tint = fillColor;

        MeshWriteData mwd = mgc.Allocate(vertices.Length, indices.Length);
        mwd.SetAllVertices(vertices);
        mwd.SetAllIndices(indices);
    }
    public bool IsPointInside(Vector2 localPos)
    {
        if (!visible)
            return false;

        // Быстрая проверка через contentRect
        Rect bounds = contentRect;

        // Если contentRect нулевой, возможно, элемент еще не скомпонован
        if (bounds.width <= 0 || bounds.height <= 0)
            return false;

        // Проверяем, находится ли точка в bounding box
        if (!bounds.Contains(localPos))
            return false;

        // Детальная проверка трапеции
        float w = bounds.width;
        float h = bounds.height;
        float topWidth = Mathf.Clamp01(TopRatio) * w;
        float offsetX = (w - topWidth) / 2f;

        Vector2[] poly = new Vector2[4];
        poly[0] = new Vector2(0f, h);
        poly[1] = new Vector2(offsetX, 0f);
        poly[2] = new Vector2(offsetX + topWidth, 0f);
        poly[3] = new Vector2(w, h);

        return IsPointInTriangle(localPos, poly[0], poly[1], poly[3]) ||
               IsPointInTriangle(localPos, poly[1], poly[2], poly[3]);
    }

    private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = c - a;
        Vector2 v1 = b - a;
        Vector2 v2 = p - a;

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        // Если треугольник вырожденный (нулевая площадь)
        float denominator = dot00 * dot11 - dot01 * dot01;
        if (Mathf.Abs(denominator) < Mathf.Epsilon)
            return false;

        float invDenom = 1f / denominator;
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= -Mathf.Epsilon) && (v >= -Mathf.Epsilon) && (u + v <= 1 + Mathf.Epsilon);
    }


}

