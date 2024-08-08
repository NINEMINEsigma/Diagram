using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using Diagram;
using UnityEngine;

namespace Game
{
    public class IJudgeModule : MinnesController
    {

    }

    public class ISoundModule : MinnesController
    {
        [SerializeField] private AudioSystem source;
        public AudioSystem Source
        {
            get
            {
                if (source == null)
                    source = this.SeekComponent<AudioSystem>();
                if (source == null)
                    source = this.gameObject.AddComponent<AudioSystem>();
                return source;
            }
        }
        private SortedList<float, bool> TimeClocker = new(); 
        public void LoadAudio(string audio)
        {
            Source.LoadOnUrl(audio,AudioSystem.GetAudioType(audio));
        }

        public bool IsInvoked = false;
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
        public override void ReloadLineScript()
        {
            if (TimeListener == null) return;
            if (MyScriptName == null || MyScriptName.Length == 0) return;
            this.TimeListener.Clear();
            LineScript.RunScript(MyScriptName, ("this", this), ("note", this));
        }

        public Dictionary<string, IJudgeModule> JudgeModules = new();
        public Dictionary<string, ISoundModule> SoundModules = new();
        public IJudgeModule LoadJudgeModule(string package, string name)
        {
            IJudgeModule module = null;
            if (LoadSubGameObject(package, name).Share(out var obj) != null && obj.SeekComponent<IJudgeModule>().Share(out module) != null)
                JudgeModules.Add(name, module);
            return module;
        }
        public ISoundModule LoadSoundModule(string package, string name)
        {
            ISoundModule module = null;
            if (LoadSubGameObject(package, name).Share(out var obj) != null && obj.SeekComponent<ISoundModule>().Share(out module) != null)
                SoundModules.Add(name, module);
            return module;
        }
        public float JudgeTime { get => this.FocusTime; set => this.FocusTime = value; }
        public void SetJudgeTime(float judgeTime) => SetFocusTime(judgeTime);
        public void InitNote(float judgeTime, string judge_module_package, string judge_module_name, string sound_module_package, string sound_module_name)
        {
            SetJudgeTime(judgeTime);
            LoadJudgeModule(judge_module_package, judge_module_name);
            LoadSoundModule(sound_module_package, sound_module_name);
        }

        public static Note _focusNote;
        public static Note FocusNote
        {
            get => _focusNote;
            set
            {
                if (_focusNote == value) return;
                if (_focusNote != null)
                    _focusNote.FocusBoundLight.SetFloat("_IsOpen", 0);
                if (value != null)
                {
                    if(string.IsNullOrEmpty( value.MyScriptName)==false)
                    {
                        MinnesTimeline.instance.Stats.SetTitle("Focus Note <Runtime> " + value.MyScriptName);
                    }
                    else
                    {
                        MinnesTimeline.instance.Stats.SetTitle("Focus Note <Static>");
                    }
                    value.FocusBoundLight.SetFloat("_IsOpen", 1);
                }
                _focusNote = value;
            }
        }
        private Material focusBoundLight;
        public Material FocusBoundLight
        {
            get
            {
                if (focusBoundLight == null)
                    focusBoundLight = this.MyMeshRenderer.materials[1];
                return focusBoundLight;
            }
        }

        private void OnMouseDown()
        {
            if(this != FocusNote)  FocusNote = this;
            else FocusNote = null;
        }

        public ISoundModule GetSoundModule(string name)
        {
            return SoundModules[name];
        }
        public Note MakeSoundPlay(float startTime, string name)
        {
            var audio = GetSoundModule(name);
            TimeListener.TryAdd($"SoundPlay-{startTime}", (float time, float stats) =>
            {
                if (startTime > time)
                {
                    audio.IsInvoked = false;
                }
                else if (audio.IsInvoked == false && startTime <= time && stats <= Minnes.ProjectNoteMaxDisplayLength)
                {
                    audio.IsInvoked = true;
                    audio.Source.Play();
                }
            });
            return this;
        }
        public IJudgeModule GetJudgeModule(string name)
        {
            return JudgeModules[name];
        }
        public Note MakeJudgeEffectPlay(float startTime,string name)
        {
            return this;
        }
    }
}
