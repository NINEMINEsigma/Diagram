using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AD;
using AD.UI;
using AD.Utility;
using Diagram;
using Diagram.Arithmetic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class Minnes : LineBehaviour
    {
        public static float PastTime = 0;
        public void LoadScene(string package, string sceneName)
        {
            using ToolFile file = new(package, false, true, false);
            var ab = file.LoadAssetBundle();
            sceneName.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive);//.MarkCompleted
        }
        public void SetSpeed(float speed) => ProjectSpeed = speed;
        public void SetDisplayMaxLength(float max) => ProjectNoteMaxDisplayLength = max;
        public AudioSystem ASC;
        public void LoadSong(string song)
        {
            song = Path.Combine(LineScript.BinPath, song);
            ASC.LoadOnUrl(song, AudioSourceController.GetAudioType(song));
        }
        public void WaitForPlay(float time)
        {
            StartCoroutine(IWaitForPlay(time));
            IEnumerator IWaitForPlay(float time)
            {
                yield return new WaitForSeconds(time);
                while (ASC.CurrentClip == null) yield return null;
                ASC.Play();
                ASC.CurrentTime = -time;
            }
        }
        public void PlaySong()
        {
            ASC.Play();
        }
        public void StopSong()
        {
            ASC.Stop();
        }
        public void PauseSong()
        {
            ASC.Pause();
        }
        public void PlayOrPauseSong()
        {
            ASC.PlayOrPause();
        }
        public void SetBPM(float bpm) => ProjectBPM = bpm;
        public void SetProject(string projectName)
        {
            ProjectName = projectName;
            LineScript.BinPath = Path.Combine(ToolFile.userPath, ProjectName);
        }

        public void ReloadScripts()
        {
            ADGlobalSystem.instance.OnEnd();
        }

        public static string ProjectName = "Test";
        public static string ProjectPath => Path.Combine(ToolFile.userPath, ProjectName);
        public static float ProjectBPM = 60;
        public static float ProjectSpeed = 1;
        public static float ProjectNoteDefaultDisplayLength => ProjectNoteMaxDisplayLength / ProjectSpeed;
        public static float ProjectNoteMaxDisplayLength = 3;
        public static Minnes MinnesInstance;

        public class StartRuntimeCommand { }
        public List<MinnesController> EnableContronller = new();

        private void Awake()
        {
            ArchitectureDiagram.RegisterArchitecture(this);
            MinnesInstance = this;
            Game.NoteGenerater.InitNoteGenerater();
            Game.LongNoteBodyGenerater.InitNoteGenerater();
            {
                using ToolFile file = new(Path.Combine(ToolFile.userPath, "MinnesConfig.ls"), true, true, false);
                new LineScript(("this", this)).Run(file.GetString(false, System.Text.Encoding.UTF8));
            }
            {
                LineScript.RunScript("Minnes.ls", ("this", this)).ImportScriptAndRun("MinnesNoteDefine.ls").ImportScriptAndRun(ProjectName + ".ls");
            }
            foreach (var item in EnableContronller)
            {
                item.gameObject.SetActive(true);
            }

            ADGlobalSystem.instance.OnSceneEnd.AddListener(this.ASC.PrepareToOtherScene);

            StartCoroutine(Wait());
            IEnumerator Wait()
            {
                while (ASC.CurrentClip == null)
                    yield return null;
                this.Architecture<Minnes>().Register<StartRuntimeCommand>(new BaseWrapper.Model(new StartRuntimeCommand()));
                ASC.CurrentTime = PastTime;
            }
        }
        private void OnDestroy()
        {
            ArchitectureDiagram.UnregisterArchitecture<Minnes>();
            MinnesGenerater.GenerateAction.Clear();
        }

        [SerializeField] private float RawPastTick = 0;
        [SerializeField] private float RawCurrentTick = 0;
        [SerializeField] private float RawLimitTick = 3;
        public float CurrentTick
        {
            get => RawPastTick;
            set => RawCurrentTick = value;
        }
        public float CurrentStats => RawCurrentTick - RawPastTick;
        public List<MinnesController> AllControllers = new();
        private void Update()
        {
            if (Keyboard.current[Key.LeftCtrl].isPressed && Keyboard.current[Key.R].wasPressedThisFrame)
            {
                if (Note.FocusNote == null)
                {
                    PastTime = CurrentTick;
                    ADGlobalSystem.instance.OnEnd();
                }
                else if (string.IsNullOrEmpty(Note.FocusNote.MyScriptName) == false)
                {
                    Note.FocusNote.TimeListener.Clear();
                    Note.FocusNote.UnregisterOnTimeLine();
                    Note.FocusNote.ReloadLineScript();
                    Note.FocusNote = null;
                }
                return;
            }
            if (ASC.CurrentClip == null) return;
            CurrentTick = ASC.CurrentTime;
            "CurrentTick".InsertVariable(CurrentTick);
            float t = CurrentTick, s = CurrentStats;
            foreach (var item in AllControllers)
            {
                try
                {
                    item.TimeUpdate(ref t, ref s);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            float delta = Mathf.Abs(CurrentStats);
            if (delta < Time.deltaTime * this.RawLimitTick)
            {
                this.RawPastTick = this.CurrentTick;
            }
            if (delta < Time.deltaTime * 3 * this.RawLimitTick)
            {
                this.RawPastTick += CurrentStats * 0.5f;
            }
            else
            {
                this.RawPastTick += CurrentStats * 0.1f;
            }
        }

        public Text InfoBarText;

        public void SetInfo(string str)
        {
            InfoBarText.SetText(str);
        }
    }

    [Serializable]
    public class MinnesController : LineBehaviour
    {
        public string MyMinnesID;

        public static IEnumerator WaitForSomeTime(Action action)
        {
            yield return null;
            yield return null;
            yield return null;
            action.Invoke();
        }

        public float FocusTime;
        public void SetFocusTime(float time) => this.FocusTime = time;

        virtual protected void OnEnable()
        {
            this.SetupLBTimeContainer(true);
            StartCoroutine(WaitForSomeTime(ReloadLineScript));
        }

        virtual protected void OnDisable()
        {
            this.SetupLBTimeContainer(false);
            Minnes.MinnesInstance.AllControllers.Remove(this);
        }

        public void RegisterOnTimeLine()
        {
            Minnes.MinnesInstance.AllControllers.Add(this);
        }
        public void UnregisterOnTimeLine()
        {
            Minnes.MinnesInstance.AllControllers.Remove(this);
        }

        public void OnMinnesInspector()
        {
            if (this.MyMinnesID == null || this.MyMinnesID.Length == 0)
            {
                OnMinnesInspectorYouCanntEdit();
            }
            else
            {
                OnMinnesInspectorV();
            }
        }
        protected void OnMinnesInspectorYouCanntEdit() { }
        protected virtual void OnMinnesInspectorV() { } 
    }

    public class MinnesGenerater
    {
        public static Dictionary<string, Func<MinnesController>> GenerateAction = new();

        public MinnesController target;

        public MinnesGenerater(string class_name,string minnesID)
        {
            target = GenerateAction[class_name].Invoke();
            target.MyMinnesID = minnesID;
        }

        public void SetID(string ID)
        {
            target.MyMinnesID = ID;
        }

        public void SetName(string name)
        {
            target.name = name;
        }
    }
}
