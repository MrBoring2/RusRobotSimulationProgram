using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts.UI
{
    public class DragManipulator : Clickable
    {
        public bool FreeMoving { get; set; }
        public Vector2 StartMousePosition { get; set; }
        public Vector2 Delta => (lastMousePosition - StartMousePosition);

        private event System.Action onDragging;

        public DragManipulator(System.Action clickHandler, System.Action dragHandler)
            : base(clickHandler, 250, 30)
        {
            FreeMoving = false;
            onDragging += dragHandler;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            base.RegisterCallbacksOnTarget();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            base.UnregisterCallbacksFromTarget();
        }

        protected override void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            FreeMoving = false;
            StartMousePosition = localPosition;
            base.ProcessDownEvent(evt, localPosition, pointerId);
        }

        protected override void ProcessMoveEvent(EventBase evt, Vector2 localPosition)
        {
            FreeMoving = true;

            base.ProcessMoveEvent(evt, localPosition);

            if (evt.eventTypeId == PointerMoveEvent.TypeId())
            {
                evt.PreventDefault();
            }

            onDragging?.Invoke();
        }

        protected void OnPointerDown(PointerDownEvent evt)
        {
            if (!CanStartManipulation(evt)) return;

            ProcessDownEvent(evt, evt.localPosition, evt.pointerId);
            evt.PreventDefault();
        }

        protected void OnPointerMove(PointerMoveEvent evt)
        {
            if (active)
            {
                ProcessMoveEvent(evt, evt.localPosition);
            }
        }

        protected void OnPointerUp(PointerUpEvent evt)
        {
            if (active && CanStopManipulation(evt))
            {
                ProcessUpEvent(evt, evt.localPosition, evt.pointerId);
            }
        }

        protected void OnPointerCancel(PointerCancelEvent evt)
        {
            if (IsNotMouseEvent(evt) && CanStopManipulation(evt))
            {
                ProcessCancelEvent(evt, evt.pointerId);
            }
        }

        private static bool IsNotMouseEvent<T>(T evt) where T : PointerEventBase<T>, new()
        {
            // We need to ignore temporarily mouse callback on mobile because they are sent with the wrong type.
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            return true;
#else
            return evt.pointerId != PointerId.mousePointerId;
#endif
        }

        // ============================================================================================================
    }
}
