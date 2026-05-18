using Assets.Scripts.Managers;
using Assets.Scripts.StageControlSystem.Models;
using Assets.Scripts.SystemManager;
using Assets.UI.CustomElements;
using Assets.UI.CustomElements.ColorField;
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
        private static List<Action> _pendingFlushActions = new List<Action>();
        public static Action BindFieldWithHistory<T>(BaseField<T> field, object target, string propertyName, UndoRedoManager undoRedoManager, UIStatusManager uIStatusManager, Action applyImmediately = null)
        {
            if (target == null || field == null || string.IsNullOrEmpty(propertyName))
                return () => { };

            object oldValue = null;
            bool isFocused = false;
            PropertyInfo propertyInfo = null;

            // Находим свойство один раз
            propertyInfo = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null)
            {
                Debug.LogError($"Свойства '{propertyName}' не найдено для {target.GetType().Name}");
                return () => { };
            }
            field.focusable = false;
            EventCallback<FocusEvent> focusHandler = _ =>
            {
                isFocused = true;
                oldValue = propertyInfo.GetValue(target);
                uIStatusManager.SetInputMode(true);
            };

            //EventCallback<ChangeEvent<T>> changeHandler = evt =>
            //{
            //    applyImmediately?.Invoke();
            //    if (isFocused)
            //    {
            //        var currentValue = propertyInfo.GetValue(target);
            //        if (!Equals(oldValue, currentValue))
            //        {
            //            var command = new PropertyChangeCommand(target, propertyName, oldValue, currentValue);
            //            undoRedoManager.Execute(command);
            //            oldValue = currentValue; // Обновляем oldValue для следующих изменений
            //        }
            //    }
            //};

            EventCallback<MouseEnterEvent> mouseEnterHandler = evt =>
            {
                field.focusable = true;
            };
            EventCallback<MouseLeaveEvent> mouseLeaveHandler = evt =>
            {
                field.focusable = false;
            };
            //EventCallback<DetachFromPanelEvent> onDestroy = evt =>
            //{
            //    field.focusable = false;
            //    uIStatusManager.SetInputMode(false);
            //    //uIStatusManager.SetPointerOberUI(false);
            //};


            EventCallback<BlurEvent> blurHandler = _ =>
            {
                applyImmediately?.Invoke();
                if (isFocused)
                {
                    var currentValue = propertyInfo.GetValue(target);
                    if (!Equals(oldValue, currentValue))
                    {
                        var command = new PropertyChangeCommand(target, propertyName, oldValue, currentValue);
                        undoRedoManager.Execute(command);
                        oldValue = currentValue; // Обновляем oldValue для следующих изменений
                    }
                }
                field.focusable = false;
                uIStatusManager.SetInputMode(false);
            };
            // Регистрируем обработчики
            field.RegisterCallback(focusHandler);
            //field.RegisterValueChangedCallback(changeHandler);
            field.RegisterCallback(blurHandler);
            field.RegisterCallback(mouseEnterHandler);
            field.RegisterCallback(mouseLeaveHandler);
            //field.RegisterCallback(onDestroy);
            Action flushAction = () =>
            {
                if (isFocused && field != null && propertyInfo != null && target != null)
                {
                    applyImmediately?.Invoke();
                    var currentValue = propertyInfo.GetValue(target);
                    if (!Equals(oldValue, currentValue))
                    {
                        var command = new PropertyChangeCommand(target, propertyName, oldValue, currentValue);
                        undoRedoManager.Execute(command);
                        oldValue = currentValue;
                    }
                    isFocused = false;
                    uIStatusManager.SetInputMode(false);
                }
            };
            _pendingFlushActions.Add(flushAction);
            // Возвращаем функцию для отписки
            return () =>
            {
                _pendingFlushActions.Remove(flushAction);
                field.UnregisterCallback(focusHandler);
                //field.UnregisterValueChangedCallback(changeHandler);
                field.UnregisterCallback(blurHandler);
                field.UnregisterCallback(mouseEnterHandler);
                field.UnregisterCallback(mouseLeaveHandler);
            };
        }
        public static Action BindCustomFieldWithHistory<T>(
        BaseField<T> field,
        object target,
        string propertyName,
        Func<object> getter,
        Action<object> setter,
        UndoRedoManager undoRedoManager,
        UIStatusManager uIStatusManager)
        {
            if (target == null || field == null || getter == null || setter == null)
                return () => { };

            object oldValue = null;
            bool isFocused = false;

            field.focusable = false;

            EventCallback<FocusEvent> focusHandler = _ =>
            {
                isFocused = true;
                oldValue = getter();
                uIStatusManager.SetInputMode(true);
            };

            EventCallback<MouseEnterEvent> mouseEnterHandler = _ => field.focusable = true;
            EventCallback<MouseLeaveEvent> mouseLeaveHandler = _ => field.focusable = false;

            EventCallback<BlurEvent> blurHandler = _ =>
            {
                if (!isFocused)
                {
                    field.focusable = false;
                    uIStatusManager.SetInputMode(false);
                    return;
                }

                // БЕРЕМ ЗНАЧЕНИЕ НАПРЯМУЮ ИЗ ПОЛЯ
                var fieldValue = field.value;
                setter(fieldValue);
                var currentValue = getter();

                if (!Equals(oldValue, currentValue))
                {
                    var command = new CustomPropertyChangeCommand(
                        target, propertyName, oldValue, currentValue, setter, getter);
                    undoRedoManager.Execute(command);
                }

                isFocused = false;
                field.focusable = false;
                uIStatusManager.SetInputMode(false);
            };

            Action flushAction = () =>
            {
                if (isFocused && field != null && target != null)
                {
                    var fieldValue = field.value;
                    setter(fieldValue);
                    var currentValue = getter();

                    if (!Equals(oldValue, currentValue))
                    {
                        var command = new CustomPropertyChangeCommand(
                            target, propertyName, oldValue, currentValue, setter, getter);
                        undoRedoManager.Execute(command);
                    }

                    isFocused = false;
                    uIStatusManager.SetInputMode(false);
                }
            };

            _pendingFlushActions.Add(flushAction);
            field.RegisterCallback(focusHandler);
            field.RegisterCallback(blurHandler);
            field.RegisterCallback(mouseEnterHandler);
            field.RegisterCallback(mouseLeaveHandler);

            return () =>
            {
                _pendingFlushActions.Remove(flushAction);
                field.UnregisterCallback(focusHandler);
                field.UnregisterCallback(blurHandler);
                field.UnregisterCallback(mouseEnterHandler);
                field.UnregisterCallback(mouseLeaveHandler);
            };
        }

        private static object GetFieldValue<T>(BaseField<T> field)
        {
            return field.value;
        }
        public static void FlushAllPendingChanges()
        {
            // Создаём копию, т.к. flushAction может модифицировать список
            var actions = new List<Action>(_pendingFlushActions);
            foreach (var action in actions)
            {
                action?.Invoke();
            }
        }
        //public static Action BindFieldWithHistory(TextField field, object target, string propertyName, Action applyImmediately = null)
        //{
        //    return BindFieldWithHistory<string>(field, target, propertyName, applyImmediately);
        //}

        //public static Action BindFieldWithHistory(IntegerField field, object target, string propertyName, Action applyImmediately = null)
        //{
        //    return BindFieldWithHistory<int>(field, target, propertyName, applyImmediately);
        //}
        public static Action BindDropdownWithHistory(
              CustomDropdown dropdown,
              object target,
              string propertyName,
              UndoRedoManager undoRedoManager,
              UIStatusManager uIStatusManager,
              Action applyImmediately)
        {
            if (target == null || dropdown == null)
                return () => { };

            object oldValue = null;
            bool isFocused = false;

            dropdown.RegisterCallback<FocusEvent>(_ =>
            {
                isFocused = true;
                oldValue = dropdown.value;
                uIStatusManager.SetInputMode(true);
            });

            dropdown.RegisterCallback<BlurEvent>(_ =>
            {
                applyImmediately?.Invoke();
                if (isFocused)
                {
                    var currentValue = dropdown.value;
                    if (!Equals(oldValue, currentValue))
                    {
                        var command = new CustomPropertyChangeCommand(
                            target, propertyName, oldValue, currentValue,
                            val => dropdown.value = val,
                            () => dropdown.value);
                        undoRedoManager.Execute(command);
                    }
                }
                isFocused = false;
                uIStatusManager.SetInputMode(false);
            });

            return () => { };
        }
        public static Action BindColorFieldWithHistory(
    ColorFieldElement colorField,
    object target,
    string propertyName,
    Func<object> getter,
    Action<object> setter,
    UndoRedoManager undoRedoManager,
    UIStatusManager uIStatusManager)
        {
            if (target == null || colorField == null || getter == null || setter == null)
                return () => { };

            object oldValue = null;
            bool isFocused = false;

            // При получении фокуса — запоминаем старое значение
            EventCallback<FocusEvent> focusHandler = _ =>
            {
                isFocused = true;
                oldValue = getter();
                uIStatusManager.SetInputMode(true);
            };

            // При потере фокуса — если изменилось, создаём команду
            EventCallback<BlurEvent> blurHandler = _ =>
            {
                if (!isFocused) return;

                var currentValue = getter();
                if (!Equals(oldValue, currentValue))
                {
                    var command = new CustomPropertyChangeCommand(
                        target, propertyName, oldValue, currentValue, setter, getter);
                    undoRedoManager.Execute(command);
                }

                isFocused = false;
                uIStatusManager.SetInputMode(false);
            };

            // Flush action для принудительного сохранения
            Action flushAction = () =>
            {
                if (isFocused && colorField != null && target != null)
                {
                    var currentValue = getter();
                    if (!Equals(oldValue, currentValue))
                    {
                        var command = new CustomPropertyChangeCommand(
                            target, propertyName, oldValue, currentValue, setter, getter);
                        undoRedoManager.Execute(command);
                    }

                    isFocused = false;
                    uIStatusManager.SetInputMode(false);
                }
            };

            _pendingFlushActions.Add(flushAction);
            colorField.RegisterCallback(focusHandler);
            colorField.RegisterCallback(blurHandler);

            return () =>
            {
                _pendingFlushActions.Remove(flushAction);
                colorField.UnregisterCallback(focusHandler);
                colorField.UnregisterCallback(blurHandler);
            };
        }
    }

}