using TMPro;
using UnityEngine;

//public class RotateMode : IManipulatorMode
//{
//    private Quaternion startRotation;         // исходная ориентация объекта
//    private Quaternion previewRotation;       // для окончательного применения
//    private float sensitivity = 0.3f;

//    private Vector3 mouseStart;               // стартовая позиция мыши
//    private Vector3 ringTangentScreen;        // касательная кольца на экране

//    private LineRenderer previewLine;         // линия предпросмотра
//    private int previewSegments = 50;
//    private float previewRadiusOffset = 1f;

//    private float currentAngle = 0f;          // угол для предпросмотра

//    public RotateMode(float sensitivity = 1f)
//    {
//        this.sensitivity = sensitivity;
//    }

//    public void OnObjectSelected(Transform target, GyzmoManupulator manipulator)
//    {
//        // Создаем LineRenderer для предпросмотра, если его нет
//        if (previewLine == null && target != null)
//        {
//            GameObject previewObj = new GameObject("GizmoPreviewLine");
//            previewObj.transform.SetParent(target, false);
//            previewLine = previewObj.AddComponent<LineRenderer>();
//            previewLine.useWorldSpace = true;
//            previewLine.startWidth = 0.01f;
//            previewLine.endWidth = 0.01f;
//            previewLine.material = new Material(Shader.Find("Unlit/Color"));
//            previewLine.material.color = new Color(1f, 0.5f, 0f, 0.7f);
//            previewLine.positionCount = 0;
//        }
//    }

//    public void OnHandleDown(AxisHandle handle)
//    {
//        if (handle == null || handle.manipulator?.Target == null) return;

//        Transform target = handle.manipulator.Target;
//        Vector3 axis = handle.direction.normalized;

//        startRotation = target.rotation;   // сохраняем текущую ориентацию
//        mouseStart = Input.mousePosition;
//        currentAngle = 0f;

//        // направление камеры
//        Vector3 camDir = (Camera.main.transform.position - target.position).normalized;

//        // касательная кольца в 3D: перпендикуляр к оси и направлению камеры
//        Vector3 ringTangent = Vector3.Cross(camDir, axis).normalized * -1f;

//        // проекция касательной на экран
//        Vector3 screenStart = Camera.main.WorldToScreenPoint(target.position);
//        Vector3 screenEnd = Camera.main.WorldToScreenPoint(target.position + ringTangent);
//        ringTangentScreen = (screenEnd - screenStart);
//        ringTangentScreen.z = 0;

//        if (ringTangentScreen.sqrMagnitude > 1e-6f)
//            ringTangentScreen.Normalize();
//        else
//            ringTangentScreen = Vector3.right;

//        // очищаем предыдущий предпросмотр
//        if (previewLine != null)
//            previewLine.positionCount = 0;
//    }

//    public void OnHandleDrag(AxisHandle handle, Vector3 delta, Vector3 startPos)
//    {
//        if (handle == null || handle.manipulator?.Target == null) return;
//        if (handle.type != HandleType.Axis) return;

//        // движение мыши
//        Vector3 mouseDelta = Input.mousePosition - mouseStart;
//        float projected = Vector3.Dot(mouseDelta, ringTangentScreen);

//        // вычисляем угол предпросмотра
//        currentAngle = projected * sensitivity;

//        // не трогаем объект! Только визуальный предпросмотр
//        DrawPreviewArc(handle, currentAngle);
//    }

//    public void OnHandleUp(AxisHandle handle)
//    {
//        if (handle?.manipulator?.Target == null) return;

//        // теперь применяем вращение
//        Transform target = handle.manipulator.Target;
//        Vector3 axis = handle.direction.normalized;
//        previewRotation = Quaternion.AngleAxis(currentAngle, axis) * startRotation;
//        target.rotation = previewRotation;

//        // убираем дугу предпросмотра
//        if (previewLine != null)
//            previewLine.positionCount = 0;
//    }

//    private void DrawPreviewArc(AxisHandle handle, float angle)
//    {
//        if (previewLine == null || handle.manipulator?.Target == null) return;

//        Transform target = handle.manipulator.Target;
//        Vector3 axis = handle.direction.normalized;
//        Vector3 center = target.position;

//        float radius = Vector3.Distance(handle.transform.position, center) + previewRadiusOffset;

