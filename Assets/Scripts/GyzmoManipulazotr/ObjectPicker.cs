using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class ObjectPicker : MonoBehaviour
{
    public GyzmoManupulator manipulator;
    private AxisHandle currentHandle;

    private void Start()
    {
        if (manipulator != null)
            manipulator.gameObject.SetActive(false);
    }

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int manipLayerMask = LayerMask.GetMask("Manipulator");

        if (Input.GetMouseButtonDown(0))
        {
            // Клик по манипулятору
            if (Physics.Raycast(ray, out RaycastHit hitHandle, Mathf.Infinity, manipLayerMask))
            {
                Debug.Log(hitHandle.transform.gameObject.name);
                AxisHandle handle = hitHandle.collider.GetComponent<AxisHandle>();
                if (handle != null)
                {
                    currentHandle = handle;
                    currentHandle.StartDrag();
                }
            }
            // Клик по объекту сцены
            else if (Physics.Raycast(ray, out RaycastHit hitObject))
            {
                manipulator.Attach(hitObject.transform);
                manipulator.gameObject.SetActive(true);
            }
            // Клик по пустому месту сцены
            else
            {
                // Тут мы не проверяем UI вообще — манипулятор пропадёт только при клике на пустую сцену
                manipulator.gameObject.SetActive(false);
            }
        }

        // Перетаскивание манипулятора
        if (Input.GetMouseButton(0) && currentHandle != null)
            currentHandle.UpdateDrag();

        // Отпускание
        if (Input.GetMouseButtonUp(0) && currentHandle != null)
        {
            currentHandle.EndDrag();
            currentHandle = null;
        }
    }
}


