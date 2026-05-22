using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace Assets.Scripts.GyzmoManipulazotr
{

    public class JOGMode
    {
        private Quaternion startRotation;      // начальный поворот объекта
        private Quaternion gizmoStartRotation; // начальный поворот гизмо
        private Vector3 mouseStart;            // стартовая позиция мыши
        private Vector3 screenTangent;         // касательная для проекции
        private Vector3 axisWorld;             // ось вращения
        private Quaternion startRot;
        private Vector3 ringTangentScreen;
        private Vector3 axisLocal;
        private float sensitivity = 0.3f;
        public TextMeshPro cursorAngleText;           // или TextMeshProUGUI
        private Transform targetTransform;
        public void OnHandleDown(AxisHandleJOG handle)
        {
            if (handle.posType != JOGHandleType.Rotate) return;
            if (handle?.manipulator?.Target == null) return;

            Transform t = handle.manipulator.Target;
            axisLocal = handle.direction.normalized;
            axisWorld = t.TransformDirection(handle.direction).normalized;

            startRotation = t.rotation;
            gizmoStartRotation = handle.manipulator.gizmoRoot.rotation;

            mouseStart = Input.mousePosition;

            Vector3 camDir = (Camera.main.transform.position - t.position).normalized;
            Vector3 tangentWorld = Vector3.Cross(axisWorld, camDir).normalized;

            if (tangentWorld.sqrMagnitude < 1e-6f)
                tangentWorld = Vector3.Cross(axisWorld, Vector3.up).normalized;

            Vector3 p0 = Camera.main.WorldToScreenPoint(t.position);
            Vector3 p1 = Camera.main.WorldToScreenPoint(t.position + tangentWorld);

            screenTangent = (p1 - p0);
            screenTangent.z = 0;
            screenTangent.Normalize();
            cursorAngleText.gameObject.SetActive(true);
        }

        public void OnHandleDrag(AxisHandleJOG handle, Vector3 d, Vector3 startPos)
        {
            if (handle.posType == JOGHandleType.Move)
            {
                var t = handle.manipulator.Target;
                var manip = handle.manipulator;
                if (t == null || manip == null) return;

                if (handle.directionType == HandleType.Axis)
                {
                    Vector3 moveDir;

                    // В локальном режиме: берем мировое направление оси
                    moveDir = t.TransformDirection(handle.direction.normalized);

                    // Считаем проекцию движения мыши на выбранную ось
                    float moveAmount = Vector3.Dot(d, moveDir);
                    t.position = startPos + moveDir * moveAmount;
                }
                else
                {
                    Vector3 planeNormal;

                    // В локальном режиме: берем мировую нормаль плоскости
                    planeNormal = t.TransformDirection(handle.planeNormal);

                    // Для плоскости всегда проецируем движение на плоскость
                    t.position = startPos + Vector3.ProjectOnPlane(d, planeNormal);
                }
            }
            else
            {
                if (handle?.manipulator?.Target == null) return;

                Transform t = handle.manipulator.Target;
                Vector3 mouseDelta = Input.mousePosition - mouseStart;
                float projected = Vector3.Dot(mouseDelta, screenTangent);
                float angle = projected * sensitivity;

                // Вращаем объект
                t.rotation = Quaternion.AngleAxis(angle, axisWorld) * startRotation;

                // Вращаем гизмо
                handle.manipulator.gizmoRoot.rotation =
                    Quaternion.AngleAxis(angle, axisWorld) * gizmoStartRotation;

                //string axisLetter = GetAxisLetter(axisWorld);
                string axisLetter = GetAxisLetter(axisLocal); ;
                cursorAngleText.text = $"{axisLetter}: {angle:F1}°";
                SetAxisColor(axisLocal);

                // Обновляем позицию и цвет
                //UpdateTextPositionAndRotation(axisWorld);
            }
        }

        public void OnHandleUp(AxisHandleJOG handle)
        {
            if (handle.posType == JOGHandleType.Rotate)
            {
                if (handle?.manipulator == null) return;

                //handle.manipulator.gizmoRoot.rotation = gizmoStartRotation;

                // Скрываем текст
                cursorAngleText.gameObject.SetActive(false);
            }
        }
        private string GetAxisLetter(Vector3 localAxis)
        {
            if (Vector3.Dot(localAxis, Vector3.right) > 0.99f) return "X";
            if (Vector3.Dot(localAxis, Vector3.up) > 0.99f) return "Y";
            if (Vector3.Dot(localAxis, Vector3.forward) > 0.99f) return "Z";
            return "?";
        }
        private void SetAxisColor(Vector3 localAxis)
        {
            if (Vector3.Dot(localAxis, Vector3.forward) > 0.99f)
                cursorAngleText.color = Color.red;
            else if (Vector3.Dot(localAxis, Vector3.right) > 0.99f)
                cursorAngleText.color = Color.green;
            else if (Vector3.Dot(localAxis, Vector3.up) > 0.99f)
                cursorAngleText.color = Color.blue;
            else
                cursorAngleText.color = Color.white;
        }
        public void OnObjectSelected(Transform target, GizmoManupulator manupulator)
        {
            targetTransform = target;
            if (manupulator != null)
                gizmoStartRotation = manupulator.gizmoRoot.rotation;
        }
    }
}
