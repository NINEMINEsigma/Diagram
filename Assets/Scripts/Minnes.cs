using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AD;
using AD.UI;
using AD.Utility;
using Diagram;
using Diagram.Arithmetic;
using Diagram.Message;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Game
{
    public class Minnes : MonoBehaviour
    {
        public void LoadScene(string package, string sceneName)
        {
            using ToolFile file = new(package, false, true, false);
            var ab = file.LoadAssetBundle();
            sceneName.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive);//.MarkCompleted
        }
        public void SetSpeed(float speed) => ProjectSpeed = speed;
        public AudioSourceController ASC;
        public void LoadSong(string song)
        {
            ASC.LoadOnUrl(song, AudioSourceController.GetAudioType(song), true);
        }
        public void WaitForPlay(float time)
        {
            StartCoroutine(IWaitForPlay());
            IEnumerator IWaitForPlay()
            {
                yield return new WaitForSeconds(time);
                while (ASC.CurrentClip == null) yield return null;
                ASC.Play();
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
        public void Log(string message)
        {
            Debug.Log(message);
        }
        public void LogCache(int index)
        {
            Debug.Log(new GetCache(index).message);
        }

        public void ReloadScripts()
        {
            ADGlobalSystem.instance.OnEnd();
        }

        public static string ProjectName = "Test";
        public static string ProjectPath = Path.Combine(ToolFile.userPath, ProjectName);
        public static float ProjectBPM = 60;
        public static float ProjectSpeed = 1;
        public static Minnes MinnesInstance;

        public class StartRuntimeCommand { }
        public List<MinnesController> EnableContronller = new();

        private void Awake()
        {
            ArchitectureDiagram.RegisterArchitecture(this);
            MinnesInstance = this;
            Game.NoteGenerater.InitNoteGenerater();
            {
                using ToolFile file = new(Path.Combine(ToolFile.userPath, "MinnesConfig.ls"), true, true, false);
                new LineScript(("this", this)).Run(file.GetString(false, System.Text.Encoding.UTF8));
            }
            {
                LineScript.RunScript("Minnes.ls", ("this", this)).ReadAndRun(ProjectName + ".ls");
            }
            foreach (var item in EnableContronller)
            {
                item.gameObject.SetActive(true);
            }
            this.Architecture<Minnes>().Register<StartRuntimeCommand>(new BaseWrapper.Model(new StartRuntimeCommand()));
        }
        private void OnDestroy()
        {
            ArchitectureDiagram.UnregisterArchitecture<Minnes>();
            MinnesGenerater.GenerateAction.Clear();
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
        [SerializeField] private float RawLimitTick = 3;
        public float CurrentTick
        {
            get => RawPastTick;
            set
            {
                if (RawCurrentTick != value)
                {
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
            RawPastTick =
                Mathf.Abs(RawCurrentTick - RawPastTick) > Time.deltaTime * RawLimitTick 
                ? (RawCurrentTick - RawPastTick > 0 
                    ? RawPastTick + Time.deltaTime * RawLimitTick 
                    : RawPastTick - Time.deltaTime * RawLimitTick )
                : RawCurrentTick;
        }
    }

    [Serializable]
    public class MinnesController:MonoBehaviour
    {
        public static IEnumerator WaitForSomeTime(Action action)
        {
            yield return null;
            yield return null;
            yield return null;
            action.Invoke();
        }

        public string MyScriptName;
        public float FocusTime;
        public void SetTargetScript(string path) => MyScriptName = path;
        public void SetFocusTime(float time) => this.FocusTime = time;

        public void ReloadLineScript()
        {
            if (TimeListener == null) return;
            if (MyScriptName == null || MyScriptName.Length == 0) return;
            LineScript.RunScript(MyScriptName);
        }

        virtual protected void OnEnable()
        {
            TimeListener ??= new();
            StartCoroutine(WaitForSomeTime(ReloadLineScript));
        }

        virtual protected void OnDisable()
        {
            TimeListener = null;
            Minnes.MinnesInstance.AllControllers.Remove(this);
        }

#if UNITY_EDITOR
        public int TimeListenerCounter;
#endif
        public Dictionary<string, Action<float, float>> TimeListener;
        public virtual void TimeUpdate(float time, float stats)
        {
#if UNITY_EDITOR
            TimeListenerCounter = TimeListener.Count;
#endif
            foreach (var invoker in TimeListener)
            {
                invoker.Value(time, stats);
            }
        }
        public void RegisterOnTimeLine()
        {
            Minnes.MinnesInstance.AllControllers.Add(this);
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
        public void MakeRelativeMovement(float startTime, float endTime, float x, float y, float z, int easeType)
        {
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(transform.position.x, transform.position.y, transform.position.z);
            Vector3 to = new Vector3(x, y, z) + from;
            TimeListener.TryAdd($"RelativeMovement-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.position = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
        }
        public void MakeRelativeRotating(float startTime, float endTime, float x, float y, float z, int easeType)
        {
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
            Vector3 to = new Vector3(x, y, z) + from;
            TimeListener.TryAdd($"RelativeRotating-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.rotation = Quaternion.Lerp(Quaternion.Euler(from), Quaternion.Euler(to), eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
        }
        public void MakeRelativeScale(float startTime, float endTime, float x, float y, float z,int easeType)
        {
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            Vector3 to = new Vector3(x, y, z) + from;
            TimeListener.TryAdd($"RelativeScaleTransform-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.localScale = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
        }

        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public MeshRenderer MyRenderer => meshRenderer ??= this.SeekComponent<MeshRenderer>();
        public MeshFilter MyMeshFilter => meshFilter ??= this.SeekComponent<MeshFilter>();
        public Mesh MyMesh { get => MyMeshFilter.mesh; set => MyMeshFilter.mesh = value; }
        public void LoadMesh(string package, string name)
        {
            if (package == "None" || name == "None") return;
            using ToolFile file = new(package, false, true, true);
            if (file)
                this.MyMesh = file.LoadAssetBundle().LoadAsset<Mesh>(name);
            else
                Debug.LogError(file.FilePath + " is not exist");
        }
        public Material MyMaterial { get => MyRenderer.sharedMaterial; set => MyRenderer.sharedMaterial = value; }
        public void LoadMaterial(string package, string name)
        {
            if (package == "None" || name == "None") return;
            using ToolFile file = new(package, false, true, true);
            if (file)
                this.MyMaterial = file.LoadAssetBundle().LoadAsset<Material>(name);
            else
                Debug.LogError(file.FilePath + " is not exist");
        }
        public GameObject LoadSubGameObject(string package, string name)
        {
            if (package == "None" || name == "None") return null;
            using ToolFile file = new(package, false, true, true);
            if (file)
            {
                file.LoadAssetBundle().LoadAsset<GameObject>(name).Share(out var obj).transform.SetParent(transform, false);
                obj.name = name;
                return obj;
            }
            else
                Debug.LogError(file.FilePath + " is not exist");
            return null;
        }
        public void RemoveChild(string name)
        {
            GameObject.Destroy(this.transform.Find(name));
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
