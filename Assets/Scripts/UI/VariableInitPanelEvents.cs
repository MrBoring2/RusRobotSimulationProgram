using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Assets.Scripts.UI
{
    public class VariableInitPanelEvents : BaseModalWindow
    {
        private string varName = "";
        private string varValue = "";
        private Button confirmButton;
        private TextField varNameTextBox;
        private TextField varValueTextBox;

        protected override void Start()
        {
            base.Start();

        }

        protected override void OnBeforeShow(ModalParameters parameters)
        {
            if (messageLabel != null)
            {
                string message = parameters.Get("message", "Инициализация переменной");
                messageLabel.text = message;
            }
        }


        private void ConfirmCondition()
        {
            CloseWithValue(new PLCSetVariable { VariableName = varName, Value = varValue });
            varName = "";
            varValue = "";
            varNameTextBox.SetValueWithoutNotify("");
            varValueTextBox.SetValueWithoutNotify("");
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            varNameTextBox.RegisterCallback<ChangeEvent<string>>(p =>
            {
                varName = p.newValue;
            });
            varValueTextBox.RegisterCallback<ChangeEvent<string>>(p =>
            {
                varValue = p.newValue;
            });
            confirmButton.clicked += () => { ConfirmCondition(); };
        }
        protected override void InitializeElements(VisualElement root)
        {
            base.InitializeElements(root);

            messageLabel = root.Q<Label>("message-label");
            closeButton = root.Q<Button>("close-button");
            varNameTextBox = root.Q<TextField>("variable-name");
            varValueTextBox = root.Q<TextField>("set-field");
            confirmButton = root.Q<Button>("confirmBtn");
        }
    }
}
