using System.Collections.Generic;
using Diagram;
using UnityEngine;

namespace Game
{
    public interface IJudgeModule
    {

    }

    public class NoteGenerater : MinnesGenerater
    {
        public static void InitNoteGenerater()
        {
            MinnesGenerater.GenerateAction.Add("Note", () =>
            {
                return new GameObject().AddComponent<Note>();
            });
        }

        public NoteGenerater() : base("Note")
        {
            this.target = Resources.Load<Note>("NoteBase0");
        }
    }

    public class Note : MinnesController
    {
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public MeshRenderer MyRenderer => meshRenderer ??= this.SeekComponent<MeshRenderer>();
        public MeshFilter MyMeshFilter => meshFilter ??= this.SeekComponent<MeshFilter>();
        public Mesh MyMesh { get => MyMeshFilter.mesh; set => MyMeshFilter.mesh = value; }
        public void LoadMesh(string package,string name)
        {
            using ToolFile file = new(package, false, true, true);
            this.MyMesh = file.LoadAssetBundle().LoadAsset<Mesh>(name);
        } 
        public Material MyMaterial { get => MyRenderer.sharedMaterial;set => MyRenderer.sharedMaterial = value; }
        public void LoadMaterial(string package,string name)
        {
            using ToolFile file = new(package, false, true, true);
            this.MyMaterial = file.LoadAssetBundle().LoadAsset<Material>(name);
        }
        public GameObject LoadSubGameObject(string package,string name)
        {
            using ToolFile file = new(package, false, true, true);
            file.LoadAssetBundle().LoadAsset<GameObject>(name).Share(out var obj).transform.SetParent(transform, false);
            obj.name = name;
            return obj;
        }
        public Dictionary<string, IJudgeModule> JudgeModules = new();
        public void LoadJudgeModule(string package,string name)
        {
            if (LoadSubGameObject(package, name).SeekComponent<IJudgeModule>().Share(out var module) != null)
                JudgeModules.Add(name, module);
        }
        public float JudgeTime => this.FocusTime;
        public void InitNote(float judgeTime,string judge_module_package,string judge_module_name)
        {
            SetFocusTime(judgeTime);
            LoadJudgeModule(judge_module_package,judge_module_name);
        }
        public void RemoveChild(string name)
        {
            GameObject.Destroy(this.transform.Find(name));
        }
    }
}
