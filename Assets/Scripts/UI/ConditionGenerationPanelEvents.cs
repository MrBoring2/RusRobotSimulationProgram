using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.ObjectsLibrary;
using Assets.Scripts.CustomEventBus.Signals.PLC;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class ConditionGenerationPanelEvents : BaseModalWindow
    {
        private string expression = "";
        private string currentParentObjectId = null;
        private Button confirmButton;
        private TextField textBox;

        protected override void Start()
        {
            base.Start();
            textBox = windowRoot.Q<TextField>("conditionField");  
        }

        protected override void OnBeforeShow(ModalParameters parameters)
        {
            currentParentObjectId = parameters.Get("currentParentObjectId", "");
            if (messageLabel != null)
            {
                string message = parameters.Get("message", "Библиотека объектов");
                messageLabel.text = message;
            }
        }


        private void ConfirmCondition()
        {
            CloseWithValue(expression);
            expression = "";
            textBox.SetValueWithoutNotify("");
          
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            textBox.RegisterCallback<ChangeEvent<string>>(p =>
            {
                expression = p.newValue;
            });
            confirmButton.clicked += () => { ConfirmCondition(); };
        }
        protected override void InitializeElements(VisualElement root)
        {
            base.InitializeElements(root);

            messageLabel = root.Q<Label>("message-label");
            closeButton = root.Q<Button>("close-button");
            textBox = windowRoot.Q<TextField>("conditionField");
            confirmButton = windowRoot.Q<Button>("confirmBtn");
        }
    }
}
