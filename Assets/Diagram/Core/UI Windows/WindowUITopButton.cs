using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Diagram.UI
{
    public class WindowUITopButton : LineBehaviour, IPointerClickHandler
    {
        public event Action OnClick;
        public WindowUIBox MyBox;
        public WindowUITitleBar MyTitleBar;
        public Text TextComponent;
        public string text
        {
            get=>TextComponent.text;
            set
            {
                TextComponent.text = value;
                MyTitleBar.text = value;
            }
        }
        public void SetWindowActive(bool active)
        {
            this.gameObject.SetActive(active);
            MyBox.gameObject.SetActive(active);
        }

        private bool IsExpand = true;
        public void Expand()
        {
            this.MyRectTransform.sizeDelta = new(Mathf.Min(text.Length * 12, 600), this.MyRectTransform.sizeDelta.x);
        }
        public void Shrink()
        {
            this.MyRectTransform.sizeDelta = new(75, this.MyRectTransform.sizeDelta.x);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (IsExpand) Shrink();
            else Expand();
        }
    }
}
