using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.UI.CustomElements;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

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
        private UIStatusManager _uiStatusManager;
        private string selectedType;
        private NotificationSystemManager _notificationSystemManager;

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnBeforeShow(ModalParameters parameters)
        {
            _uiStatusManager = ServiceManager.Current.Get<UIStatusManager>();
            _notificationSystemManager = ServiceManager.Current.Get<NotificationSystemManager>();

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
            if (varName.Contains(" "))
            {
                _notificationSystemManager.ShowWarning("Название переменной должно быть без пробелов!");
                return;
            }

            if (varValue == "")
            {
                _notificationSystemManager.ShowWarning("Значение переменной не омжет быть пустым!");
                return;
            }

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

            string[] validBoolValues = { "true", "True", "false", "False" };
            if (type == VarType.Bool && !validBoolValues.Contains(varValue))
            {
                _notificationSystemManager.ShowWarning("Булевая переменная можеть иметь значение только true или false!");
                return;
            }

            if (type == VarType.Int && !int.TryParse(varValue, out int v))
            {
                _notificationSystemManager.ShowWarning("Значение не подходит для целочисленной переменной!");
                return;
            }

            if (type == VarType.Float && !float.TryParse(varValue, out float v2))
            {
                _notificationSystemManager.ShowWarning("Значение не подходит для вещественной переменной или вместо запятой стоит точка!");
                return;
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
            varNameTextBox.RegisterCallback<FocusEvent>((e) =>
            {
                _uiStatusManager.SetInputMode(true);
            });
            varNameTextBox.RegisterCallback<BlurEvent>((e) =>
            {
                _uiStatusManager.SetInputMode(false);
            });
            varValueTextBox.RegisterCallback<ChangeEvent<string>>(p =>
            {
                varValue = p.newValue;
            });
            varValueTextBox.RegisterCallback<FocusEvent>((e) =>
            {
                _uiStatusManager.SetInputMode(true);
            });
            varValueTextBox.RegisterCallback<BlurEvent>((e) =>
            {
                _uiStatusManager.SetInputMode(false);
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
