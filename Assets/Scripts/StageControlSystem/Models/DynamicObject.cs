using UnityEngine;
using System.Collections;

namespace Assets.Scripts.Models
{
    public class DynamicObject : SceneObject
    {
        public DynamicObject(string id, ObjectType type, GameObject reference, string parentId = null) : base(id, type, reference, parentId)
        {
        }
    }
}