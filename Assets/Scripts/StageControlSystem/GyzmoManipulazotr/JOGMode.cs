using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.GyzmoManipulazotr;
using Assets.Scripts.Managers;
using TMPro;
using UnityEngine;

public class JOGMode
{
    private Quaternion startRotation;
    private Quaternion gizmoStartRotation;
    private Vector3 mouseStart;
    private Vector3 screenTangent;
    private Vector3 axisWorld;
    private Vector3 axisLocal;
    private float sensitivity = 0.3f;
    public TextMeshPro cursorAngleText;
    private Transform targetTransform;

    public void OnHandleDown(AxisHandleJOG handle)
    {
        if (handle.posType != JOGHandleType.Rotate) return;
        if (handle?.manipulator?.Target == null) return;

        Transform t = handle.manipulator.Target;
        axisLocal = handle.direction.normalized;

        if (handle.manipulator.CurrentAxisMode == AxisMode.Local)
            axisWorld = t.TransformDirection(handle.direction).normalized;
        else
            axisWorld = handle.direction.normalized;

        startRotation = t.rotation;
        mouseStart = Input.mousePosition;

        Vector3 camDir = (Camera.main.transform.position - t.position).normalized;
        Vector3 tangentWorld = Vector3.Cross(axisWorld, camDir).normalized;
        if (tangentWorld.sqrMagnitude < 1e-6f)
            tangentWorld = Vector3.Cross(axisWorld, Vector3.up).normalized;

        Vector3 p0 = Camera.main.WorldToScreenPoint(t.position);
        Vector3 p1 = Camera.main.WorldToScreenPoint(t.position + tangentWorld);
        screenTangent = (p1 - p0);
        screenTangent.z = 0;
        if (screenTangent.sqrMagnitude > 1e-6f)
            screenTangent.Normalize();
        else
            screenTangent = Vector3.right;

        gizmoStartRotation = handle.manipulator.gizmoRoot.rotation;
        cursorAngleText.gameObject.SetActive(true);
    }

    public void OnHandleDrag(AxisHandleJOG handle, Vector3 d, Vector3 startPos)
    {
                Debug.Log(
            $"CurrentAxisMode={handle.manipulator.CurrentAxisMode} " +
            $"ManagerMode={ServiceManager.Current.Get<AxisModeManager>().Mode}"
        );
        var t = handle.manipulator.Target;
        var manip = handle.manipulator;
        if (t == null || manip == null) return;

        if (handle.posType == JOGHandleType.Move)
        {
            if (handle.directionType == HandleType.Axis)
            {
                Vector3 moveDir;
                if (manip.CurrentAxisMode == AxisMode.Local)
                    moveDir = t.TransformDirection(handle.direction.normalized);
                else
                    moveDir = handle.direction.normalized;
                Debug.Log(
                    $"Mode={manip.CurrentAxisMode} " +
                    $"HandleDir={handle.direction} " +
                    $"MoveDir={moveDir}"
                );
                float moveAmount = Vector3.Dot(d, moveDir);
                t.position = startPos + moveDir * moveAmount;
            }
            else
            {
                Vector3 planeNormal;
                if (manip.CurrentAxisMode == AxisMode.Local)
                    planeNormal = t.TransformDirection(handle.planeNormal);
                else
                    planeNormal = handle.planeNormal;

                t.position = startPos + Vector3.ProjectOnPlane(d, planeNormal);
            }
        }
        else
        {
            Vector3 mouseDelta = Input.mousePosition - mouseStart;
            float projected = Vector3.Dot(mouseDelta, screenTangent);
            float angle = projected * sensitivity;

            if (manip.CurrentAxisMode == AxisMode.Local)
            {
                t.rotation = Quaternion.AngleAxis(angle, axisWorld) * startRotation;
                manip.gizmoRoot.rotation = Quaternion.AngleAxis(angle, axisWorld) * gizmoStartRotation;
            }
            else
            {
                // Global: вращаем вокруг мировой оси относительно НАЧАЛЬНОГО поворота в этом драге
                t.rotation = Quaternion.AngleAxis(angle, axisWorld) * startRotation;
                // gizmoRoot не вращаем в Global
            }

            string axisLetter = GetAxisLetter(axisLocal);
            cursorAngleText.text = $"{axisLetter}: {angle:F1}°";
            SetAxisColor(axisLocal);
        }
    }

    public void OnHandleUp(AxisHandleJOG handle)
    {
        if (handle.posType == JOGHandleType.Rotate)
        {
            handle.manipulator.gizmoRoot.rotation = gizmoStartRotation;
            cursorAngleText.gameObject.SetActive(false);
        }
    }

    private string GetAxisLetter(Vector3 localAxis)
    {
        if (Vector3.Dot(localAxis, Vector3.right) > 0.99f) return "X";
        if (Vector3.Dot(localAxis, Vector3.up) > 0.99f) return "Y";
        if (Vector3.Dot(localAxis, Vector3.forward) > 0.99f) return "Z";
        return "?";
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

    public void OnObjectSelected(Transform target, GizmoManupulator manupulator)
    {
        targetTransform = target;
        if (manupulator != null)
            gizmoStartRotation = manupulator.gizmoRoot.rotation;
    }
}