//        previewLine.positionCount = previewSegments + 1;
//        for (int i = 0; i <= previewSegments; i++)
//        {
//            float t = i / (float)previewSegments;
//            float a = Mathf.Lerp(0f, angle, t) * Mathf.Deg2Rad;
//            Vector3 dir = (handle.transform.position - center).normalized * radius;
//            Vector3 point = Quaternion.AngleAxis(Mathf.Rad2Deg * a, axis) * dir + center;
//            previewLine.SetPosition(i, point);
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class RotateMode : IManipulatorMode
{
    private Quaternion startRotation;      // начальный поворот объекта
    private Quaternion gizmoStartRotation; // начальный поворот гизмо
    private Vector3 mouseStart;            // стартовая позиция мыши
    private Vector3 screenTangent;         // касательная для проекции
    private Vector3 axisWorld;             // ось вращения
    private float sensitivity;             // чувствительность вращения

    // UI элемент для отображения угла рядом с курсором
    public TextMeshPro cursorAngleText;           // или TextMeshProUGUI
    private Transform targetTransform;
    public RotateMode(float sens = 0.3f)
    {
        sensitivity = sens;
    }

    // Вызывается при выборе объекта
    public void OnObjectSelected(Transform t, GyzmoManupulator m)
    {
        targetTransform = t;
        if (m != null)
            gizmoStartRotation = m.gizmoRoot.rotation;
    }

    // Начало вращения
    public void OnHandleDown(AxisHandle handle)
    {
        if (handle?.manipulator?.Target == null) return;

        Transform t = handle.manipulator.Target;

        // Ось вращения (локальная или глобальная)
        axisWorld =
            handle.manipulator.CurrentAxisMode == AxisMode.Local
                ? t.TransformDirection(handle.direction).normalized
                : handle.direction.normalized;

        startRotation = t.rotation;
        mouseStart = Input.mousePosition;

        // Касательная к кольцу
        Vector3 camDir = (Camera.main.transform.position - t.position).normalized;
        Vector3 tangentWorld = Vector3.Cross(axisWorld, camDir).normalized;
        if (tangentWorld.sqrMagnitude < 1e-6f)
            tangentWorld = Vector3.Cross(axisWorld, Vector3.up).normalized;

        // Проекция на экран
        Vector3 p0 = Camera.main.WorldToScreenPoint(t.position);
        Vector3 p1 = Camera.main.WorldToScreenPoint(t.position + tangentWorld);
        screenTangent = (p1 - p0);
        screenTangent.z = 0;
        if (screenTangent.sqrMagnitude > 1e-6f)
            screenTangent.Normalize();
        else
            screenTangent = Vector3.right;

        gizmoStartRotation = handle.manipulator.gizmoRoot.rotation;

        // Показываем текст рядом с точкой клика в мире
        cursorAngleText.gameObject.SetActive(true);
        cursorAngleText.text = "0°";
        UpdateTextPositionAndRotation(axisWorld);
    }

    // Движение мыши — вращение объекта
    public void OnHandleDrag(AxisHandle handle, Vector3 delta, Vector3 startPos)
    {
        if (handle?.manipulator?.Target == null) return;

        Transform t = handle.manipulator.Target;
        Vector3 mouseDelta = Input.mousePosition - mouseStart;
        float projected = Vector3.Dot(mouseDelta, screenTangent);
        float angle = projected * sensitivity;

        // Вращаем объект
        t.rotation = Quaternion.AngleAxis(angle, axisWorld) * startRotation;

        // Вращаем гизмо
        handle.manipulator.gizmoRoot.rotation =
            Quaternion.AngleAxis(angle, axisWorld) * gizmoStartRotation;

        // Обновляем текст возле курсора
        string axisLetter = "X";
        if (axisWorld == Vector3.right) axisLetter = "X";
        else if (axisWorld == Vector3.up) axisLetter = "Y";
        else if (axisWorld == Vector3.forward) axisLetter = "Z";
        cursorAngleText.text = $"{axisLetter}: {angle:F1}°";

        // Обновляем позицию и цвет
        UpdateTextPositionAndRotation(axisWorld);
    }

    // Отпускание мыши — фиксируем вращение, скрываем текст
    public void OnHandleUp(AxisHandle handle)
    {
        if (handle?.manipulator == null) return;

        handle.manipulator.gizmoRoot.rotation = gizmoStartRotation;

        // Скрываем текст
        cursorAngleText.gameObject.SetActive(false);
    }
    private void UpdateTextPositionAndRotation(Vector3 axis)
    {
        if (cursorAngleText == null || targetTransform == null) return;

        // Позиция чуть выше объекта
        cursorAngleText.transform.position = targetTransform.position + Vector3.up * 0.5f;

        // Поворачиваем к камере
        cursorAngleText.transform.rotation = Quaternion.LookRotation(
            cursorAngleText.transform.position - Camera.main.transform.position
        );

        // Меняем цвет в зависимости от оси
        if (axis == Vector3.right)
            cursorAngleText.color = Color.red;
        else if (axis == Vector3.up)
            cursorAngleText.color = Color.green;
        else if (axis == Vector3.forward)
            cursorAngleText.color = Color.blue;
        else
            cursorAngleText.color = Color.white; // для произвольной оси
    }
}