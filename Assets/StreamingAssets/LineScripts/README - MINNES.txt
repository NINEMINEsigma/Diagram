namespace Game
{
    public class Minnes : LineBehaviour
    {
        //��package�İ��м���sceneName����
        public void LoadScene(string package, string sceneName);
        //��������Ԥ������
        public void SetSpeed(float speed);
        //�����������ʱ��(����Ϊ1ʱnote�ӹ��ͷ�����ж�λ�õ���ʱ��)
        public void SetDisplayMaxLength(float max);
        //������Ŀ
        public void LoadSong(string song);
        //��ʼ����������:�ȴ�һ��ʱ������̿�ʼ����
        public void WaitForPlay(float time);
        //����������Ŀ
        public void PlaySong();
        //ֹͣ
        public void StopSong();
        //��ͣ
        public void PauseSong();
        //��ͣ/����
        public void PlayOrPauseSong();
        //����BPM
        public void SetBPM(float bpm);
        //����Ŀ����Ŀ
        public void SetProject(string projectName);
        //���¼���ȫ������ʱ�ű�
        public void ReloadScripts();

        public static float ProjectBPM = 60;
        public static float ProjectSpeed = 1;
        public static float ProjectNoteDefaultDisplayLength => ProjectNoteMaxDisplayLength / ProjectSpeed;
        public static float ProjectNoteMaxDisplayLength = 3;

        //���õײ���Ϣ������Ϣ
        public void SetInfo(string str);
    }

    public class MinnesController : LineBehaviour
    {
        //����һ������ʱ��
        public void SetFocusTime(float time);

        //ע����ʱ����,������ע����������ʱ����ص������޷���Ч
        public void RegisterOnTimeLine();
        //ȡ������ʱ����¼�ע��
        public void UnregisterOnTimeLine();
    }

    public class MinnesGenerater
    {
        //���ɵĶ���
        public MinnesController target;
        //���캯��(����,ID)
        public MinnesGenerater(string class_name,string minnesID);
        //����ID
        public void SetID(string ID);
        //��������
        public void SetName(string name);
    }
    
    public class MinnesCamera : MinnesController
    {
        //��ȡunity camera
        public Camera GetCamera();
        //����Ϊ͸�����
        public void SetPerspective();
        //����Ϊ�������
        public void SetOrthographic();
        //����͸��������ӳ���С
        public void SetFieldOfView(float value);
        //��������������ӳ���С
        public void SetOrthographicSize(float value);
        //���������ƽ��
        public void SetNear(float near) => GetCamera().nearClipPlane = near;
        //�������Զƽ��
        public void SetFar(float far) => GetCamera().farClipPlane = far;
    }

    public class MinnesTimeline : LineBehaviour
    {
        //����ʱ����Ŀ���ʱ�䳤��
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
        //����һ��Note,��target��ȡ
        public NoteGenerater() : base("Note","Untag");
    }

    public class Note : MinnesController
    {
        //��package����name���Ƶ��ж�ģ��
        public IJudgeModule LoadJudgeModule(string package, string name);
        //��package����name���Ƶ�����ģ��
        public ISoundModule LoadSoundModule(string package, string name);
        //�����ж�ʱ��(ʹ��FocusTime)
        public void SetJudgeTime(float judgeTime);
        //������������������ʼ��note
        public void InitNote(float judgeTime, string judge_module_package, string judge_module_name, string sound_module_package, string sound_module_name);
        
        //��ȡ����ģ��
        public ISoundModule GetSoundModule(string name);
        //�趨��ĳʱ���Ÿ�ģ�������һ��
        public Note MakeSoundPlay(float startTime, string name);
        //��ȡ�ж�ģ��
        public IJudgeModule GetJudgeModule(string name);
        //�趨��ĳʱ���Ÿ�ģ���Ч��һ��
        public Note MakeJudgeEffectPlay(float startTime,string name);
    }
    
    public class TimeLineTable : MinnesController
    {
        public float LeftBound = -6, RightBound = 6;
        public float StartZ = 0, EndZ = 0;
        public float StartY = 0;
        //���õ�ǰ�༭���ҹ���߽�
        public void SetBound(float left, float right);
        //���õ�ǰ�༭���ǰ��߽�
        public void SetTrackZ(float start,float end);
        //���õ�ǰ�༭����߶�
        public void SetTrackY(float start);
        
        //����Ĭ�ϵ�VirtualNote.ls�ļ�
        public void GenerateVirtualNoteLS();
        //����.virtual�µ�note
        public void ReloadVirtualFolderNotes()
    }
}
