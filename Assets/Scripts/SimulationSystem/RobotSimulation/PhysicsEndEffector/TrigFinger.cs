using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.CustomEventBus;

public class TrigFingerParent : MonoBehaviour
{
    private EventBus _eventBus;
    public Rigidbody RealFinger;

    // Общий список всех объектов внутри ЛЮБОГО дочернего триггера
    private List<GameObject> collisionObjects = new List<GameObject>();

    // Словарь для отслеживания, какой объект в каком дочернем триггере находится
    private Dictionary<GameObject, int> objectTriggerCount = new Dictionary<GameObject, int>();

    private void Awake()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
    }

    // Этот метод вызывают дочерние триггеры при входе
    public void OnChildTriggerEnter(Collider other, GameObject childTrigger)
    {
        if (!(other.gameObject.CompareTag("SceneObject") || other.gameObject.CompareTag("Составные части робота")))
            return;

        // Увеличиваем счётчик для этого объекта
        if (objectTriggerCount.ContainsKey(other.gameObject))
        {
            objectTriggerCount[other.gameObject]++;
        }
        else
        {
            objectTriggerCount[other.gameObject] = 1;
            collisionObjects.Add(other.gameObject);

            // Объект действительно вошёл в зону (первый триггер)
            _eventBus.Invoke(new RobotCollisionEvent(gameObject, other.gameObject));
            RealFinger.isKinematic = true;
        }

        Debug.Log($"Объект {other.gameObject.name} вошёл. Счётчик: {objectTriggerCount[other.gameObject]}");
    }

    // Этот метод вызывают дочерние триггеры при выходе
    public void OnChildTriggerExit(Collider other, GameObject childTrigger)
    {
        if (!(other.gameObject.CompareTag("SceneObject") || other.gameObject.CompareTag("Составные части робота")))
            return;

        if (objectTriggerCount.ContainsKey(other.gameObject))
        {
            objectTriggerCount[other.gameObject]--;

            Debug.Log($"Объект {other.gameObject.name} вышел из триггера. Счётчик: {objectTriggerCount[other.gameObject]}");

            // Если объект вышел из ВСЕХ дочерних триггеров
            if (objectTriggerCount[other.gameObject] <= 0)
            {
                objectTriggerCount.Remove(other.gameObject);
                collisionObjects.Remove(other.gameObject);

                // Объект действительно покинул зону полностью
                _eventBus.Invoke(new RobotCollisionExitEvent(gameObject, other.gameObject));

                if (collisionObjects.Count == 0)
                {
                    RealFinger.isKinematic = false;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (collisionObjects.Count == 0) return;

        List<GameObject> toRemove = new List<GameObject>();
        foreach (var obj in collisionObjects)
        {
            if (obj == null || obj.activeInHierarchy == false)
            {
                toRemove.Add(obj);
            }
        }

        foreach (var obj in toRemove)
        {
            DeleteCollision(obj);
        }
    }

    private void DeleteCollision(GameObject obj)
    {
        if (objectTriggerCount.ContainsKey(obj))
        {
            objectTriggerCount.Remove(obj);
        }
        collisionObjects.Remove(obj);

        _eventBus.Invoke(new RobotCollisionExitEvent(gameObject, obj));

        if (collisionObjects.Count == 0)
        {
            RealFinger.isKinematic = false;
        }
    }
}