using Assets.Scripts.Managers;
using Assets.Scripts.SystemManager;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;

namespace Assets.Scripts.Models
{
    public class FieldBindingUtils
    {
        public static Action BindFieldWithHistory<T>(BaseField<T> field, object target, string propertyName, UndoRedoManager undoRedoManager, UIStatusManager uIStatusManager, Action applyImmediately = null)
        {
            if (target == null || field == null || string.IsNullOrEmpty(propertyName))
                return () => { };

            object oldValue = null;

            PropertyInfo propertyInfo = null;

            // Находим свойство один раз
            propertyInfo = target.GetType() .GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                Debug.LogError($"Свойства '{propertyName}' не найдено для {target.GetType().Name}");
                return () => { };
            }
            field.focusable = false;
            EventCallback<FocusEvent> focusHandler = _ =>
            {
                oldValue = propertyInfo.GetValue(target);
                uIStatusManager.SetInputMode(true);
            };

            EventCallback<ChangeEvent<T>> changeHandler = evt =>
            {
                applyImmediately?.Invoke();
            };

            EventCallback<MouseEnterEvent> mouseEnterHandler = evt =>
            {
                field.focusable = true;
            };
            EventCallback<MouseLeaveEvent> mouseLeaveHandler = evt =>
            {
                field.focusable = false;
            };
            EventCallback<DetachFromPanelEvent> onDestroy = evt =>
            {
                field.focusable = false;
                uIStatusManager.SetInputMode(false);
                uIStatusManager.SetPointerOberUI(false);
            };


            EventCallback<BlurEvent> blurHandler = _ =>
            {
                var currentValue = propertyInfo.GetValue(target);
                if (!Equals(oldValue, currentValue))
                {
                    var command = new PropertyChangeCommand(target, propertyName, oldValue, currentValue);
                    undoRedoManager.Execute(command);
                }
                field.focusable = false;
                uIStatusManager.SetInputMode(false);
            };
            // Регистрируем обработчики
            field.RegisterCallback(focusHandler);
            field.RegisterValueChangedCallback(changeHandler);
            field.RegisterCallback(blurHandler);
            field.RegisterCallback(mouseEnterHandler);
            field.RegisterCallback(mouseLeaveHandler);
            field.RegisterCallback(onDestroy);

            // Возвращаем функцию для отписки
            return () =>
            {
                field.UnregisterCallback(focusHandler);
                field.UnregisterValueChangedCallback(changeHandler);
                field.UnregisterCallback(blurHandler);
                field.UnregisterCallback(mouseEnterHandler);
                field.UnregisterCallback(mouseLeaveHandler);
            };
        }

        //public static Action BindFieldWithHistory(TextField field, object target, string propertyName, Action applyImmediately = null)
        //{
        //    return BindFieldWithHistory<string>(field, target, propertyName, applyImmediately);
        //}

        //public static Action BindFieldWithHistory(IntegerField field, object target, string propertyName, Action applyImmediately = null)
        //{
        //    return BindFieldWithHistory<int>(field, target, propertyName, applyImmediately);
        //}

    }
}