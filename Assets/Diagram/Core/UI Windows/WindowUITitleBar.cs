using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Diagram.UI
{
    /// <summary>
    /// <see langword="m_drag_helper.RT_Drag_Helper.TargetRectTransform"/> set parent item, and let this
    /// <para>script work in title bar</para>
    /// <para>Parent(Window Background)</para>
    /// <para>-->Title Bar(this)</para>
    /// <para>---->Icon</para>
    /// <para>---->Buttons...</para>
    /// <para>-->SubBlock</para>
    /// <para>---->Items...</para>
    /// </summary>
    public partial class WindowUITitleBar : LineBehaviour, IBeginDragHandler, IDragHandler, IWindowComponent
    {
        [SerializeField] private RT_Drag_Helper m_drag_helper;
        public TMP_Text TextComponent;
        WindowUI IWindowComponent.Core { get; set; }
        public string text
        {
            get => TextComponent.text;
            set => TextComponent.text = value;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            m_drag_helper.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            m_drag_helper.OnDrag(eventData);
        }

        public override void Reset()
        {
            base.Reset();
            m_drag_helper = new(transform.parent as RectTransform);
        }
    }
}
