namespace Game
{
    public class Minnes : LineBehaviour
    {
        //从package的包中加载sceneName场景
        public void LoadScene(string package, string sceneName);
        //设置谱面预设流速
        public void SetSpeed(float speed);
        //设置最大下落时长(流速为1时note从轨道头落入判定位置的总时长)
        public void SetDisplayMaxLength(float max);
        //加载曲目
        public void LoadSong(string song);
        //初始化制谱器后:等待一段时间后立刻开始播放
        public void WaitForPlay(float time);
        //立即播放曲目
        public void PlaySong();
        //停止
        public void StopSong();
        //暂停
        public void PauseSong();
        //暂停/播放
        public void PlayOrPauseSong();
        //设置BPM
        public void SetBPM(float bpm);
        //设置目标项目
        public void SetProject(string projectName);
        //重新加载全部运行时脚本
        public void ReloadScripts();

        public static float ProjectBPM = 60;
        public static float ProjectSpeed = 1;
        public static float ProjectNoteDefaultDisplayLength => ProjectNoteMaxDisplayLength / ProjectSpeed;
        public static float ProjectNoteMaxDisplayLength = 3;

        //设置底部信息栏的信息
        public void SetInfo(string str);
    }

    public class MinnesController : LineBehaviour
    {
        //设置一个焦点时间
        public void SetFocusTime(float time);

        //注册于时间轴,不进行注册则所有与时间相关的设置无法生效
        public void RegisterOnTimeLine();
        //取消基于时间的事件注册
        public void UnregisterOnTimeLine();
    }

    public class MinnesGenerater
    {
        //生成的对象
        public MinnesController target;
        //构造函数(类名,ID)
        public MinnesGenerater(string class_name,string minnesID);
        //设置ID
        public void SetID(string ID);
        //设置名称
        public void SetName(string name);
    }
    
    public class MinnesCamera : MinnesController
    {
        //获取unity camera
        public Camera GetCamera();
        //设置为透视相机
        public void SetPerspective();
        //设置为正交相机
        public void SetOrthographic();
        //设置透视相机的视场大小
        public void SetFieldOfView(float value);
        //设置正交相机的视场大小
        public void SetOrthographicSize(float value);
        //设置相机近平面
        public void SetNear(float near) => GetCamera().nearClipPlane = near;
        //设置相机远平面
        public void SetFar(float far) => GetCamera().farClipPlane = far;
    }

    public class MinnesTimeline : LineBehaviour
    {
        //设置时间轴的可视时间长度
        public void SetTimeDisplayLength(float length);
    }
    
    public class IJudgeModule : LineBehaviour
    {

    }

    public class ISoundModule : LineBehaviour
    { 
    
    }

    public class NoteGenerater : MinnesGenerater
    {
        //构造一个Note,从target获取
        public NoteGenerater() : base("Note","Untag");
    }

    public class Note : MinnesController
    {
        //从package加载name名称的判定模块
        public IJudgeModule LoadJudgeModule(string package, string name);
        //从package加载name名称的声音模块
        public ISoundModule LoadSoundModule(string package, string name);
        //设置判定时间(使用FocusTime)
        public void SetJudgeTime(float judgeTime);
        //调用以上三个函数初始化note
        public void InitNote(float judgeTime, string judge_module_package, string judge_module_name, string sound_module_package, string sound_module_name);
        
        //获取声音模块
        public ISoundModule GetSoundModule(string name);
        //设定在某时播放该模块的声音一次
        public Note MakeSoundPlay(float startTime, string name);
        //获取判定模块
        public IJudgeModule GetJudgeModule(string name);
        //设定在某时播放该模块的效果一次
        public Note MakeJudgeEffectPlay(float startTime,string name);
    }
    
    public class TimeLineTable : MinnesController
    {
        public float LeftBound = -6, RightBound = 6;
        public float StartZ = 0, EndZ = 0;
        public float StartY = 0;
        //设置当前编辑左右轨道边界
        public void SetBound(float left, float right);
        //设置当前编辑轨道前后边界
        public void SetTrackZ(float start,float end);
        //设置当前编辑轨道高度
        public void SetTrackY(float start);
        
        //生成默认的VirtualNote.ls文件
        public void GenerateVirtualNoteLS();
        //加载.virtual下的note
        public void ReloadVirtualFolderNotes()
    }
}
