﻿using System.Collections.Generic;
using System.Linq;
using Diagram;
using UnityEngine;

namespace Game
{
    public class IJudgeModule : LineBehaviour
    {

    }

    public class ISoundModule : LineBehaviour
    { 
    
    }

    public class NoteGenerater : MinnesGenerater
    {
        public static void InitNoteGenerater()
        {
            MinnesGenerater.GenerateAction.Add("Note", () => Resources.Load<Note>("NoteBase").PrefabInstantiate());
        }

        public NoteGenerater() : base("Note","Untag")
        {
            target.name = "NoteBase";
            target.TimeListener = new();
        }
    }

    public class Note : MinnesController
    {
        public Dictionary<string, IJudgeModule> JudgeModules = new();
        public Dictionary<string, ISoundModule> SoundModules = new();
        public void LoadJudgeModule(string package, string name)
        {
            if (LoadSubGameObject(package, name).Share(out var obj) != null && obj.SeekComponent<IJudgeModule>().Share(out var module) != null)
                JudgeModules.Add(name, module);
        }
        public void LoadSoundModule(string package, string name)
        {
            if (LoadSubGameObject(package, name).Share(out var obj) != null && obj.SeekComponent<ISoundModule>().Share(out var module) != null)
                SoundModules.Add(name, module);
        }
        public float JudgeTime { get => this.FocusTime; set => this.FocusTime = value; }
        public void SetJudgeTime(float judgeTime) => SetFocusTime(judgeTime);
        public void InitNote(float judgeTime, string judge_module_package, string judge_module_name, string sound_module_package, string sound_module_name)
        {
            SetJudgeTime(judgeTime);
            LoadJudgeModule(judge_module_package, judge_module_name);
            LoadSoundModule(sound_module_package, sound_module_name);
        }

        public static Note FocusNote;
        private Material focusBoundLight;
        public Material FocusBoundLight
        {
            get
            {
                if (focusBoundLight == null)
                    focusBoundLight = this.meshRenderer.materials[1];
                return focusBoundLight;
            }
        }

        private void Update()
        {
            FocusBoundLight.SetFloat("_IsOpen", this == FocusNote ? 1 : 0);
        }

        private void OnMouseDown()
        {
            if(this != FocusNote)  FocusNote = this;
            else FocusNote = null;
        }
    }
}
