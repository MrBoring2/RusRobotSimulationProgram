using UnityEngine;
using System.Collections;

namespace Assets.Scripts.Models
{
    public class RobotObject : SceneObject
    {
        public RobotObject(string id, ObjectType type, GameObject reference, string parentId = null) : base(id, type, reference, parentId)
        {
        }
    }
}