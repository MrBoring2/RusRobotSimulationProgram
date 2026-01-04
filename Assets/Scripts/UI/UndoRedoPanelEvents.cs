using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.SystemManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;                                 
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class UndoRedoPanelEvents : MonoBehaviour
    {
        public TooltipEvents tooltipEvents;
        private VisualElement root;
        private UndoRedoManager _undoRedoManager;
        private void Start()
        {
            _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
            root = GetComponent<UIDocument>().rootVisualElement;
            var backBtn = root.Q<Button>("back-button");
            var forwardBtn = root.Q<Button>("forward-button");
            tooltipEvents.RegisterTooltip(backBtn, "Назад");
            tooltipEvents.RegisterTooltip(forwardBtn, "Вперёд");
            backBtn.RegisterCallback<ClickEvent>(evt =>
            {
                Undo();
            });
            forwardBtn.RegisterCallback<ClickEvent>(evt =>
            {
                Redo();
            });
        }

        private void Redo()
        {
            _undoRedoManager.Redo();
        }

        private void Undo()
        {
            _undoRedoManager.Undo();
        }
    }
}
