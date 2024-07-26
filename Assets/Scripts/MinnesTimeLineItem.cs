using System;
using System.IO;
using System.Text;
using AD.Utility;
using Diagram;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game
{
    public class MinnesTimeLineItem : LineBehaviour,IPointerClickHandler,IDragHandler,IBeginDragHandler,IEndDragHandler
    {
        public Note MyNote;

        public void WriteLineScript()
        {
            if (ToolFile.TryCreateDirectroryOfFile(Path.Combine(ToolFile.userPath, Minnes.ProjectName, ".virtual", "x.ls"))) { }
            if (this.MyNote.MyScriptName == null || this.MyNote.MyScriptName.Length == 0)
            {
                this.MyNote.MyScriptName = $".virtual/note{MyNote.GetHashCode().ToString()}_{MyNote.JudgeTime.GetHashCode()}.ls";
                using ToolFile file = new(Path.Combine(LineScript.BinPath, this.MyNote.MyScriptName), true, false, true);
                string str =
                    $"include \"MinnesNoteDefine.ls\"\n\n" +
                    $"define @StartX = {MyNote.transform.position.x}\n" +
                    $"define @EndX = {MyNote.transform.position.x}\n" +
                    $"define @StartY = {MyNote.transform.position.y}\n" +
                    $"define @EndY = {MyNote.transform.position.y}\n" +
                    $"define @StartTime = {MyNote.JudgeTime - Minnes.ProjectNoteMaxDisplayLength}\n" +
                    $"define @JudgeTime = {MyNote.JudgeTime}\n" +
                    $"define @InitScaleX = {MyNote.transform.localScale.x}\n" +
                    $"define @InitScaleY = {MyNote.transform.localScale.y}\n" +
                    $"define @InitScaleZ = {MyNote.transform.localScale.z}\n" +
                    $"call \"VirtualNote.ls\"\n";
                file.ReplaceAllData(str.ToByteArrayUTF8());
                file.SaveFileData();
            }
            else
            {
                using ToolFile file = new(Path.Combine(LineScript.BinPath, this.MyNote.MyScriptName), true, false, true);
                string[] lines = file.GetString(true, Encoding.UTF8).Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    lines[i] = lines[i].Trim(' ', '\r');
                    if (line.Contains("MinnesNoteDefine.ls"))
                        lines[i] = "";
                    else if (line.Contains("@StartY") || line.Contains("@EndY"))
                        lines[i] = lines[i].Trim(' ') + "\n";
                    else if (line.Contains("@") && line.Contains("define"))
                        lines[i] = "";
                    else
                        lines[i] += "\n";
                }
                string newString = lines.Link();
                string newDefine =
                    $"include \"MinnesNoteDefine.ls\"\n\n" +
                    $"define @StartX = {MyNote.transform.position.x}\n" +
                    $"define @EndX = {MyNote.transform.position.x}\n" +
                    //$"define @StartY = {MyNote.transform.position.y}\n" +
                    //$"define @EndY = {MyNote.transform.position.y}\n" +
                    $"define @StartTime = {MyNote.JudgeTime - Minnes.ProjectNoteMaxDisplayLength}\n" +
                    $"define @JudgeTime = {MyNote.JudgeTime}\n" +
                    $"define @InitScaleX = {MyNote.transform.localScale.x}\n" +
                    $"define @InitScaleY = {MyNote.transform.localScale.y}\n" +
                    $"define @InitScaleZ = {MyNote.transform.localScale.z}\n";
                file.ReplaceAllData((newDefine + newString).ToByteArrayUTF8());
                file.SaveFileData();
            }
        }
        public void SetNote(Note note)
        {
            if (MyNote == null && note == null)
            {
                throw new BadImageFormatException();
            }
            if (MyNote)
            {
                ToolFile.DeleteFile(Path.Combine(LineScript.BinPath, MyNote.MyScriptName));
                MyNote.UnregisterOnTimeLine();
                GameObject.Destroy(MyNote.gameObject);
            }
            if (note)
            {
                MyNote = note;
                note.TimeListener.Clear();
                if (this.MyNote.MyScriptName == null || this.MyNote.MyScriptName.Length == 0)
                {
                    WriteLineScript();
                    note.name = note.MyScriptName;
                }
                note.UnregisterOnTimeLine();
                note.ReloadLineScript();
            }
        }
        public void LoadIcon(string icon)
        {
            this.SeekComponent<View>().LoadOnUrl(icon);
        }

        private void Update()
        {
            if (Minnes.MinnesInstance.ASC.IsPlay)
            {
                if (Note.FocusNote == MyNote)
                    Note.FocusNote = null;
            }
            float t = (MyNote.JudgeTime - Minnes.MinnesInstance.CurrentTick) / MinnesTimeline.instance.TimeLineDisplayLength;
            if (t > 2 || t < -1) return;
            if(Note.FocusNote == MyNote) return;
            if (isDrag) return;
            var rects = TimeLineTable.instance.rects;
            float x = (MyNote.transform.position.x - TimeLineTable.instance.LeftBound) / (TimeLineTable.instance.RightBound - TimeLineTable.instance.LeftBound);
            float y = (MyNote.JudgeTime - MinnesTimeline.instance.CurrnetTime) / MinnesTimeline.instance.TimeLineDisplayLength;
            this.transform.position = new(rects[0].x + x * (rects[2].x - rects[0].x), rects[0].y + y * (rects[1].y - rects[0].y), 0);
        }
        public void InitTablePosition()
        {
            float t = (MyNote.JudgeTime - Minnes.MinnesInstance.CurrentTick) / MinnesTimeline.instance.TimeLineDisplayLength; 
            var rects = TimeLineTable.instance.rects;
            float x = (MyNote.transform.position.x - TimeLineTable.instance.LeftBound) / (TimeLineTable.instance.RightBound - TimeLineTable.instance.LeftBound);
            float y = (MyNote.JudgeTime - MinnesTimeline.instance.CurrnetTime) / MinnesTimeline.instance.TimeLineDisplayLength;
            this.transform.position = new(rects[0].x + x * (rects[2].x - rects[0].x), rects[0].y + y * (rects[1].y - rects[0].y), 0);
        }


        RectTransform LimitContainer;
        public Canvas canvas;
        RectTransform rt => MyRectTransform;
        Vector3 offset = Vector3.zero;
        float minX, maxX, minY, maxY;
        bool isDrag = false;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Minnes.MinnesInstance.ASC.IsPlay) return;
            Note.FocusNote = this.MyNote;
            if (MinnesStatsSharedPanel.EditTypeName == "Delete")
            {
                SetNote(null);
                GameObject.Destroy(this.gameObject);
            }
            else if (MinnesStatsSharedPanel.EditTypeName == "Create")
            {
                if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.E].isPressed)
                {
                    MyNote.transform.localScale = MyNote.transform.localScale.AddX(Keyboard.current[Key.LeftAlt].isPressed ? -1 : 1);
                    WriteLineScript();
                }
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (Minnes.MinnesInstance.ASC.IsPlay) return;
            //if (Note.FocusNote != this.MyNote) return;
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            // 将屏幕空间上的点转换为位于给定RectTransform平面上的世界空间中的位置
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector3 globalMousePos))
            {
                rt.position = DragRangeLimit(globalMousePos + offset);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Minnes.MinnesInstance.ASC.IsPlay) return;
            OnPointerClick(eventData);
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            LimitContainer = this.transform.parent.transform as RectTransform;
            canvas = transform.parent.parent.parent.GetComponent<Canvas>();
            isDrag = true;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, eventData.enterEventCamera, out Vector3 globalMousePos))
            {
                // 计算偏移量
                offset = rt.position - globalMousePos;
                // 设置拖拽范围
                SetDragRange();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Minnes.MinnesInstance.ASC.IsPlay) return;
            if (Note.FocusNote == MyNote)
                Note.FocusNote = null;
            MyNote.UnregisterOnTimeLine();
            isDrag = false;
            float oneBarScale = 60.0f / Minnes.ProjectBPM / (float)MinnesTimeline.instance.BarLineColorsList[MinnesTimeline.instance.BarColorPointer].Length;
            float x = (transform.position.x - TimeLineTable.instance.rects[0].x) / (TimeLineTable.instance.rects[2].x - TimeLineTable.instance.rects[0].x);
            float y = (transform.position.y - TimeLineTable.instance.rects[0].y) / (TimeLineTable.instance.rects[1].y - TimeLineTable.instance.rects[0].y);
            float
                startTime = oneBarScale * (int)((Minnes.MinnesInstance.CurrentTick + (y - 1) * MinnesTimeline.instance.TimeLineDisplayLength) / oneBarScale),
                endTime = oneBarScale * (int)((Minnes.MinnesInstance.CurrentTick + y * MinnesTimeline.instance.TimeLineDisplayLength) / oneBarScale);
            //TimeLineTable.SetupNote(MyNote,
            //    startTime,
            //    endTime,
            //    TimeLineTable.instance.LeftBound + x * (TimeLineTable.instance.RightBound - TimeLineTable.instance.LeftBound), TimeLineTable.instance.StartY, TimeLineTable.instance.StartZ,
            //   TimeLineTable.instance.LeftBound + x * (TimeLineTable.instance.RightBound - TimeLineTable.instance.LeftBound), TimeLineTable.instance.StartY, TimeLineTable.instance.EndZ
            //    );
            MyNote.SetJudgeTime(endTime);
            var scale = MyNote.transform.localScale;
            var rot = MyNote.transform.rotation;
            float a = 0, b = 0;
            MyNote.TimeUpdate(ref a, ref b);
            MyNote.transform.position = new(
                TimeLineTable.instance.LeftBound + x * (TimeLineTable.instance.RightBound - TimeLineTable.instance.LeftBound),
                MyNote.transform.position.y,
                MyNote.transform.position.z);
            MyNote.transform.localScale = scale;
            MyNote.transform.rotation = rot;
            WriteLineScript();
            MyNote.ReloadLineScript();
        }

        // 设置最大、最小坐标
        void SetDragRange()
        {
            // 最小x坐标 = 容器当前x坐标 - 容器轴心距离左边界的距离 + UI轴心距离左边界的距离
            minX = LimitContainer.position.x
                - LimitContainer.pivot.x * LimitContainer.rect.width * canvas.scaleFactor
                + rt.rect.width * canvas.scaleFactor * rt.pivot.x;
            // 最大x坐标 = 容器当前x坐标 + 容器轴心距离右边界的距离 - UI轴心距离右边界的距离
            maxX = LimitContainer.position.x
                + (1 - LimitContainer.pivot.x) * LimitContainer.rect.width * canvas.scaleFactor
                - rt.rect.width * canvas.scaleFactor * (1 - rt.pivot.x);

            // 最小y坐标 = 容器当前y坐标 - 容器轴心距离底边的距离 + UI轴心距离底边的距离
            minY = LimitContainer.position.y
                - LimitContainer.pivot.y * LimitContainer.rect.height * canvas.scaleFactor
                + rt.rect.height * canvas.scaleFactor * rt.pivot.y;

            // 最大y坐标 = 容器当前x坐标 + 容器轴心距离顶边的距离 - UI轴心距离顶边的距离
            maxY = LimitContainer.position.y
                + (1 - LimitContainer.pivot.y) * LimitContainer.rect.height * canvas.scaleFactor
                - rt.rect.height * canvas.scaleFactor * (1 - rt.pivot.y);
        }
        // 限制坐标范围
        Vector3 DragRangeLimit(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            return pos;
        }
    }
}
