using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    internal class test : MonoBehaviour
    {
        //public NotificationSystemEvents notificationSystem;

        //private void Start()
        //{
        //    notificationSystem = GetComponent<NotificationSystemEvents>();
        //    if (notificationSystem == null)
        //        notificationSystem = gameObject.AddComponent<NotificationSystemEvents>();

        //    // Создаем тестовые кнопки в интерфейсе
        //    CreateTestButtons();
        //}

        //private void CreateTestButtons()
        //{
        //    var uiDocument = GetComponent<UIDocument>();
        //    if (uiDocument == null) return;

        //    var root = uiDocument.rootVisualElement;

        //    // Создаем панель с кнопками для тестирования
        //    var buttonPanel = new VisualElement();
        //    buttonPanel.style.position = Position.Absolute;
        //    buttonPanel.style.left = 20;
        //    buttonPanel.style.top = 20;
        //    buttonPanel.style.flexDirection = FlexDirection.Column;
        //    buttonPanel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        //    buttonPanel.style.paddingBottom = 10;
        //    buttonPanel.style.paddingLeft = 10;
        //    buttonPanel.style.paddingRight = 10;
        //    buttonPanel.style.paddingTop = 10;
        //    //buttonPanel.style.borderRadius = 8;

        //    root.Add(buttonPanel);

        //    // Кнопки для разных типов уведомлений
        //    buttonPanel.Add(CreateTestButton("Info", () =>
        //        notificationSystem.ShowInfo("Это информационное сообщение")));

        //    buttonPanel.Add(CreateTestButton("Success", () =>
        //        notificationSystem.ShowSuccess("Операция выполнена успешно!")));

        //    buttonPanel.Add(CreateTestButton("Warning", () =>
        //        notificationSystem.ShowWarning("Внимание! Проверьте данные")));

        //    buttonPanel.Add(CreateTestButton("Error", () =>
        //        notificationSystem.ShowError("Произошла ошибка")));

        //    // Кнопка для массового создания уведомлений
        //    buttonPanel.Add(CreateTestButton("Show 10 messages", () =>
        //    {
        //        for (int i = 0; i < 10; i++)
        //        {
        //            notificationSystem.ShowInfo($"Сообщение #{i + 1}", $"Заголовок {i + 1}");
        //        }
        //    }));
        //}

        //private Button CreateTestButton(string text, System.Action onClick)
        //{
        //    var button = new Button(onClick);
        //    button.text = text;
        //    button.style.width = 200;
        //    button.style.height = 40;
        //    button.style.marginBottom = 5;
        //    button.style.fontSize = 14;
        //    return button;
        //}
    }
}
