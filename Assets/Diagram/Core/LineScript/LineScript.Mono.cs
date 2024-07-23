using System;
using System.Collections.Generic;
using Diagram.Arithmetic;
using Diagram.Message;
using UnityEngine;

namespace Diagram
{
    [Serializable]
    public class LineBehaviour : MonoBehaviour
    {
        public string MyScriptName;
        public void SetTargetScript(string path) => MyScriptName = path;

        public void ReloadLineScript()
        {
            if (TimeListener == null) return;
            if (MyScriptName == null || MyScriptName.Length == 0) return;
            LineScript.RunScript(MyScriptName);
        }

        public Dictionary<string, Action<float, float>> TimeListener;
        public virtual void TimeUpdate(float time, float stats)
        {
            foreach (var invoker in TimeListener)
            {
                invoker.Value(time, stats);
            }
        }

        public void MakeDelay(int time, string expression)
        {
            TimeListener.Add($"Delay-{time}-{expression}", (float timex, float statsx) =>
            {
                if (time == timex)
                {
                    new LineScript(("this", this), ("time", timex), ("stats", statsx)).Run(expression);
                }
            });
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
        public void MakeRelativeScale(float startTime, float endTime, float x, float y, float z, int easeType)
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

    public class ResourceLineBehaviourLoader
    {
        public LineBehaviour target;
        public ResourceLineBehaviourLoader(string path)
        {
            target = Resources.Load<LineBehaviour>(path);
        }
    }

}
