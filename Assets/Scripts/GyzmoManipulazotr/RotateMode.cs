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

public class RotateMode : IManipulatorMode 
{ 
    private Quaternion startRot; 
    private Vector3 mouseStart;
    private Vector3 ringTangentScreen; 
    private float sensitivity;
    public RotateMode(float sens = 1f)
    { 
        sensitivity = sens; 
    }
    public void OnObjectSelected(Transform t, GyzmoManupulator m) 
    {
    } 
    public void OnHandleDown(AxisHandle handle)
    { 
        var t = handle.manipulator.Target; 
        var manip = handle?.manipulator;
        if (t == null || manip == null) return;
        startRot = t.rotation; mouseStart = Input.mousePosition; 
        Vector3 axis = handle.direction.normalized; 
        if (manip.CurrentAxisMode == AxisMode.Global) 
            manip.gizmoRootStartRotation = manip.gizmoRoot.rotation;
        Vector3 camDir = (Camera.main.transform.position - t.position).normalized; 
        Vector3 ringTangent = Vector3.Cross(camDir, axis).normalized * -1f;
        Vector3 s0 = Camera.main.WorldToScreenPoint(t.position); 
        Vector3 s1 = Camera.main.WorldToScreenPoint(t.position + ringTangent); 
        ringTangentScreen = (s1 - s0).sqrMagnitude < 0.001f ? Vector3.right : (s1 - s0).normalized; 
    }
    public void OnHandleDrag(AxisHandle handle, Vector3 d, Vector3 s)
    {
        var t = handle.manipulator.Target;
        var manip = handle.manipulator;
        if (t == null) return;

        Vector3 axis = handle.direction.normalized;

        if (manip.CurrentAxisMode == AxisMode.Local)
        {
            Vector3 localAxisWorld = t.TransformDirection(axis);

            Vector3 mouseDelta = Input.mousePosition - mouseStart;
            mouseStart = Input.mousePosition; // обновляем для следующего кадра

            // Берём только горизонтальную или вертикальную составляющую мыши
            float projected = mouseDelta.x; // например, движение мыши по X
            float angle = Mathf.Clamp(projected * sensitivity, -5f, 5f);

            Quaternion delta = Quaternion.AngleAxis(angle, localAxisWorld);
            t.rotation = delta * t.rotation;
            manip.gizmoRoot.rotation = t.rotation;
        }
        else
        {
            // глобальный режим оставляем прежним
            Vector3 mouseDelta = Input.mousePosition - mouseStart;
            float ang = Vector3.Dot(mouseDelta, ringTangentScreen) * sensitivity;
            Quaternion delta = Quaternion.AngleAxis(ang, axis);
            t.rotation = delta * startRot;
            manip.gizmoRoot.rotation = delta * manip.gizmoRootStartRotation;
        }
    }

    public void OnHandleUp(AxisHandle handle)
    {
        var manip = handle?.manipulator;
        if (manip == null) return;

        if (manip.CurrentAxisMode == AxisMode.Global)
            manip.gizmoRoot.rotation = manip.gizmoRootStartRotation; // возвращаем визуально к глобальной ориентации
    }
}