using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.StageControlSystem.UI
{
    public class RobotCollisionInfoPanelEvents : BaseModalWindow
    {
        private ScrollView collisionScrollView;
        private Button closeBtn;
        private Label noCollisionsLabel;
        private IVisualElementScheduledItem _updateJob;
        private bool _isDirty = false;
        private Dictionary<string, List<string>> _collisionData;

        protected override void OnBeforeShow(ModalParameters parameters)
        {
            if (parameters != null)
            {
                _collisionData = parameters.Get<Dictionary<string, List<string>>>("collisionData");
                PopulateCollisionData();
            }

            _updateJob = root?.schedule.Execute(() =>
            {
                if (_isDirty)
                {
                    PopulateCollisionData();
                    _isDirty = false;
                }
            }).Every(200);
        }
        protected override void InitializeElements(VisualElement root)
        {
            collisionScrollView = root.Q<ScrollView>("collision-scroll-view");
            noCollisionsLabel = root.Q<Label>("no-collisions-label");
            closeBtn = root.Q<Button>("close-btn");
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            closeBtn.clicked += () => CloseWithValue(null);

            // Заполняем данные после того как окно показано
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            PopulateCollisionData();
        }
        public void UpdateCollisionData(Dictionary<string, List<string>> newData)
        {
            _collisionData = newData;
            _isDirty = true;
        }
        private void PopulateCollisionData()
        {
            if (_collisionData == null || !_collisionData.Any(kvp => kvp.Value.Count > 0))
            {
                noCollisionsLabel.style.display = DisplayStyle.Flex;
                collisionScrollView.style.display = DisplayStyle.None;
                return;
            }

            noCollisionsLabel.style.display = DisplayStyle.None;
            collisionScrollView.style.display = DisplayStyle.Flex;

            collisionScrollView.Clear();

            foreach (var kvp in _collisionData)
            {
                string partName = kvp.Key;
                List<string> collidedObjects = kvp.Value;

                if (collidedObjects == null || collidedObjects.Count == 0)
                    continue;

                // Заголовок — часть робота
                var partHeader = new Label($"Часть: {partName}");
                partHeader.style.color = new Color(1f, 0.85f, 0.3f);
                partHeader.style.fontSize = 14;
                partHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                partHeader.style.marginTop = 8;
                partHeader.style.marginBottom = 4;
                partHeader.AddToClassList("part-header");
                collisionScrollView.Add(partHeader);

                // Список объектов коллизии
                foreach (var objName in collidedObjects)
                {
                    var objLabel = new Label($"  ▸ {objName}");
                    objLabel.style.color = Color.white;
                    objLabel.style.fontSize = 13;
                    objLabel.style.marginBottom = 2;
                    objLabel.AddToClassList("collision-item");
                    collisionScrollView.Add(objLabel);
                }

                // Разделитель
                var separator = new VisualElement();
                separator.style.height = 1;
                separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                separator.style.marginTop = 4;
                separator.style.marginBottom = 4;
                separator.AddToClassList("separator");
                collisionScrollView.Add(separator);
              
          
             
            }
        }
    }
}