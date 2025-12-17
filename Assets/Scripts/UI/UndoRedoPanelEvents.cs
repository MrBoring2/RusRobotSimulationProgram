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
        private VisualElement root;
        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            var backBtn = root.Q<Button>("back-button");
            var forwardBtn = root.Q<Button>("forward-button");
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
            UndoRedoSystem.Instance.Redo();
        }

        private void Undo()
        {
            Debug.Log("DDSADASDASD@!#!@#@!");
            UndoRedoSystem.Instance.Undo();
        }
    }
}
