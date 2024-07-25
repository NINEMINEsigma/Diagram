namespace Game
{
    public class Minnes : LineBehaviour
    {
        public static float PastTime;
        public void LoadScene(string package, string sceneName);
        public void SetSpeed(float speed);
        public void SetDisplayMaxLength(float max);
        public AudioSystem ASC;
        public void LoadSong(string song);
        public void WaitForPlay(float time);
        public void PlaySong();
        public void StopSong();
        public void PauseSong();
        public void PlayOrPauseSong();
        public void SetBPM(float bpm);
        public void SetProject(string projectName);

        public void ReloadScripts();

        public static string ProjectName = "Test";
        public static string ProjectPath => Path.Combine(ToolFile.userPath, ProjectName);
        public static float ProjectBPM = 60;
        public static float ProjectSpeed = 1;
        public static float ProjectNoteDefaultDisplayLength => ProjectNoteMaxDisplayLength / ProjectSpeed;
        public static float ProjectNoteMaxDisplayLength = 3;
        public static Minnes MinnesInstance;

        public class StartRuntimeCommand { }
        public List<MinnesController> EnableContronller;

        private void Awake();
        private void OnDestroy();

        private float RawPastTick;
        private float RawCurrentTick;
        private float RawLimitTick;
        public float CurrentTick;
        public float CurrentStats => get;
        public List<MinnesController> AllControllers ;
        private void Update();
        public Text InfoBarText;
        public void SetInfo(string str);
    }

    public class MinnesController : LineBehaviour
    {
        public string MyMinnesID;
        public static IEnumerator WaitForSomeTime(Action action);
        public float FocusTime;
        public void SetFocusTime(float time);

        virtual protected void OnEnable();
        virtual protected void OnDisable();
        public void RegisterOnTimeLine();
        public void UnregisterOnTimeLine();

        public void OnMinnesInspector();
        protected void OnMinnesInspectorYouCanntEdit();
        protected virtual void OnMinnesInspectorV();
    }

    public class MinnesGenerater
    {
        public static Dictionary<string, Func<MinnesController>> GenerateAction = new();
        public MinnesController target;
        public MinnesGenerater(string class_name,string minnesID);
        public void SetID(string ID);
        public void SetName(string name);
    }
    
    public class MinnesCamera : MinnesController
    {
        private void Start();
        public void OnDependencyCompleting();
        public Camera GetCamera() => this.SeekComponent<Camera>();
        public void SetPerspective() => GetCamera().orthographic = false;
        public void SetOrthographic() => GetCamera().orthographic = true;
        public void SetFieldOfView(float value) => GetCamera().fieldOfView = value;
    }
    
    public class MinnesStatsSharedPanel : LineBehaviour
    {
        public void OnDependencyCompleting();

        private void Start();

        public void SetStats();
        public void SetNoteTypeDefault(bool on);
        public void SetEditTypeCreate(bool on);

        public ModernUIDropdown NoteType;
        public ModernUIDropdown EditType;

        public static ModernUIDropdown StaticNoteType;
        public static ModernUIDropdown StaticEditType;

        public static string NoteTypeName = "Create";
        public static string EditTypeName = "Default";
    }
    
    public class MinnesTimeline : LineBehaviour
    {
        public static MinnesTimeline instance;

        public AudioSystem ASC => Minnes.MinnesInstance.ASC;
        public float CurrnetTime => Minnes.MinnesInstance.CurrentTick;
        public RawImage TimeLineRawImage;
        public float TimeLineDisplayLength = 3;
        public RawImage BarlineRawImage;
        public ModernUIFillBar TimeLineBar;
        public ModernUIButton Stats;


        public void SetTimeDisplayLength(float length);

        public List<Color[]> BarLineColorsList;
        public int BarColorPointer = 0;

        public static Texture2D BakeAudioWaveformBarline(float bpm, float songLength, int width, int height, params Color[] barLineColors);

        public void OnDependencyCompleting();

        private void Start();
        private void Update();
    }
    
    public class IJudgeModule : LineBehaviour
    {

    }

    public class ISoundModule : LineBehaviour
    { 
    
    }

    public class NoteGenerater : MinnesGenerater
    {
        public static void InitNoteGenerater();

        public NoteGenerater() : base("Note","Untag");
    }

    public class Note : MinnesController
    {
        public override void ReloadLineScript();

        public Dictionary<string, IJudgeModule> JudgeModules;
        public Dictionary<string, ISoundModule> SoundModules;
        public void LoadJudgeModule(string package, string name);
        public void LoadSoundModule(string package, string name);
        public float JudgeTime { get; set; }
        public void SetJudgeTime(float judgeTime);
        public void InitNote(float judgeTime, string judge_module_package, string judge_module_name, string sound_module_package, string sound_module_name);

        public static Note FocusNote;
        private Material focusBoundLight;
        public Material FocusBoundLight;
        private void Update();
        private void OnMouseDown();
    }
    
    public class TimeLineTable : MinnesController
    {
        public static TimeLineTable instance;

        public void OnDependencyCompleting();

        public float LeftBound = -6, RightBound = 6;
        public float StartZ = 0, EndZ = 0;
        public float StartY = 0;
        public void SetBound(float left, float right);
        public void SetTrackZ(float start,float end);
        public void SetTrackY(float start);

        public Vector3[] rects;

        public static void SetupNote(Note note,float startTime,float endTime,float x,float y,float z,float x2,float y2,float z2);
        public void OnPointerClick(PointerEventData eventData);
        private void Start();
    }
}
