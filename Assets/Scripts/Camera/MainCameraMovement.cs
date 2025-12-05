using UnityEngine;
using UnityEngine.InputSystem;

public class MainCameraMovement : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float movementSpeed = 10f;

    [Header("Мышь")]
    [SerializeField] private float mouseSensitivity = 20f;
    //[SerializeField] private float minPitch = -89f;
    //[SerializeField] private float maxPitch = 89f;

    [Header("Перспектива")]
    [SerializeField] private float buttonRotationSpeedDegPerSec = 360f;

    [Header("Камера")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float orthographicSize = 10f;

    private float pitch; // X
    private float yaw;   // Y

    private Vector3 targetEulerAngles;
    private bool isButtonRotating = false;
    private bool orthographicMode = false;

    private void Start()
    {
        Vector3 e = transform.localEulerAngles;
        pitch = NormalizeAngle(e.x);
        yaw = NormalizeAngle(e.y);
        targetEulerAngles = new Vector3(pitch, yaw, 0f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseRotation();
        HandleButtonRotation();
    }

    private void HandleMovement()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) input += Vector3.back;
        if (Input.GetKey(KeyCode.A)) input += Vector3.left;
        if (Input.GetKey(KeyCode.D)) input += Vector3.right;

        if (input != Vector3.zero)
        {
            input.Normalize();

            Vector3 move =
                transform.forward * input.z +
                transform.right * input.x;

            transform.position += move * movementSpeed * Time.deltaTime;
        }
    }

    private void HandleMouseRotation()
    {
        if (Input.GetMouseButton(1))
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * 60f; 
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * 60f;

            yaw += mx;
            pitch -= my;
            //pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Quaternion q = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = q;

            isButtonRotating = false;

            targetEulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    private void HandleButtonRotation()
    {
        if (!isButtonRotating) return;

        Quaternion targetRot = Quaternion.Euler(NormalizeAngle(targetEulerAngles.x),
                                                NormalizeAngle(targetEulerAngles.y),
                                                0f);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, buttonRotationSpeedDegPerSec * Time.deltaTime);

        float angle = Quaternion.Angle(transform.rotation, targetRot);
        if (angle < 0.5f)
        {
            transform.rotation = targetRot;
            isButtonRotating = false;

            Vector3 e = transform.localEulerAngles;
            pitch = NormalizeAngle(e.x);
            yaw = NormalizeAngle(e.y);
            targetEulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    public void RotateToView(Vector3 eulerAngles)
    {
        if (eulerAngles.x <= -90f) eulerAngles.x = -89.999f;
        if (eulerAngles.x >= 90f) eulerAngles.x = 89.999f;

        targetEulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, 0f);
        isButtonRotating = true;
    }

    public void ToggleOrthographic()
    {
        orthographicMode = !orthographicMode;
        if (mainCamera != null)
        {
            mainCamera.orthographic = orthographicMode;
            mainCamera.orthographicSize = orthographicSize;
        }
    }

    private static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a <= -180f) a += 360f;
        return a;
    }
}
