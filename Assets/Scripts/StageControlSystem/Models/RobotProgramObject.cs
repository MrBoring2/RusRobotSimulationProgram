using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class RobotProgramObject : SceneObject
    {
        public RobotProgramObject(string id, ObjectType type, GameObject reference, string parentId = null) : base(id, type, reference, parentId)
        {
            Items = new List<CommandObject>();
        }

        public List<CommandObject> Items { get; private set; }
    }
}