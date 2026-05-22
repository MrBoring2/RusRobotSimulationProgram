using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GyzmoManipulazotr
{
    public enum JOGHandleType
    {
        Move,
        Rotate
    }
    public class AxisHandleJOG : MonoBehaviour
    {
        public HandleType directionType;
        public JOGHandleType posType;
        public Vector3 direction;     // для Axis
        public Vector3 planeNormal;   // для Plane
        public JOGManipulator manipulator;

        private bool dragging;
        private Vector3 dragStartPos;
        private Vector3 dragStartMouseWorld;
        private Plane dragPlane;
        [SerializeField] private float highlightMultiplier = 2f;
        [SerializeField] private float normalAlpha = 0.5f;
        [SerializeField] private float highlightAlpha = 1f;
        private UIStatusManager _uIStatusManager;
        private Material material;
        private Color originalColor;
        private Color highlightedColor;
        private void Start()
        {
            InitializeHighlight();
            _uIStatusManager = ServiceManager.Current.Get<UIStatusManager>();
        }
        private void InitializeHighlight()
        {
            var renderer = GetComponent<Renderer>();
            material = renderer.material;
            originalColor = material.GetColor("_Color");
            highlightedColor = new Color(
                Mathf.Clamp01(originalColor.r * highlightMultiplier),
                Mathf.Clamp01(originalColor.g * highlightMultiplier),
                Mathf.Clamp01(originalColor.b * highlightMultiplier),
                originalColor.a
            );
            SetNormal();
        }
        void Update()
        {
            if (dragging)
            {
                SetHighlighted();
                return;
            }
            if (_uIStatusManager.AnyHandleDragging)
            {
                SetNormal();
                return;
            }
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Manipulator")))
            {
                if (hit.collider.gameObject == gameObject)
                    SetHighlighted();
                else
                    SetNormal();
            }
            else
            {
                SetNormal();
            }
        }
        void SetHighlighted()
        {
            material.SetColor("_Color", highlightedColor);
            material.SetFloat("_AlphaMultiplier", 2.0f);
            material.renderQueue = 4000;
            material.SetInt("_ZWrite", 1);

        }

        void SetNormal()
        {
            material.SetColor("_Color", originalColor);
            material.SetFloat("_AlphaMultiplier", 0.5f);
            material.renderQueue = 3000;
            material.SetInt("_ZWrite", 0);
        }
        public void StartDrag()
        {
            dragging = true;
            dragStartPos = manipulator.Target.position;
            manipulator.NotifyStartDrag();
            // Определяем правильную плоскость для движения
            dragPlane = GetOptimalDragPlane();

            // Получаем начальную позицию мыши на плоскости
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                dragStartMouseWorld = ray.GetPoint(enter);
            }

            manipulator.CurrentManipulatorMode.OnHandleDown(this);
            _uIStatusManager.SetHandleDragging(true);
        }

        public void UpdateDrag()
        {
            if (!dragging || manipulator.Target == null || manipulator.CurrentManipulatorMode == null) return;

            // Получаем текущую позицию мыши на той же плоскости
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentMouseWorld = ray.GetPoint(enter);
                Vector3 delta = currentMouseWorld - dragStartMouseWorld;

                // Передаем delta в мировых координатах
                manipulator.CurrentManipulatorMode.OnHandleDrag(this, delta, dragStartPos);
                manipulator.NotifyTransformChanged();
            }
        }

        public void EndDrag()
        {
            if (dragging && manipulator.CurrentManipulatorMode != null)
            {
                manipulator.CurrentManipulatorMode.OnHandleUp(this);
            }
            dragging = false;
            manipulator.NotifyDragEnd();
            _uIStatusManager.SetHandleDragging(false);
        }

        private Plane GetOptimalDragPlane()
        {
            Camera cam = Camera.main;
            Vector3 targetPos = manipulator.Target.position;

            if (directionType == HandleType.Axis)
            {
                Vector3 axisDir;

                if (manipulator.CurrentAxisMode == AxisMode.Local)
                {
                    // В локальном режиме берем направление в мировых координатах
                    axisDir = manipulator.Target.TransformDirection(direction.normalized);
                }
                else
                {
                    // В глобальном режиме берем мировое направление
                    axisDir = direction.normalized;
                }

                // Создаем плоскость, которая всегда хорошо работает с лучом камеры
                Vector3 camForward = cam.transform.forward;
                float dot = Vector3.Dot(camForward, axisDir);

                // Если камера смотрит почти вдоль оси, используем другую плоскость
                if (Mathf.Abs(dot) > 0.9f)
                {
                    // Плоскость через объект, перпендикулярно направлению от камеры к объекту
                    Vector3 camToTarget = (targetPos - cam.transform.position).normalized;
                    return new Plane(camToTarget, targetPos);
                }
                else
                {
                    // Обычная плоскость: перпендикулярно камере
                    return new Plane(camForward, targetPos);
                }
            }
            else // Plane
            {
                Vector3 normal;

                if (manipulator.CurrentAxisMode == AxisMode.Local)
                {
                    // В локальном режиме берем нормаль в мировых координатах
                    normal = manipulator.Target.TransformDirection(planeNormal);
                }
                else
                {
                    // В глобальном режиме берем мировую нормаль
                    normal = planeNormal;
                }

                return new Plane(normal, targetPos);
            }
        }
    }
}
