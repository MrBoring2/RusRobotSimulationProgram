using Assets.Scripts.SystemManager;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts.Models
{
    public class FieldBindingUtils
    {
        public static Action BindFieldWithHistory<T>(BaseField<T> field, object target, string propertyName, Action applyImmediately = null)
        {
            if (target == null || field == null || string.IsNullOrEmpty(propertyName))
                return () => { };

            object oldValue = null;

            PropertyInfo propertyInfo = null;

            // Находим свойство один раз
            propertyInfo = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                Debug.LogError($"Свойства '{propertyName}' не найдено для {target.GetType().Name}");
                return () => { };
            }

            // СОХРАНЯЕМ ССЫЛКИ на обработчики
            EventCallback<FocusEvent> focusHandler = _ =>
            {
                oldValue = propertyInfo.GetValue(target);
            };

            EventCallback<ChangeEvent<T>> changeHandler = evt =>
            {
                applyImmediately?.Invoke();
            };

            EventCallback<BlurEvent> blurHandler = _ =>
            {
                var currentValue = propertyInfo.GetValue(target);
                if (!Equals(oldValue, currentValue))
                {
                    var command = new PropertyChangeCommand(target, propertyName, oldValue, currentValue);
                    UndoRedoSystem.Instance.Execute(command);
                }
            };

            // Регистрируем обработчики
            field.RegisterCallback(focusHandler);
            field.RegisterValueChangedCallback(changeHandler);
            field.RegisterCallback(blurHandler);

            // Возвращаем функцию для отписки
            return () =>
            {
                field.UnregisterCallback(focusHandler);
                field.UnregisterValueChangedCallback(changeHandler);
                field.UnregisterCallback(blurHandler);
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