using UnityEngine;
using UnityEngine.UIElements;

public class PrimitivePropertyProvider : MonoBehaviour, IPropertyProvider
{
    public string Name { get => gameObject.name; set => gameObject.name = Name; }
    public Vector3 Position { get => transform.position; set => transform.position = value; }
    private Vector3 rotationEuler; // ÈÑÒÈÍÀ

    public Vector3 Rotation
    {
        get => transform.rotation.eulerAngles;
        set
        {
            rotationEuler = new Vector3(
                Mathf.Repeat(value.x, 361f),
                Mathf.Repeat(value.y, 361f),
                Mathf.Repeat(value.z, 361f)
            );
            transform.rotation = Quaternion.Euler(rotationEuler);
        }
    }
    public Vector3 Scale { get => transform.localScale; set => transform.localScale = value; }

    public void BuildCustomProperties(VisualElement root)
    {
        
    }

    private void Awake()
    {
        rotationEuler = transform.eulerAngles;
    }
}
