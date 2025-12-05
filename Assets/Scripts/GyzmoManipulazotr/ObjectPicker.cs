using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class ObjectPicker : MonoBehaviour
{
    public GyzmoManupulator manipulator;
    private AxisHandle currentHandle;

    private void Start()
    {
        manipulator.gameObject.SetActive(false);
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int manipLayerMask = LayerMask.GetMask("Manipulator");

        if (Input.GetMouseButtonDown(0))
        {
            // Сначала проверяем клик по манипулятору
            if (Physics.Raycast(ray, out RaycastHit hitHandle, Mathf.Infinity, manipLayerMask))
            {
                AxisHandle handle = hitHandle.collider.GetComponent<AxisHandle>();
                if (handle != null)
                {
                    currentHandle = handle;
                    currentHandle.StartDrag();
                }
            }
            else
            {
                // Проверяем клик по объекту сцены (любому, кроме манипулятора)
                if (Physics.Raycast(ray, out RaycastHit hitObject))
                {
                    manipulator.Attach(hitObject.collider.transform);
                    manipulator.gameObject.SetActive(true);
                }
                else
                {
                    // Кликнули по пустому месту → скрываем манипулятор
                    manipulator.gameObject.SetActive(false);
                }
            }
        }

        // Перетаскивание (если зажато)
        if (Input.GetMouseButton(0) && currentHandle != null)
        {
            currentHandle.UpdateDrag();
        }

        // Отпускание мыши
        if (Input.GetMouseButtonUp(0) && currentHandle != null)
        {
            currentHandle.EndDrag();
            currentHandle = null;
        }
    }
}


