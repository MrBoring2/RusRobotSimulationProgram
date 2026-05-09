using UnityEngine;
using System.Collections;

namespace Assets.Scripts.Models
{
    public class CommandObject : SceneObject
    {
        public CommandObject(string id, ObjectType type, GameObject reference, string parentId = null) : base(id, type, reference, parentId)
        {
        }
    }
}