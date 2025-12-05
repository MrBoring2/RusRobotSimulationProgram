using UnityEngine;
using UnityEngine.UIElements;
public enum HandleType { Axis, Plane }

public class AxisHandle : MonoBehaviour
{
    public HandleType type;
    public Vector3 direction;     // дл€ Axis, мирова€ ось (X/Y/Z)
    public Vector3 planeNormal;   // дл€ Plane
    public GyzmoManupulator manipulator;

    private bool dragging;
    private Vector3 dragStartPos;          // позици€ объекта в момент начала перетаскивани€
    private Vector3 dragStartMouseWorld;   // точка пересечени€ луча с плоскостью на старте

    public void StartDrag()
    {
        if (manipulator.Target == null) return;

        dragging = true;
        dragStartPos = manipulator.Target.position;

        Plane plane = GetDragPlane();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        plane.Raycast(ray, out float enter);
        dragStartMouseWorld = ray.GetPoint(enter);
    }

    public void UpdateDrag()
    {
        if (!dragging || manipulator.Target == null) return;

        Plane plane = GetDragPlane();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        plane.Raycast(ray, out float enter);
        Vector3 currentMouseWorld = ray.GetPoint(enter);

        Vector3 delta = currentMouseWorld - dragStartMouseWorld;

        if (type == HandleType.Axis)
        {
            float move = Vector3.Dot(delta, direction.normalized);
            manipulator.Target.position = dragStartPos + direction.normalized * move;
        }
        else
        {
            manipulator.Target.position = dragStartPos + Vector3.ProjectOnPlane(delta, planeNormal);
        }
    }

    public void EndDrag()
    {
        dragging = false;
    }

    private Plane GetDragPlane()
    {
        if (type == HandleType.Axis)
        {
            // ѕлоскость перпендикул€рна оси и проходит через объект
            Vector3 normal = Vector3.Cross(direction.normalized, Vector3.up);
            if (normal.sqrMagnitude < 0.001f)
                normal = Vector3.Cross(direction.normalized, Vector3.forward);
            return new Plane(normal, dragStartPos);
        }
        else
        {
            return new Plane(planeNormal, dragStartPos);
        }
    }
}