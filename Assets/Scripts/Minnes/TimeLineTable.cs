using System.IO;
using AD.Utility;
using Diagram;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    public class TimeLineTable : MinnesController,IOnDependencyCompleting,IPointerClickHandler
    {
        public static TimeLineTable instance;

        public void GenerateVirtualNoteLS()
        {
            using ToolFile file = new(Path.Combine(ToolFile.userPath, Minnes.ProjectName, "VirtualNote.ls"), true, false, true);
            file.ReplaceAllData((
                "note -> InitPosition(@StartX,@StartY,@StartZ)\n" +
                "note -> InitScale(@InitScaleX,@InitScaleY,@InitScaleZ)\n" +
                "note -> MakeMovement(0,@StartTime,@StartX,@StartY,@StartZ,@StartX,@StartY,@StartZ,0)\n" +
                "note -> MakeMovement(@StartTime,@JudgeTime,@StartX,@StartY,@StartZ,@EndX,@EndY,@EndZ,0)\n" +
                "note -> MakeMovement(@JudgeTime,@SongEndTime,@EndX,@EndY,@EndZ,@EndX,@EndY,@EndZ,0)\n" +
                "note -> MakeScale(0,@StartTime,0,0,0,0,0,0,0)\n" +
                "note -> MakeScale(@StartTime,@JudgeTime,@InitScaleX,@InitScaleY,@InitScaleZ,@InitScaleX,@InitScaleY,@InitScaleZ,0)\n" +
                "note -> MakeScale(@JudgeTime,@SongEndTime,0,0,0,0,0,0,0)\n" +
                "note -> InitNote(@JudgeTime,@JudgeModulePackage,@JudgeModuleName,@SoundModulePackage,@SoundModuleName)\n" +
                "note -> RegisterOnTimeLine()\n").ToByteArrayUTF8()
                );
            file.SaveFileData();
        }

        public void ReloadVirtualFolderNotes()
        {
            if (ToolFile.DirectoryExists(Path.Combine(ToolFile.userPath, Minnes.ProjectName, ".virtual")))
                foreach (var file in ToolFile.CreateDirectroryOfFile(Path.Combine(ToolFile.userPath, Minnes.ProjectName, ".virtual", "x.ls")).GetFiles())
                {
                    if (file.Extension == ".ls" && file.Name.StartsWith("note"))
                    {
                        new NoteGenerater().target.Share(out var note);
                        note.SetTargetScript(".virtual" + "/" + file.Name);
                        Resources
                            .Load<MinnesTimeLineItem>("TimeLineItem")
                            .PrefabInstantiate()
                            .As<MinnesTimeLineItem>()
                            .Share(out var item);
                        //.SetNote(LineScript.RunScript(Path.Combine(".virtual", file.Name), ("note", note))
                        //.CreatedInstances["note"] as Note);
                        item.transform.SetParent(this.transform, false);
                        item.SetNote(note as Note);
                        item.InitTablePosition();
                    }
                }
        }

        public void OnDependencyCompleting()
        {
            LineScript.RunScript("TimeTable.ls", ("this", this));
        }

        public float LeftBound = -6, RightBound = 6;
        public float StartZ = 0, EndZ = 0;
        public float StartY = 0;
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

        public static void SetupNote(Note note,float startTime,float endTime,float x,float y,float z,float x2,float y2,float z2)
        {
            note.UnregisterOnTimeLine();
            note.InitPosition(x, y, z);
            note.InitScale(1, 1, 1);
            note.MakeMovement(0, startTime, x, y, z, x2, y2, z2, 0);
            note.MakeMovement(startTime, endTime, x, y, z, x2, y2, z2, 0);
            note.MakeMovement(endTime, Minnes.MinnesInstance.ASC.CurrentClip.length, x2, y2, z2, x2, y2, z2, 0);
            note.MakeScale(0,startTime, 0, 0, 0, 0, 0, 0, 0);
            note.MakeScale(startTime, endTime, 1, 1, 1, 1, 1, 1, 0);
            note.MakeScale(startTime, endTime, 0, 0, 0, 0, 0, 0, 0);
            note.InitNote(endTime, "None", "None", "None", "None");
            note.RegisterOnTimeLine();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (MinnesStatsSharedPanel.EditTypeName == "Create")
            {
                float x = (eventData.position.x - rects[0].x) / (rects[2].x - rects[0].x);
                float y = (eventData.position.y - rects[0].y) / (rects[1].y - rects[0].y);
                Resources.Load<MinnesTimeLineItem>("TimeLineItem").PrefabInstantiate().Share(out var item);
                item.transform.SetParent(this.transform, false);
                var note = new NoteGenerater().target as Note;
                SetupNote(note,
                    Minnes.MinnesInstance.CurrentTick + (y - 1) * Minnes.ProjectNoteDefaultDisplayLength,
                    Minnes.MinnesInstance.CurrentTick + y * Minnes.ProjectNoteDefaultDisplayLength,
                    LeftBound + x * (RightBound - LeftBound), StartY, StartZ,
                    LeftBound + x * (RightBound - LeftBound), StartY, EndZ
                    );
                item.SetNote(note);
                item.InitTablePosition();
            }
        }

        private void Update()
        {           
            rects = Diagram.RectTransformExtension.GetRect(this.MyRectTransform);
        }

        private void Start()
        {
            instance = this;
            this.RegisterControllerOn(typeof(Minnes), new(), typeof(Minnes.StartRuntimeCommand),typeof(MinnesTimeline));
        }
    }
}
