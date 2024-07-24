using System.Collections;
using System.Collections.Generic;
using System.IO;
using AD.UI;
using Diagram;
using UnityEngine;
using UnityEngine.EventSystems; 

namespace Game
{
    public class TimeLineTable : MinnesController,IOnDependencyCompleting,IPointerClickHandler
    {
        public static TimeLineTable instance;

        public ModernUIDropdown NoteTypeDropdown;
        public ModernUIDropdown EditTypeDropdown;

        public void OnDependencyCompleting()
        {
            LineScript.RunScript("TimeTable.ls", ("this", this));
            foreach (var file in ToolFile.CreateDirectroryOfFile(Path.Combine(ToolFile.userPath, Minnes.ProjectName, ".virtual", "x.ls")).GetFiles())
            {
                if(file.Extension=="ls")
                {
                    Resources
                        .Load<TimeLineItem>("TimeLineItem")
                        .PrefabInstantiate()
                        .As<TimeLineItem>()
                        .Share(out var item)
                        .SetNote(LineScript.RunScript(Path.Combine(".virtual", file.Name), ("this", this)).CreatedInstances["note"] as Note);
                    item.MyNote.SetTargetScript(file.FullName);
                }
            }
            rects = Diagram.RectTransformExtension.GetRect(this.transform.As<RectTransform>());
            instance = this;
        }

        public float LeftBound, RightBound;
        public float StartZ,EndZ;
        public float StartY;
        public void SetBound(float left, float right)
        {
            this.LeftBound = left;
            this.RightBound = right;
        }
        public void SetTrackZ(float start,float end)
        {
            this.StartZ = start; 
            this.EndZ = end;
        }
        public void SetTrackY(float start)
        {
            this.StartY = start; 
        }

        public Vector3[] rects;

        private void SetupNote(Note note,float startTime,float endTime,float x,float y,float z,float x2,float y2,float z2)
        {
            note.SetJudgeTime(endTime);
            note.MakeMovement(startTime, endTime, x, y, z, x2, y2, z2, 0);
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            ToolFile.TryCreateDirectroryOfFile(Path.Combine(ToolFile.userPath, Minnes.ProjectName, ".virtual", "x.ls"));
            float x = (eventData.position.x - rects[0].x) / (rects[2].x - rects[0].x);
            float y = (eventData.position.y - rects[0].y) / (rects[1].y - rects[0].y);
            Resources.Load<TimeLineItem>("TimeLineItem").PrefabInstantiate().Share(out var item);
            item.transform.SetParent(this.transform, false);
            item.BuildupNote();
            SetupNote(item.MyNote,
                Minnes.MinnesInstance.CurrentTick + (y - 1) * MinnesTimeline.instance.TimeLineDisplayLength,
                Minnes.MinnesInstance.CurrentTick + y * MinnesTimeline.instance.TimeLineDisplayLength,
                LeftBound + x * (RightBound - LeftBound), StartY, StartZ,
                LeftBound + x * (RightBound - LeftBound), StartY, EndZ
                );
            item.WriteLineScript();
        }

        private void Start()
        {
            this.RegisterControllerOn(typeof(Minnes), new(), typeof(Minnes.StartRuntimeCommand),typeof(MinnesTimeline));
        }
    }
}
