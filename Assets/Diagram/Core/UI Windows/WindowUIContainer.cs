using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Diagram.UI
{
    /// <summary>
    /// Automatic sorting module for windows<para></para>
    /// Add elements to it to reveal its content
    /// </summary>
    [Serializable]
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class WindowUIContainer : LineBehaviour, IWindowComponent
    {
        private VerticalLayoutGroup m_verticalLayoutGroup;
        public VerticalLayoutGroup MyVerticalLayoutGroup
        {
            get
            {
                if (m_verticalLayoutGroup == null)
                    m_verticalLayoutGroup = this.SeekComponent<VerticalLayoutGroup>();
                return m_verticalLayoutGroup;
            }
        }
        public Stack<HorizontalLayoutGroup> ChildsHorizontals = new();
        public HorizontalLayoutGroup CurrentLine => ChildsHorizontals.Count > 0 ? ChildsHorizontals.Peek() : null;
        public RectTransform CurrentLineRectT => CurrentLine == null ? null : CurrentLine.SeekComponent<RectTransform>();

        WindowUI IWindowComponent.Core { get; set; }

        protected RectTransform GetHorizontalLine()
        {
            GameObject newLine = new($"{transform.childCount}-horizontal-line", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            newLine.transform.SetParent(transform, false);
            var hlg = newLine.SeekComponent<HorizontalLayoutGroup>();
            hlg.childControlHeight = hlg.childControlWidth = false;
            hlg.childForceExpandHeight = hlg.childForceExpandWidth = false;
            hlg.childAlignment = TextAnchor.UpperLeft;
            var rect = newLine.SeekComponent<RectTransform>();
            rect.sizeDelta = Vector2.zero;
            return rect;
        }
        public void PushLine()
        {
            ChildsHorizontals.Push(GetHorizontalLine().SeekComponent<HorizontalLayoutGroup>());
        }
        public void PushLine(HorizontalLayoutGroup hlg)
        {
            ChildsHorizontals.Push(hlg);
        }
        public void PopLine(out HorizontalLayoutGroup hlg)
        {
            ChildsHorizontals.TryPop(out hlg);
        }
        public void PopLine()
        {
            ChildsHorizontals.TryPop(out var hlg);
            GameObject.Destroy(hlg.gameObject);
        }

        public WindowUIContainer AddElement(GameObject target)
        {
            if (CurrentLine == null)
                throw new DiagramException("There is no horizontal line on window");
            target.transform.SetParent(CurrentLineRectT, false);
            return this;
        }
    }
}
