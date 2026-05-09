using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.UI.CustomElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Assets.Scripts.UI
{
    public class VariableSetPanelEvents : BaseModalWindow
    {
        private Button confirmButton;
        private VariablePopupField variablesList;
        private StringPopupField operationsList;
        private TextField varValueTextBox;
        private string varValue;
        private SceneObjectsManager _sceneObjectManager;
        private Variable selectedVariable;
        private string selectedOperation;

        protected override void Start()
        {
            base.Start();

        }

        protected override void OnBeforeShow(ModalParameters parameters)
        {
            _sceneObjectManager = ServiceManager.Current.Get<SceneObjectsManager>();

            if (messageLabel != null)
            {
                string message = parameters.Get("message", "Инициализация переменной");
                messageLabel.text = message;
            }
            operationsList.choices = new List<string>
            {
                "Присвоить",
                "Инкремент",
                "Декремент"
            };
            operationsList.value = operationsList.choices[0];
            variablesList.choices = _sceneObjectManager.PLCData.Variables;
        }


        private void ConfirmCondition()
        {
            OperationType operation = OperationType.Assign;
            switch (selectedOperation)
            {
                case "Присвоить":
                    operation = OperationType.Assign;
                    break;
                case "Инкремент":
                    operation = OperationType.Increment;
                    break;
                case "Декремент":
                    operation = OperationType.Decrement;
                    break;
                default:
                    break;
            }
            CloseWithValue(new PLCSetVariable { VarType = selectedVariable.VarType, Operation = operation, VariableName = selectedVariable.Name, Value = varValue });
            selectedOperation = operationsList.choices[0];
            selectedVariable = variablesList.choices[0];
            operationsList.value = operationsList.choices[0];
            variablesList.SetValueWithoutNotify(variablesList.choices[0]);
            varValue = "";
            varValueTextBox.SetValueWithoutNotify("");
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            varValueTextBox.RegisterCallback<ChangeEvent<string>>(p =>
            {
                varValue = p.newValue;
            });
            operationsList.RegisterCallback<ChangeEvent<string>>(p =>
            {
                selectedOperation = p.newValue;
                switch (selectedOperation)
                {
                    case "Присвоить":
                        variablesList.choices = _sceneObjectManager.PLCData.Variables;
                        break;
                    case "Инкремент":
                    case "Декремент":
                        variablesList.choices = _sceneObjectManager.PLCData.Variables.Where(p => p.VarType == VarType.Int || p.VarType == VarType.Float).ToList();
                        break;
                    default:
                        break;
                }
                if (variablesList.choices.Count > 0)
                {
                    selectedVariable = variablesList.choices[0];
                    variablesList.SetValueWithoutNotify(selectedVariable);
                }
                else
                {
                    selectedVariable = null;
                    variablesList.SetValueWithoutNotify(null);
                }
            });
            variablesList.RegisterCallback<ChangeEvent<Variable>>(p =>
            {
                selectedVariable = p.newValue;
            });
            confirmButton.clicked += () => { ConfirmCondition(); };
        }
        protected override void InitializeElements(VisualElement root)
        {
            base.InitializeElements(root);

            messageLabel = root.Q<Label>("message-label");
            closeButton = root.Q<Button>("close-button");
            varValueTextBox = root.Q<TextField>("set-field");
            variablesList = root.Q<VariablePopupField>("variable-name");
            operationsList = root.Q<StringPopupField>("operations-list");
            confirmButton = root.Q<Button>("confirmBtn");
        }
    }
}

