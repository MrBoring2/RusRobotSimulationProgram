using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Assets.Scripts.Utils
{
    public static class UIElementsExtensions
    {
        public static VisualElement GetDocumentRoot(this VisualElement ele)
        {
            return (ele.parent == null ? ele : ele.parent.GetDocumentRoot());
        }

        // ============================================================================================================
    }
}
