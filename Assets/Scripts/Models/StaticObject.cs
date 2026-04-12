using UnityEngine;
using System.Collections;

namespace Assets.Scripts.Models
{
    public class StaticObject : SceneObject
    {
        public StaticObject(string id, ObjectType type, GameObject reference, string parentId = null) : base(id, type, reference, parentId)
        {
        }
    }
}