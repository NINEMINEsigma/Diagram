using System;
using System.Collections.Generic;
using System.IO;
using AD.UI;
using AD.Utility;
using Diagram;
using Diagram.Arithmetic;
using Diagram.Message;
using UnityEngine;

namespace Game
{
    public class Minnes : MonoBehaviour
    {
        public void SetSpeed(float speed) => ProjectSpeed = speed;
        public AudioSourceController ASC;
        public void LoadSong(string song)
        {
            ASC.LoadOnUrl(song, AudioSourceController.GetAudioType(song), true);
        }
        public void SetBPM(float bpm) => ProjectBPM = bpm;
        public void SetProject(string projectName) => ProjectName = projectName;
        public void Log(string message)
        {
            Debug.Log(message);
        }
        public void LogCache(int index)
        {
            Debug.Log(new GetCache(index).message);
        }

        public static string ProjectName = "Test";
        public static float ProjectBPM = 60;
        public static float ProjectSpeed = 1;
        public static Minnes MinnesInstance;

        public class StartRuntimeCommand { }
        public List<MinnesController> EnableContronller = new();

        private void Awake()
        {
            ArchitectureDiagram.RegisterArchitecture(this);
            MinnesInstance = this;
        }
        private void Start()
        {
            this.Architecture<Minnes>().Register<StartRuntimeCommand>(new BaseWrapper.Model(new StartRuntimeCommand()));
            {
                using ToolFile file = new(Path.Combine(ToolFile.userPath, "Config.ls"), true, true, false);
                new LineScript(("this", this)).Run(file.GetString(false, System.Text.Encoding.UTF8));
            }
            {
                using ToolFile projectFile = new(Path.Combine(ToolFile.userPath, ProjectName, "Minnes.ls"), true, true, false);
                new LineScript(("this", this)).Run(projectFile.GetString(false, System.Text.Encoding.UTF8));
            }
            foreach (var item in EnableContronller)
            {
                item.gameObject.SetActive(true);
            }
        }
        private void OnDestroy()
        {
            ArchitectureDiagram.UnregisterArchitecture<Minnes>();
        }

        /*
        [SerializeField] private int RawPastTick = 0;
        [SerializeField] private int RawCurrentTick = 0;
        public int CurrentTick
        {
            get => RawPastTick;
            set
            {
                if (RawCurrentTick != value)
                {
                    RawPastTick = RawCurrentTick;
                    RawCurrentTick = value;
                }
            }
        }
        public int CurrentStats => RawCurrentTick - RawPastTick;
        public List<MinnesController> AllControllers = new();
        private void Update()
        {
            //if (!IsDirty) return;  
            if (ASC.CurrentClip == null) return;
            CurrentTick = (int)(ASC.CurrentTime * (60 / ProjectBPM) * 64);
            "CurrentTick".InsertVariable(CurrentTick);
            while (RawPastTick != RawCurrentTick)
            {
                foreach (var item in AllControllers)
                {
                    item.TimeUpdate(CurrentTick, CurrentStats);
                }
                RawPastTick += RawCurrentTick > RawPastTick ? 1 : -1;
            }
        }*/

        [SerializeField] private float RawPastTick = 0;
        [SerializeField] private float RawCurrentTick = 0;
        public float CurrentTick
        {
            get => RawPastTick;
            set
            {
                if (RawCurrentTick != value)
                {
                    RawPastTick = RawCurrentTick;
                    RawCurrentTick = value;
                }
            }
        }
        public float CurrentStats => RawCurrentTick - RawPastTick;
        public List<MinnesController> AllControllers = new();
        private void Update()
        {
            if (ASC.CurrentClip == null) return;
            CurrentTick = ASC.CurrentTime;
            "CurrentTick".InsertVariable(CurrentTick);
            foreach (var item in AllControllers)
            {
                item.TimeUpdate(CurrentTick, CurrentStats);
            }
            RawPastTick = RawCurrentTick;
        }
    }

    [Serializable]
    public class MinnesController:MonoBehaviour
    {
        public string MyScriptName;
        public float FocusTime;
        public void SetTargetScript(string path) => MyScriptName = path;
        public void SetFocusTime(float time) => this.FocusTime = time;

        public void ReloadLineScript()
        {
            if (MyScriptName.Length == 0) return;
            LineScript core = new(("this", this));
            using ToolFile file = new(Path.Combine(ToolFile.userPath, Minnes.ProjectName, MyScriptName), true, true, false);
            core.Run(file.GetString(false, System.Text.Encoding.UTF8));
        }

        virtual protected void OnEnable()
        {
            ReloadLineScript();
        }

        virtual protected void OnDisable()
        {
            TimeListener.Clear();
        }

        public Dictionary<string, Action<float, float>> TimeListener = new();
        public virtual void TimeUpdate(float time, float stats)
        {
            foreach (var invoker in TimeListener)
            {
                invoker.Value(time, stats);
            }
        }

        public void MakeDelay(int time,string expression)
        {
            TimeListener.Add($"Delay-{time}-{expression}", (float timex, float statsx) =>
            {
                if(time==timex)
                {
                    new LineScript(("this", this), ("time", timex), ("stats", statsx)).Run(expression);
                }
            });
        }
        public void InitPosition(float x,float y,float z)
        {
            this.transform.position = new Vector3(x, y, z);
        }
        public void InitRotation(float x,float y,float z)
        {
            this.transform.eulerAngles = new Vector3(x,y,z);
        }
        public void InitScale(float x,float y,float z)
        {
            this.transform.localScale = new Vector3(x,y,z);
        }

        public void MakeMovement(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType)
        {
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(x, y, z), to = new(x2, y2, z2);
            TimeListener.TryAdd($"Movement-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.position = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
        }
        public void MakeRotating(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType)
        {
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(x, y, z), to = new(x2, y2, z2);
            TimeListener.TryAdd($"Rotating-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.eulerAngles = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
        }
        public void MakeScale(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType)
        {
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(x, y, z), to = new(x2, y2, z2);
            TimeListener.TryAdd($"ScaleTransform-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.localScale = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogCache(int index)
        {
            Debug.Log(new GetCache(index).message);
        }
    }

    public class MinnesGenerater
    {
        public static Dictionary<string, Func<MinnesController>> GenerateAction = new();

        public MinnesController target;

        public MinnesGenerater(string class_name)
        {
            target = GenerateAction[class_name].Invoke();
        }
    }
}
