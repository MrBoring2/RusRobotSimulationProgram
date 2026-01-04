using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Managers
{
    public class UIStatusManager : MonoBehaviour, IService
    {
        public bool isPointerOverUI { get; private set; }
        public bool isInputMode { get; private set; }
        public void Init()
        {
            isInputMode = false;
            isPointerOverUI = false;
        }

        public void SetInputMode(bool isInputMode)
        {
            this.isInputMode = isInputMode;
        }
        public void SetPointerOberUI(bool isPointerOverUI)
        {
            this.isPointerOverUI = isPointerOverUI;
        }

        public void AddContextMenu(VisualElement element)
        {
            
        }
        public void AddModalWindow(VisualElement element)
        {

        }
    }
}
