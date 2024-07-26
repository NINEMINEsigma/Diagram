using System;
using System.Collections.Generic;
using Diagram.Arithmetic;
using Diagram.Message;
using Unity.VisualScripting;
using UnityEngine;

namespace Diagram
{
    [Serializable]
    public class LineBehaviour : MonoBehaviour
    {
        private RectTransform _rectTransform;
        public RectTransform MyRectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = this.transform as RectTransform;
                return _rectTransform;
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
        public virtual void TimeUpdate(ref float time, ref float stats)
        {
            foreach (var invoker in TimeListener)
            {
                invoker.Value(time, stats);
            }
        }

        public void InitPosition(float x, float y, float z)
        {
            this.transform.position = new Vector3(x, y, z);
        }
        public void InitRotation(float x, float y, float z)
        {
            this.transform.eulerAngles = new Vector3(x, y, z);
        }
        public void InitScale(float x, float y, float z)
        {
            this.transform.localScale = new Vector3(x, y, z);
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

        [SerializeField] protected MeshRenderer meshRenderer;
        [SerializeField] protected MeshFilter meshFilter;
        public MeshRenderer MyRenderer => meshRenderer = meshRenderer != null ? meshRenderer : this.SeekComponent<MeshRenderer>();
        public MeshFilter MyMeshFilter => meshFilter = meshFilter != null ? meshFilter : this.SeekComponent<MeshFilter>();
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
        public Material MyMaterial { get => MyRenderer.material; set => MyRenderer.material = value; }
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

        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogCache(int index)
        {
            Debug.Log(new GetCache(index).message);
        }
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

}
