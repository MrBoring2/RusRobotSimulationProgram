using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Assets.Scripts.Models
{
    public class TooltipCallbacks
    {
        public EventCallback<MouseEnterEvent> Enter;
        public EventCallback<MouseMoveEvent> Move;
        public EventCallback<MouseLeaveEvent> Leave;
        public EventCallback<ClickEvent> Click;
    }
}
