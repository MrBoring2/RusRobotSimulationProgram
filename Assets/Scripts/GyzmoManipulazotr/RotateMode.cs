using TMPro;
using UnityEngine;

public class RotateMode : IManipulatorMode
{
    private Quaternion startRotation;      // начальный поворот объекта
    private Quaternion gizmoStartRotation; // начальный поворот гизмо
    private Vector3 mouseStart;            // стартовая позиция мыши
    private Vector3 screenTangent;         // касательная для проекции
    private Vector3 axisWorld;             // ось вращения
    private float sensitivity;             // чувствительность вращения
    private Vector3 axisLocal;
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
        axisLocal = handle.direction.normalized;
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
        string axisLetter = GetAxisLetter(axisLocal); ;
        cursorAngleText.text = $"{axisLetter}: {angle:F1}°";

        // Обновляем позицию и цвет
        UpdateTextPositionAndRotation(axisWorld);
    }
    private string GetAxisLetter(Vector3 localAxis)
    {
        if (Vector3.Dot(localAxis, Vector3.right) > 0.99f) return "X";
        if (Vector3.Dot(localAxis, Vector3.up) > 0.99f) return "Y";
        if (Vector3.Dot(localAxis, Vector3.forward) > 0.99f) return "Z";
        return "?";
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

        Transform cam = Camera.main.transform;

        // позиция
        cursorAngleText.transform.position =
            targetTransform.position + Vector3.up * 0.5f;

        // 🔥 billboard БЕЗ зеркала
        cursorAngleText.transform.rotation =
            Quaternion.LookRotation(
                cursorAngleText.transform.position - cam.position,
                cam.up
            );

        // Меняем цвет в зависимости от оси
        SetAxisColor(axisLocal);
    }
    private void SetAxisColor(Vector3 localAxis)
    {
        if (Vector3.Dot(localAxis, Vector3.forward) > 0.99f)
            cursorAngleText.color = Color.red;
        else if (Vector3.Dot(localAxis, Vector3.right) > 0.99f)
            cursorAngleText.color = Color.green;
        else if (Vector3.Dot(localAxis, Vector3.up) > 0.99f)
            cursorAngleText.color = Color.blue;
        else
            cursorAngleText.color = Color.white;
    }


    private void UpdateTextBillboard()
    {

    }
}