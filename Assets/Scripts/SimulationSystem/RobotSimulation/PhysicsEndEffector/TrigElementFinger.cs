using UnityEngine;

public class ChildTrigger : MonoBehaviour
{
    public TrigFingerParent parentManager; // —сылка на родительский менеджер

    private void OnTriggerEnter(Collider other)
    {
        if (parentManager != null)
        {
            parentManager.OnChildTriggerEnter(other, gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (parentManager != null)
        {
            parentManager.OnChildTriggerExit(other, gameObject);
        }
    }
}