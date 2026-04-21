using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.PLC;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.UI.CustomElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;

namespace Assets.Scripts.UI
{
    public class ProgramsSetPanelEvents : BaseModalWindow
    {
        private SceneObjectsManager _sceneObjectsManager;
        private RobotProgramObject selectedProgram;
        private string currentRobotId = null;
        private Button confirmButton;
        private ProgramsPopupField programsListPopupField;
        private List<RobotProgramObject> programs = new List<RobotProgramObject>();

        protected override void Start()
        {
            base.Start();
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        }
        protected override void OnBeforeShow(ModalParameters parameters)
        {
            currentRobotId = parameters.Get("robotId", "");
            programs = _sceneObjectsManager.Commands.GetSubPrograms(currentRobotId);
            programsListPopupField.choices = programs;
            if (messageLabel != null)
            {
                string message = parameters.Get("message", "Библиотека объектов");
                messageLabel.text = message;
            }
        }

        private void ConfirmCondition()
        {
            CloseWithValue(selectedProgram);
            selectedProgram = null;
        }
        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            programsListPopupField.RegisterCallback<ChangeEvent<RobotProgramObject>>(p =>
            {
                selectedProgram = p.newValue;
            });
            confirmButton.clicked += () => { ConfirmCondition(); };
        }
        protected override void InitializeElements(VisualElement root)
        {
            base.InitializeElements(root);

            messageLabel = root.Q<Label>("message-label");
            closeButton = root.Q<Button>("close-button");
            programsListPopupField = windowRoot.Q<ProgramsPopupField>("programs-list");
            programsListPopupField.choices = programs;
            confirmButton = windowRoot.Q<Button>("confirmBtn");
        }
    }
}
