using System;
using System.Collections.Generic;
using Diagram.Arithmetic;
using Diagram.Message;
using UnityEngine;

namespace Diagram
{
    [Serializable]
    public partial class LineBehaviour : MonoBehaviour
    {
        [SerializeField] protected RectTransform m_rectTransform;
        public RectTransform MyRectTransform
        {
            get
            {
                if (m_rectTransform == null)
                    m_rectTransform = this.transform as RectTransform;
                return m_rectTransform;
            }
        }

        public string MyScriptName;
        public void SetTargetScript(string path) => MyScriptName = path;

        public virtual void ReloadLineScript()
        {
            if (TimeListener == null) return;
            if (MyScriptName == null || MyScriptName.Length == 0) return;
            LineScript.RunScript(MyScriptName);
        }

        public Dictionary<string, Action<float, float>> TimeListener;
        public Dictionary<string, bool> TimeInvokerPointer;
        public void SetupLBTimeContainer(bool isEnable)
        {
            if (isEnable)
            {
                SetupLBTimeContainerEnable();
            }
            else
            {
                SetupLBTimeContainerDisable();
            }
        }
        public void SetupLBTimeContainerEnable()
        {
            TimeListener ??= new();
            TimeInvokerPointer ??= new();
        }
        public void SetupLBTimeContainerDisable()
        {
            TimeListener = null;
            TimeInvokerPointer = null;
        }

        public virtual void TimeUpdate(ref float time, ref float stats)
        {
            foreach (var invoker in TimeListener)
            {
                invoker.Value(time, stats);
            }
        }

