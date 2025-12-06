using UnityEngine;
using UnityEngine.UIElements;
public enum HandleType { Axis, Plane }

public class AxisHandle : MonoBehaviour
{
    public HandleType type;
    public Vector3 direction;     // для Axis
    public Vector3 planeNormal;   // для Plane
    public GyzmoManupulator manipulator;

    private bool dragging;
    private Vector3 dragStartPos;
    private Vector3 dragStartMouseWorld;
    private Plane dragPlane;

    public void StartDrag()
    {
        if (manipulator.Target == null || manipulator.CurrentManipulatorMode == null) return;

        dragging = true;
        dragStartPos = manipulator.Target.position;

        // Определяем правильную плоскость для движения
        dragPlane = GetOptimalDragPlane();

        // Получаем начальную позицию мыши на плоскости
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            dragStartMouseWorld = ray.GetPoint(enter);
        }

        manipulator.CurrentManipulatorMode.OnHandleDown(this);
    }

    public void UpdateDrag()
    {
        if (!dragging || manipulator.Target == null || manipulator.CurrentManipulatorMode == null) return;

        // Получаем текущую позицию мыши на той же плоскости
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 currentMouseWorld = ray.GetPoint(enter);
            Vector3 delta = currentMouseWorld - dragStartMouseWorld;

            // Передаем delta в мировых координатах
            manipulator.CurrentManipulatorMode.OnHandleDrag(this, delta, dragStartPos);
        }
    }

    public void EndDrag()
    {
        if (dragging && manipulator.CurrentManipulatorMode != null)
        {
            manipulator.CurrentManipulatorMode.OnHandleUp(this);
        }
        dragging = false;
    }

    private Plane GetOptimalDragPlane()
    {
        Camera cam = Camera.main;
        Vector3 targetPos = manipulator.Target.position;

        if (type == HandleType.Axis)
        {
            Vector3 axisDir;

            if (manipulator.CurrentAxisMode == AxisMode.Local)
            {
                // В локальном режиме берем направление в мировых координатах
                axisDir = manipulator.Target.TransformDirection(direction.normalized);
            }
            else
            {
                // В глобальном режиме берем мировое направление
                axisDir = direction.normalized;
            }

            // Создаем плоскость, которая всегда хорошо работает с лучом камеры
            Vector3 camForward = cam.transform.forward;
            float dot = Vector3.Dot(camForward, axisDir);

            // Если камера смотрит почти вдоль оси, используем другую плоскость
            if (Mathf.Abs(dot) > 0.9f)
            {
                // Плоскость через объект, перпендикулярно направлению от камеры к объекту
                Vector3 camToTarget = (targetPos - cam.transform.position).normalized;
                return new Plane(camToTarget, targetPos);
            }
            else
            {
                // Обычная плоскость: перпендикулярно камере
                return new Plane(camForward, targetPos);
            }
        }
        else // Plane
        {
            Vector3 normal;

            if (manipulator.CurrentAxisMode == AxisMode.Local)
            {
                // В локальном режиме берем нормаль в мировых координатах
                normal = manipulator.Target.TransformDirection(planeNormal);
            }
            else
            {
                // В глобальном режиме берем мировую нормаль
                normal = planeNormal;
            }

            return new Plane(normal, targetPos);
        }
    }
}