using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine.UIElements;

namespace Assets.Scripts.Models
{
    public interface IModalWindow
    {
        string WindowId { get; }
        void Show(string message, ModalParameters parameters);
        void Show(string message, ModalParameters parameters, Action<object> onClose);
        void Hide();
        void Hide(object returnValue);
        bool IsVisible { get; }
        VisualElement RootElement { get; }
    }
}
