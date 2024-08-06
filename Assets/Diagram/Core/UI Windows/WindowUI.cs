using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diagram.UI
{
    [Serializable]
    public class WindowUI
    {
        public static string ResourcesWindowTopButtonPath = "Diagram/UI/WindowTopButton";
        public static string ResourcesWindowBox = "Diagram/UI/WindowBox";
        //public static string ResourcesWindowTitleBar = "Diagram/UI/WindowTitleBar";

        public WindowUITopButton sharedTopButton;
        public WindowUIBox sharedBox;
        //public WindowUITitleBar sharedTitleBar;

        public WindowUITopButton MyTopButton { get; private set; }
        public WindowUIBox MyBox { get; private set; }
        public WindowUITitleBar MyTitleBar { get; private set; }
        public WindowUIContainer MyContainer { get; private set; }

        /// <summary>
        /// <b>Build up:</b> Create windows component
        /// </summary>
        [_Init_]
        public void Apply()
        {
            if(MyTopButton != null)
            {
                GameObject.Destroy(MyTopButton.gameObject);
            }
            if (sharedTopButton != null)
            {
                MyTopButton = sharedTopButton.PrefabInstantiate();
            }
            if (MyBox != null)
            {
                GameObject.Destroy(MyBox.gameObject);
            }
            if (sharedBox != null)
            {
                MyBox = sharedBox.PrefabInstantiate();
                MyTitleBar = MyBox.SeekComponent<WindowUITitleBar>();
                MyContainer = MyBox.SeekComponent<WindowUIContainer>();
            }
            //if (sharedTitleBar != null)
            //{
            //    MyTitleBar = sharedTitleBar.PrefabInstantiate();
            //}
        }

        ~WindowUI()
        {
            sharedTopButton = null;
            sharedBox= null;
            //sharedTitleBar = null;
            Apply();
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
    }
}