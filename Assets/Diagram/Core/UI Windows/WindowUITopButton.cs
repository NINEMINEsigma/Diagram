using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Diagram.UI
{
    public class WindowUITopButton : LineBehaviour, IPointerClickHandler, IWindowComponent
    {
        public event Action OnClick;
        public WindowUIBox MyBox;
        public TMP_Text TextComponent;
        WindowUI IWindowComponent.Core { get; set; }
        public string text
        {
            get=>TextComponent.text;
            set
            {
                TextComponent.text = value;
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
            this.MyRectTransform.sizeDelta = new(Mathf.Max(text.Length * 12, 300), this.MyRectTransform.sizeDelta.x);
            IsExpand = true;
            MyBox.gameObject.SetActive(IsExpand);
        }
        public void Shrink()
        {
            this.MyRectTransform.sizeDelta = new(75, this.MyRectTransform.sizeDelta.x);
            IsExpand = false;
            MyBox.gameObject.SetActive(IsExpand);
        }
        public void SwitchExpandOrShrink()
        {
            if (IsExpand) Shrink();
            else Expand();
        }
        public void CloseWindow()
        {
            this.As<IWindowComponent>().Core.Destroy();
        }

        private void Start()
        {
            OnClick += SwitchExpandOrShrink;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            OnClick.Invoke();
        }
    }
}
