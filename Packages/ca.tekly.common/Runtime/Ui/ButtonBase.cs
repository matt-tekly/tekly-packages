using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Common.Ui
{
    public class ButtonBase : Selectable, IPointerClickHandler
    {
        [Serializable]
        public class ClickEvent : UnityEvent<ButtonBase> { }

        public ClickEvent OnClick = new ClickEvent();
        public ClickEvent OnClickDisabled = new ClickEvent();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) {
                return;
            }

            Press();
        }

        private void Press()
        {
            if (!IsActive()) {
                return;
            }

            if (IsInteractable()) {
                UISystemProfilerApi.AddMarker("BaseButton.OnClick", this);
                OnClick.Invoke(this);
            } else {
                UISystemProfilerApi.AddMarker("BaseButton.OnClickDisabled", this);
                OnClickDisabled.Invoke(this);
            }
        }
    }
}