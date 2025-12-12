using System;
using UnityEngine;

public class UniqueId : MonoBehaviour
{
    public Guid uniqueID;

    private void Awake()
    {
        uniqueID = Guid.NewGuid();
    }
}
