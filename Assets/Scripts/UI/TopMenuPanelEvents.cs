using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class TopMenuPanelEvents : MonoBehaviour
    {
        private VisualElement fileMenu;
        private VisualElement viewMenu;
        private VisualElement root;
        public void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            var overlay = root.Q<VisualElement>("overlay");
            fileMenu = overlay.Q<VisualElement>("FileMenu");
            viewMenu = overlay.Q<VisualElement>("ViewMenu");

            // Кнопки
            var fileBtn = root.Q<Button>("FileButton");
            var viewBtn = root.Q<Button>("ViewButton");

            fileBtn.clicked += () => ToggleMenu(fileMenu, fileBtn);
            viewBtn.clicked += () => ToggleMenu(viewMenu, viewBtn);

            // Закрытие при клике вне меню
            root.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target != fileBtn && evt.target != viewBtn)
                {
                    HideAll();
                }
            });

            // Обработка кликов внутри меню, чтобы не закрывалось
            fileMenu.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            viewMenu.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
        }

        private void ToggleMenu(VisualElement menu, VisualElement button)
        {
            HideAll();

            // Позиционирование под кнопкой
            var rect = button.worldBound;
            menu.style.left = rect.x;
            menu.style.top = rect.yMax;

            menu.RemoveFromClassList("hidden");
        }

        private void HideAll()
        {
            fileMenu.AddToClassList("hidden");
            viewMenu.AddToClassList("hidden");
        }
    }
}
