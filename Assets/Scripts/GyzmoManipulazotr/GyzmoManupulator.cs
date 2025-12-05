using UnityEngine;

public class GyzmoManupulator : MonoBehaviour
{
    [SerializeField]
    private float gyzmoScaleKoeficient = 0.1f;
    public Transform Target;
    public Camera cam;

    public void Attach(Transform obj)
    {
        Target = obj;
        transform.position = obj.position;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Target != null)
            transform.position = Target.position;

        // Повернуть гизмо к камере (как в Unity Editor)
        //transform.rotation = Quaternion.LookRotation(cam.transform.forward);
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        transform.localScale = Vector3.one * distance * gyzmoScaleKoeficient;
    }

}
