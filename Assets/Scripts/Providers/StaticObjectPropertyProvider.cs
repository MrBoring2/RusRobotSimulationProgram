using UnityEngine;
using UnityEngine.UIElements;

public class StaticObjectPropertyProvider : MonoBehaviour, IPropertyProvider
{
    public string Name { get => gameObject.name; set => gameObject.name = Name; }
    public Vector3 Position { get => transform.position; set => transform.position = Position; }
    public Vector3 Rotation { get => transform.eulerAngles; set => transform.eulerAngles = Rotation; }
    public Vector3 Scale { get => transform.localScale; set => transform.localScale = Scale; }

    public void BuildCustomProperties(VisualElement root)
    {
        
    }
}
