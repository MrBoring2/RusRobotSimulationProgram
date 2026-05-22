using UnityEngine;
using UnityEngine.EventSystems;

public class MoveMode : IManipulatorMode
{
    public void OnObjectSelected(Transform target, GizmoManupulator manipulator) { }

    public void OnHandleDown(AxisHandle handle) { }

    public void OnHandleDrag(AxisHandle handle, Vector3 delta, Vector3 startPos)
    {
        var t = handle.manipulator.Target;
        var manip = handle.manipulator;
        if (t == null || manip == null) return;

        if (handle.type == HandleType.Axis)
        {
            Vector3 moveDir;

            if (manip.CurrentAxisMode == AxisMode.Local)
            {
                // ¬ локальном режиме: берем мировое направление оси
                moveDir = t.TransformDirection(handle.direction.normalized);
            }
            else
            {
                // ¬ глобальном режиме: мировое направление
                moveDir = handle.direction.normalized;
            }

            // —читаем проекцию движени€ мыши на выбранную ось
            float moveAmount = Vector3.Dot(delta, moveDir);
            t.position = startPos + moveDir * moveAmount;
        }
        else
        {
            Vector3 planeNormal;

            if (manip.CurrentAxisMode == AxisMode.Local)
            {
                // ¬ локальном режиме: берем мировую нормаль плоскости
                planeNormal = t.TransformDirection(handle.planeNormal);
            }
            else
            {
                // ¬ глобальном режиме: мировую нормаль
                planeNormal = handle.planeNormal;
            }

            // ƒл€ плоскости всегда проецируем движение на плоскость
            t.position = startPos + Vector3.ProjectOnPlane(delta, planeNormal);
        }
    }

    public void OnHandleUp(AxisHandle handle) { }
}