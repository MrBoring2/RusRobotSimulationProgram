using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class CameraPanController : MonoBehaviour
{
    [SerializeField] private ObjectPicker picker;
    [SerializeField] private UIBlocker uiBlocker;
    [SerializeField] private float panSpeed = 0.01f;

    public Camera cam;
    private Vector3 lastMouse;


    void Update()
    {
        if (uiBlocker != null && uiBlocker.isPointerOverUI)
            return;

        //if (picker != null && picker.IsDraggingManipulator)
        //    return;
        var a = picker.GetManipulatorMode();
        if (picker != null && picker.GetManipulatorMode() != null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            lastMouse = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMouse;
            lastMouse = Input.mousePosition;

            Pan(delta);
        }
    }

    private void Pan(Vector3 mouseDelta)
    {
        float distanceFactor = cam.transform.parent.transform.position.magnitude;

        Vector3 right = cam.transform.parent.transform.right;
        Vector3 up = cam.transform.parent.transform.up;

        Vector3 move =
            (-right * mouseDelta.x - up * mouseDelta.y)
            * panSpeed * distanceFactor;

        cam.transform.parent.transform.position += move;
    }
}
