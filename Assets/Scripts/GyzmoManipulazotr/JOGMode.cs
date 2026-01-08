using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GyzmoManipulazotr
{
    
    public class JOGMode
    {
        private Quaternion startRot;
        private Vector3 mouseStart;
        private Vector3 ringTangentScreen;
        private float sensitivity = 1f;
        public void OnHandleDown(AxisHandleJOG handle)
        {
            if(handle.posType == JOGHandleType.Rotate)
            {
                var t = handle.manipulator.Target;
                var manip = handle?.manipulator;
                if (t == null || manip == null) return;
                startRot = t.rotation; mouseStart = Input.mousePosition;
                Vector3 axis = handle.direction.normalized;
                if (manip.CurrentAxisMode == AxisMode.Global)
                    manip.gizmoRootStartRotation = manip.gizmoRoot.rotation;
                Vector3 camDir = (Camera.main.transform.position - t.position).normalized;
                Vector3 ringTangent = Vector3.Cross(camDir, axis).normalized * -1f;
                Vector3 s0 = Camera.main.WorldToScreenPoint(t.position);
                Vector3 s1 = Camera.main.WorldToScreenPoint(t.position + ringTangent);
                ringTangentScreen = (s1 - s0).sqrMagnitude < 0.001f ? Vector3.right : (s1 - s0).normalized;
            }
        }

        public void OnHandleDrag(AxisHandleJOG handle, Vector3 d, Vector3 startPos)
        {
            if(handle.posType == JOGHandleType.Move)
            {
                var t = handle.manipulator.Target;
                var manip = handle.manipulator;
                if (t == null || manip == null) return;

                if (handle.directionType == HandleType.Axis)
                {
                    Vector3 moveDir;

                    if (manip.CurrentAxisMode == AxisMode.Local)
                    {
                        // В локальном режиме: берем мировое направление оси
                        moveDir = t.TransformDirection(handle.direction.normalized);
                    }
                    else
                    {
                        // В глобальном режиме: мировое направление
                        moveDir = handle.direction.normalized;
                    }

                    // Считаем проекцию движения мыши на выбранную ось
                    float moveAmount = Vector3.Dot(d, moveDir);
                    t.position = startPos + moveDir * moveAmount;
                }
                else
                {
                    Vector3 planeNormal;

                    if (manip.CurrentAxisMode == AxisMode.Local)
                    {
                        // В локальном режиме: берем мировую нормаль плоскости
                        planeNormal = t.TransformDirection(handle.planeNormal);
                    }
                    else
                    {
                        // В глобальном режиме: мировую нормаль
                        planeNormal = handle.planeNormal;
                    }

                    // Для плоскости всегда проецируем движение на плоскость
                    t.position = startPos + Vector3.ProjectOnPlane(d, planeNormal);
                }
            }
            else
            {
                var t = handle.manipulator.Target;
                var manip = handle.manipulator;
                if (t == null) return;

                Vector3 axis = handle.direction.normalized;

                if (manip.CurrentAxisMode == AxisMode.Local)
                {
                    Vector3 localAxisWorld = t.TransformDirection(axis);

                    Vector3 mouseDelta = Input.mousePosition - mouseStart;
                    mouseStart = Input.mousePosition; // обновляем для следующего кадра

                    // Берём только горизонтальную или вертикальную составляющую мыши
                    float projected = mouseDelta.x; // например, движение мыши по X
                    float angle = Mathf.Clamp(projected * sensitivity, -5f, 5f);

                    Quaternion delta = Quaternion.AngleAxis(angle, localAxisWorld);
                    t.rotation = delta * t.rotation;
                    manip.gizmoRoot.rotation = t.rotation;
                }
                else
                {
                    // глобальный режим оставляем прежним
                    Vector3 mouseDelta = Input.mousePosition - mouseStart;
                    float ang = Vector3.Dot(mouseDelta, ringTangentScreen) * sensitivity;
                    Quaternion delta = Quaternion.AngleAxis(ang, axis);
                    t.rotation = delta * startRot;
                    manip.gizmoRoot.rotation = delta * manip.gizmoRootStartRotation;
                }
            }
        }

        public void OnHandleUp(AxisHandleJOG handle)
        {
            if(handle.posType == JOGHandleType.Rotate)
            {
                var manip = handle?.manipulator;
                if (manip == null) return;

                if (manip.CurrentAxisMode == AxisMode.Global)
                    manip.gizmoRoot.rotation = manip.gizmoRootStartRotation; // возвращаем визуально к глобальной ориентации
            }
        }

        public void OnObjectSelected(Transform target, GyzmoManupulator manupulator)
        {
            throw new NotImplementedException();
        }
    }
}
