using System;
using System.Collections.Generic;

using UnityEngine;

namespace Diagram
{
    /// <summary>
    /// 鼠标光标类型枚举
    /// </summary>
    public enum CursorType
    {
        None,
        UpDown,//上下箭头
        LeftRight,//左右箭头
        LeftOblique,//左斜箭头
        RightOblique//右斜箭头
    }

    [Serializable]
    public partial class CursorManager
    {
        public static CursorManager instance;
        private Dictionary<CursorType, Texture2D> dicCursors = new Dictionary<CursorType, Texture2D>();

        private void Awake()
        {
            instance = this;
        }
        /// <summary>
        /// 加载鼠标光标保存到字典
        /// </summary>
        public void LoadCursor()
        {
            string[] cursors = System.Enum.GetNames(typeof(CursorType));
            for (int i = 0; i < cursors.Length; i++)
            {
                Texture2D sprite = Resources.Load<Texture2D>("cursor/" + cursors[i]);
                CursorType type = (CursorType)System.Enum.Parse(typeof(CursorType), cursors[i]);//根据字符串名转换为对应枚举类型
                if (!dicCursors.ContainsKey(type))
                {
                    dicCursors.Add(type, sprite);
                }
            }
        }
        /// <summary>
        /// 根据枚举设置不同的鼠标光标
        /// </summary>
        /// <param name="type"></param>
        public void SetCursor(CursorType type)
        {
            if (dicCursors.ContainsKey(type))
            {
                //因为这里的光标图片大小是32*32像素的，所以需要设置切换光标后鼠标位置为该图片中心位置
                //即偏移为(16,16),避免在UI边缘检测时出现误差
                Cursor.SetCursor(dicCursors[type], new Vector2(16, 16), CursorMode.Auto);
            }
        }
        //恢复默认光标
        public void SetDefaultCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
