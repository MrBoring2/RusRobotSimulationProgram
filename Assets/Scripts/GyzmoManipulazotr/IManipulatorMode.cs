using UnityEngine;

public interface IManipulatorMode
{
    void OnObjectSelected(Transform target, GyzmoManupulator manupulator);
    void OnHandleDown(AxisHandle handle);
    void OnHandleDrag(AxisHandle handle, Vector3 delta, Vector3 startPos);
    void OnHandleUp(AxisHandle handle);
}
