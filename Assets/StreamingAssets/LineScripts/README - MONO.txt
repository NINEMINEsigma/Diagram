namespace Diagram
{ 
    public class LineBehaviour : MonoBehaviour
    {
        private RectTransform _rectTransform;
        public RectTransform MyRectTransform
        {
            get;
        }

        public string MyScriptName;
        public void SetTargetScript(string path);

        public virtual void ReloadLineScript();

        public Dictionary<string, Action<float, float>> TimeListener;
        public virtual void TimeUpdate(ref float time, ref float stats);
        public void InitPosition(float x, float y, float z);
        public void InitRotation(float x, float y, float z);
        public void InitScale(float x, float y, float z);

        public LineBehaviour MakeMovement(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType);
        public LineBehaviour MakeRotating(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType);
        public LineBehaviour MakeScale(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType);
        public LineBehaviour MakeRelativeMovement(float startTime, float endTime, float x, float y, float z, int easeType);
        public LineBehaviour MakeRelativeRotating(float startTime, float endTime, float x, float y, float z, int easeType);
        public LineBehaviour MakeRelativeScale(float startTime, float endTime, float x, float y, float z, int easeType);

        protected MeshRenderer meshRenderer;
        protected MeshFilter meshFilter;
        public MeshRenderer MyRenderer;
        public MeshFilter MyMeshFilter ;
        public Mesh MyMesh { get; set; }
        public void LoadMesh(string package, string name);
        public Material MyMaterial { get; set; }
        public void LoadMaterial(string package, string name);
        public GameObject LoadSubGameObject(string package, string name);
        public void RemoveChild(string name);


        public Component LAddComponent(string type);
        public Component LGetComponent(string type);

        public void Log(string message);
        public void LogCache(int index);
    }

    public class ResourceLineBehaviourLoader
    {
        public LineBehaviour target;
        public ResourceLineBehaviourLoader(string path);
    }

    public class EmptyGameObjectGenerator
    {
        public LineBehaviour target;

        public EmptyGameObjectGenerator();

        public Component AddComponent(string type);
        public Component GetComponent(string type);
    }

}
