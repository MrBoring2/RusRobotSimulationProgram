using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomServiceManager;
using UnityEngine;

public class OnCollision : MonoBehaviour
{
    EventBus _eventBus => ServiceManager.Current.Get<EventBus>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Detail"))
        {
            _eventBus.Invoke(new CNCCollisionOnEvent(gameObject, other.gameObject));
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Detail"))
        {
            _eventBus.Invoke(new CNCCollisionExitEvent(gameObject, other.gameObject));
        }
    }
}