        public LineBehaviour MakeValueCurve(string name, float startTime, float endTime, float startValue, float endValue, int easeType)
        {
            if (startTime == endTime) return this;
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            DiagramMember member = DiagramType.GetOrCreateDiagramType(this.GetType()).GetMember(name);
            TimeListener.TryAdd($"{nameof(MakeValueCurve)}-{name}-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                member.reflectedMember.SetValue(this, startValue + (endValue - startValue) * eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
            return this;
        }
        public LineBehaviour MakeTimeInvoker(float targetTime, string ls)
        {
            string symbol = ls + "-" + targetTime.ToString();
            if (TimeListener.TryAdd(symbol, (float time, float stats) =>
            {
                if (time > targetTime && TimeInvokerPointer[symbol] == false)
                {
                    new LineScript(("this", this)).Run(ls);
                    TimeInvokerPointer[symbol] = true;
                }
                else if (time < targetTime)
                    TimeInvokerPointer[symbol] = false;
            }))
            {
                TimeInvokerPointer[symbol] = false;
            }
            return this;
        }

        public void InitPosition(float x, float y, float z)
        {
            this.transform.position = new Vector3(x, y, z);
            TimeListener.TryAdd($"{nameof(InitPosition)}", (float time, float stats) =>
            {
                if (Mathf.Approximately(time, 0) == false) return;
                this.transform.position = new Vector3(x, y, z);
            });
        }
        public void InitRotation(float x, float y, float z)
        {
            this.transform.eulerAngles = new Vector3(x, y, z);
            TimeListener.TryAdd($"{nameof(InitRotation)}", (float time, float stats) =>
            {
                if (Mathf.Approximately(time, 0) == false) return;
                this.transform.eulerAngles = new Vector3(x, y, z);
            });
        }
        public void InitScale(float x, float y, float z)
        {
            this.transform.localScale = new Vector3(x, y, z);
            TimeListener.TryAdd($"{nameof(InitScale)}", (float time, float stats) =>
            {
                if (Mathf.Approximately(time, 0) == false) return;
                this.transform.localScale = new Vector3(x, y, z);
            });
        }

        public LineBehaviour MakeMovement(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType)
        {
            if (startTime == endTime) return this;
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(x, y, z), to = new(x2, y2, z2);
            TimeListener.TryAdd($"Movement-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.position = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
            return this;
        }
        public LineBehaviour MakeRotating(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType)
        {
            if (startTime == endTime) return this;
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(x, y, z), to = new(x2, y2, z2);
            TimeListener.TryAdd($"Rotating-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.eulerAngles = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
            return this;
        }
        public LineBehaviour MakeScale(float startTime, float endTime, float x, float y, float z, float x2, float y2, float z2, int easeType)
        {
            if (startTime == endTime) return this;
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(x, y, z), to = new(x2, y2, z2);
            TimeListener.TryAdd($"ScaleTransform-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.localScale = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
            return this;
        }
        public LineBehaviour MakeRelativeMovement(float startTime, float endTime, float x, float y, float z, int easeType)
        {
            if (startTime == endTime) return this;
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(transform.position.x, transform.position.y, transform.position.z);
            Vector3 to = new Vector3(x, y, z) + from;
            TimeListener.TryAdd($"RelativeMovement-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.position = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
            return this;
        }
        public LineBehaviour MakeRelativeRotating(float startTime, float endTime, float x, float y, float z, int easeType)
        {
            if (startTime == endTime) return this;
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
            Vector3 to = new Vector3(x, y, z) + from;
            TimeListener.TryAdd($"RelativeRotating-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.rotation = Quaternion.Lerp(Quaternion.Euler(from), Quaternion.Euler(to), eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
            return this;
        }
        public LineBehaviour MakeRelativeScale(float startTime, float endTime, float x, float y, float z, int easeType)
        {
            if (startTime == endTime) return this;
            var eCurve = new EaseCurve((EaseCurveType)easeType);
            Vector3 from = new(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            Vector3 to = new Vector3(x, y, z) + from;
            TimeListener.TryAdd($"RelativeScaleTransform-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                this.transform.localScale = Vector3.Lerp(from, to, eCurve.Evaluate((float)(time - startTime) / (float)(endTime - startTime)));
            });
            return this;
        }

        [SerializeField] protected MeshRenderer m_meshRenderer;
        [SerializeField] protected MeshFilter m_meshFilter;
        public MeshRenderer MyMeshRenderer
        {
            get
            {
                if (m_meshRenderer == null)
                    m_meshRenderer = this.SeekComponent<MeshRenderer>();
                return m_meshRenderer;
            }
        }
        public MeshFilter MyMeshFilter
        {
            get
            {
                if (m_meshFilter == null)
                    m_meshFilter = this.SeekComponent<MeshFilter>();
                return m_meshFilter;
            }
        }
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
        public Material MyMaterial { get => MyMeshRenderer.material; set => MyMeshRenderer.material = value; }
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
                file.LoadAssetBundle().LoadAsset<GameObject>(name).PrefabInstantiate().Share(out var obj).transform.SetParent(transform, false);
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

        [SerializeField] private Animator m_animator;
        public Animator MyAnimator
        {
            get
            {
                if (m_animator == null)
                    m_animator = this.SeekComponent<Animator>();
                return m_animator;
            }
        }
        public void PlayAnimaton(string name)
        {
            MyAnimator.Play(name);
        }

        public Component LAddComponent(string type)
        {
            Type componentType = ReflectionExtension.Typen(type);
            if (componentType == null) return null;
            return gameObject.AddComponent(componentType);
        }

        public Component LGetComponent(string type)
        {
            Type componentType = ReflectionExtension.Typen(type);
            if (componentType == null) return null;
            return gameObject.GetComponent(componentType);
        }

        public void SetActive(bool stats)
        {
            this.gameObject.SetActive(stats);
        }

        public void LetActive(GameObject right, bool stats)
        {
            right.SetActive(stats);
        }

        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogCache(int index)
        {
            Debug.Log(new GetCache(index).message);
        }

        public virtual void Reset() { }
    }

    public class ResourceLineBehaviourLoader
    {
        public LineBehaviour target;
        public ResourceLineBehaviourLoader(string path)
        {
            target = Resources.Load<LineBehaviour>(path);
        }
    }

    public class EmptyGameObjectGenerator
    {
        public LineBehaviour target;

        public EmptyGameObjectGenerator()
        {
            target = new GameObject().AddComponent<LineBehaviour>();
            target.name = "EmptyLineMono";
        }

        public Component AddComponent(string type)
        {
            Type componentType = ReflectionExtension.Typen(type);
            if (componentType == null) return null;
            return target.gameObject.AddComponent(componentType);
        }

        public Component GetComponent(string type)
        {
            Type componentType = ReflectionExtension.Typen(type);
            if (componentType == null) return null;
            return target.gameObject.GetComponent(componentType);
        }
    }

    [Serializable]
    public class ModuleBehaviour : LineBehaviour
    {
        [SerializeField] private BehaviourModuleAssets[] contexts;
        public BehaviourModuleAssets[] Contexts
        {
            get
            {
                contexts ??= new BehaviourModuleAssets[0];
                return contexts;
            }
        }

        private void Awake()
        {
            foreach (var module in Contexts)
            {
                module.targetObject = this;
            }
            foreach (var module in Contexts)
            {
                if (module.IsEnable)
                    module.ModuleAwake();
            }
            this.LAwake();
        }
        protected virtual void LAwake() { }

        private void Start()
        {
            foreach (var module in Contexts)
            {
                if (module.IsEnable)
                    module.ModuleStart();
            }
            this.LStart();
        }
        protected virtual void LStart() { }

        private void Update()
        {
            foreach (var module in Contexts)
            {
                if (module.IsEnable)
                    module.ModuleUpdate();
            }
            this.LUpdate();
        }
        protected virtual void LUpdate() { }

        private void LateUpdate()
        {
            foreach (var module in Contexts)
            {
                if (module.IsEnable)
                    module.ModuleLateUpdate();
            }
            this.LLateUpdate();
        }
        protected virtual void LLateUpdate() { }

        private void FixedUpdate()
        {
            foreach (var module in Contexts)
            {
                if (module.IsEnable)
                    module.ModuleFixedUpdate();
            }
            this.LFixedUpdate();
        }
        protected virtual void LFixedUpdate() { }

        private void OnEnable()
        {
            foreach (var module in Contexts)
            {
                if (module.IsEnable)
                    module.ModuleOnEnable();
            }
            this.LonEnable();
        }
        protected virtual void LonEnable() { }

        private void OnDisable()
        {
            foreach (var module in Contexts)
            {
                if (module.IsEnable)
                    module.ModuleOnDisable();
            }
            this.LonDisable();
        }
        protected virtual void LonDisable() { }
    }

    public class BehaviourModuleAssets : ScriptableObject
    {
        [HideInInspector] public ModuleBehaviour targetObject;
        public bool IsEnable;

        public virtual void ModuleAwake()
        {

        }

        public virtual void ModuleStart()
        {

        }

        public virtual void ModuleUpdate()
        {

        }

        public virtual void ModuleLateUpdate()
        {

        }

        public virtual void ModuleFixedUpdate()
        {

        }

        public virtual void ModuleOnEnable()
        {
        }

        public virtual void ModuleOnDisable()
        {

        }
    }

    public class ABObject
    {
        public UnityEngine.Object target;

        public ABObject(string package, string name)
        {
            using ToolFile file = new(package, false, true, false);
            var ab = file.LoadAssetBundle(true);
            target = ab.LoadAsset(name);
        }
    }
}
