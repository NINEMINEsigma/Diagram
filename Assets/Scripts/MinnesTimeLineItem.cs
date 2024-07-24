using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AD.Utility;
using Diagram;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game
{
    public class TimeLineItem : LineBehaviour,IPointerClickHandler
    {
        public Note MyNote;

        public void WriteLineScript()
        {
            if (this.MyNote.MyScriptName == null || this.MyNote.MyScriptName.Length == 0) this.MyNote.MyScriptName = ".virtual/note" + MyNote.GetHashCode().ToString() + ".ls";
            using ToolFile file = new(Path.Combine(LineScript.BinPath, this.MyNote.MyScriptName), true, false, true);
            //string str=
            //    $"call \"Note.ls\" import {MyNote.TimeListener.First()}"
        }
        public void BuildupNote()
        {
            SetNote(new NoteGenerater().target as Note);
        }
        public void SetNote(Note note)
        {
            MyNote = note;
        }
        public void LoadIcon(string icon)
        {
            this.SeekComponent<View>().LoadOnUrl(icon);
        }

        private void Update()
        {
            float t = (MyNote.JudgeTime - Minnes.MinnesInstance.CurrentTick) / MinnesTimeline.instance.TimeLineDisplayLength;
            if (t > 1 || t < -1) return;
            var rects = TimeLineTable.instance.rects;
            float x = (MyNote.transform.position.x - TimeLineTable.instance.LeftBound) / (TimeLineTable.instance.RightBound - TimeLineTable.instance.LeftBound);
            float y = (MyNote.JudgeTime - MinnesTimeline.instance.CurrnetTime) / MinnesTimeline.instance.TimeLineDisplayLength;
            this.transform.position = new(rects[0].x + x * (rects[2].x - rects[0].x), rects[0].y + y * (rects[1].y - rects[0].y), 0);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Minnes.MinnesInstance.ASC.IsPlay) return;
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.E].isPressed)
            {
                MyNote.transform.localScale = MyNote.transform.localScale.AddX(Keyboard.current[Key.LeftAlt].isPressed ? -1 : 1);
                WriteLineScript();
            }
        }
    }
}
