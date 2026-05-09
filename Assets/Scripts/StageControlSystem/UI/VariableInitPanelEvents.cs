using Assets.Scripts.Models;
using Assets.UI.CustomElements;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
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
        private StringPopupField typesList;
        private string selectedType;

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
            typesList.choices = new List<string>
                {
                    "string",
                    "int",
                    "float",
                    "bool"
                };
        }


        private void ConfirmCondition()
        {
            VarType type = VarType.String;
            switch (selectedType)
            {
                case "string":
                    type = VarType.String;
                    break;
                case "int":
                    type = VarType.Int;
                    break;
                case "float":
                    type = VarType.Float;
                    break;
                case "bool":
                    type = VarType.Bool;
                    break;
                default:
                    break;
            }
            CloseWithValue(new PLCInitVariable { VarType = type, VariableName = varName, StartValue = varValue });
            varName = "";
            varValue = "";
            selectedType = "";
            typesList.SetValueWithoutNotify(null);
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
            typesList.RegisterCallback<ChangeEvent<string>>(p =>
            {
                selectedType = p.newValue;
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
            typesList = root.Q<StringPopupField>("types-list");
            confirmButton = root.Q<Button>("confirmBtn");
        }
    }
}
