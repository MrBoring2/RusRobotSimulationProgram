using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class SceneObject
    {
        public string Id { get; private set; }
        public ObjectType Type { get; private set; }
        public GameObject Reference { get; private set; }
        public string ParentId { get; private set; }
        public IPropertyProvider PropertyProvider => Reference?.GetComponent<IPropertyProvider>();
        public SceneObject(string id, ObjectType type, GameObject reference, string parentId = null)
        {
            Id = id;
            Type = type;
            Reference = reference;
            ParentId = parentId;
        }
        public void SetParent(string parentId)
        {
            ParentId = parentId;
        }
    }
}
