using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Models
{
    public class DragDropData
    {
        public string SourceId { get; set; }
        public VisualElement SourceElement { get; set; }
        public SceneObject SceneObject { get; set; }
        public Vector2 StartPosition { get; set; }
        public object UserData { get; set;  }
    }

    public enum DropPosition
    {
        Above,
        Below,
        Inside
    }

    public class DropTargetInfo
    {
        public VisualElement TargetElement { get; set; }
        public DropPosition Position { get; set; }
        public float Distance { get; set; }
        public bool IsBeforeFirst { get; set; } = false;
        public bool IsAfterLast { get; set; } = false;
    }
}
