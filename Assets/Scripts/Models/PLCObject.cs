using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class PLCObject : SceneObject
    {
        public PLCObject(string id, ObjectType type, GameObject reference, string parentId = null) : base(id, type, reference, parentId)
        {
        }
    }

    public class PLCCommandObject : SceneObject
    {
        public PLCCommandObject(string id, ObjectType type, GameObject reference, string parentId = null) : base(id, type, reference, parentId)
        {
        }
    }

    public class PLCExpression : PLCCommandObject
    {
        public ExpressionType ExpressionType { get; private set; }
        public PLCExpression(string id, ObjectType type, GameObject reference, ExpressionType expressionType, string parentId = null) : base(id, type, reference, parentId)
        {
            ExpressionType = expressionType;
        }
        public List<PLCCommandObject> pLCCommandObjects = new List<PLCCommandObject>();
    }





    public enum ExpressionType
    {
        IF,
        ELIF,
        ELSE
    }
}
