using System;
using UnityEngine;

namespace Diagram.UI
{
    public interface IWindowComponent
    {
        WindowUI Core { get; internal set; }
    }

    [Serializable]
    public class WindowUI
    {
        public static string ResourcesWindowTopButtonPath = "Diagram/UI/WindowTopButton";
        public static string ResourcesWindowBox = "Diagram/UI/WindowBox";

        public WindowUITopButton sharedTopButton;
        public WindowUIBox sharedBox;
        public RectTransform EditBar, BoxParentPlane;
        public WindowUI(WindowUITopButton sharedTopButton, WindowUIBox sharedBox, RectTransform editBar, RectTransform boxParentPlane)
        {
            this.sharedTopButton = sharedTopButton;
            this.sharedBox = sharedBox;
            this.EditBar = editBar;
            this.BoxParentPlane = boxParentPlane;
        }
        public WindowUI(RectTransform editBar, RectTransform boxParentPlane) : this()
        {
            this.EditBar = editBar;
            this.BoxParentPlane = boxParentPlane;
        }
        public WindowUI() { LoadDefaultAtEmpty(); }


        public WindowUITopButton MyTopButton { get; private set; }
        public WindowUIBox MyBox { get; private set; }
        public WindowUITitleBar MyTitleBar { get; private set; }
        public WindowUIContainer MyContainer { get; private set; }

        /// <summary>
        /// <b>Build up:</b> Create windows component
        /// </summary>
        [_Init_]
        public void Rebuild()
        {
            if (MyBox != null)
            {
                GameObject.Destroy(MyBox.gameObject);
            }
            if (sharedBox != null)
            {
                MyBox = sharedBox.PrefabInstantiate();
                MyBox.transform.SetParent(BoxParentPlane, false);
                ((IWindowComponent)MyBox).Core = this;
                MyTitleBar = MyBox.SeekComponent<WindowUITitleBar>();
                ((IWindowComponent)MyTitleBar).Core = this;
                MyContainer = MyBox.SeekComponent<WindowUIContainer>();
                ((IWindowComponent)MyContainer).Core = this;
            }
            if(MyTopButton != null)
            {
                GameObject.Destroy(MyTopButton.gameObject);
            }
            if (sharedTopButton != null)
            {
                MyTopButton = sharedTopButton.PrefabInstantiate();
                MyTopButton.transform.SetParent(EditBar, false);
                ((IWindowComponent)MyTopButton).Core = this;
                MyTopButton.MyBox = MyBox;
                MyTopButton.Expand();
            }
        }
        public void Destroy()
        {
            if (MyTopButton != null)
            {
                GameObject.Destroy(MyTopButton.gameObject);
                MyTopButton = null;
            }
            if (MyBox != null)
            {
                GameObject.Destroy(MyBox.gameObject);
                MyBox = null;
            }
        }

        ~WindowUI()
        {
            Destroy();
        }

        public WindowUI LoadDefaultAtEmpty()
        {
            if (sharedTopButton == null)
                sharedTopButton = Resources.Load<WindowUITopButton>(ResourcesWindowTopButtonPath);
            if (sharedBox == null)
                sharedBox = Resources.Load<WindowUIBox>(ResourcesWindowBox);
            //if (sharedTitleBar == null)
            //    sharedTitleBar = Resources.Load<WindowUITitleBar>(ResourcesWindowTitleBar);
            return this;
        }

        public void PushElements(params GameObject[] elements)
        {
            MyContainer.PushLine();
            foreach (var element in elements)
            {
                MyContainer.AddElement(element);
            }
        }

        public void SetTitleName(string title)
        {
            MyTopButton.text = title;
            MyTitleBar.text = title;
        }
    }
}
