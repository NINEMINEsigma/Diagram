using System.Collections.Generic;
using AD.Utility;
using Diagram;
using UnityEngine;

namespace Game
{
    public class LongNoteBodyGenerater : MinnesGenerater
    {
        public static void InitNoteGenerater()
        {
            MinnesGenerater.GenerateAction.Add("LongNoteBody", () => Resources.Load<LongNoteBody>("LongNoteBody").PrefabInstantiate());
        }

        public LongNoteBodyGenerater() : base("LongNoteBody", "Untag")
        {
            target.name = "LongNoteBody";
            target.TimeListener = new();
            target.As<LongNoteBody>().MyMesh = new();
        }
    }

    public class LongNoteBody : MinnesController
    {
        [SerializeField] protected MeshExtension.VertexEntry[] MeshSourcePairs;
        public List<Note> Pointers = new();
        public AnimationCurve BodySizeCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public LongNoteBody AddNoteGenerater(NoteGenerater generater)
        {
            AddNote(generater.target as Note);
            return this;
        }

        public LongNoteBody AddNote(Note note)
        {
            Pointers.Add(note);
            return this;
        }

        public LongNoteBody SetBodySize(float time, float size)
        {
            BodySizeCurve.AddKey(time, size);
            return this;
        }

        public LongNoteBody MakeRebuildInterval(float startTime, float endTime)
        {
            TimeListener.TryAdd($"RebuildInterval-{startTime}-{endTime}", (float time, float stats) =>
            {
                if (time < startTime || time > endTime) return;
                if (this.Pointers.Count >= 2)
                {
                    AD.Utility.CustomCurveSourceLinner linkingCurve = new();
                    linkingCurve.AllPoints = new();
                    //Vector3 InitPos = Pointers[0].transform.position;
                    for (int i = 0; i < this.Pointers.Count - 1; i++)
                    {
                        //Vector3 start = Pointers[i].transform.position - InitPos, end = Pointers[i + 1].transform.position - InitPos;
                        Vector3 start = this.Pointers[i].transform.position, end = this.Pointers[i + 1].transform.position;
                        linkingCurve.AllPoints.Add(new(start, true));
                        Vector3 dirt = (end - start).normalized;
                        linkingCurve.AllPoints.Add(new(start.AddZ(dirt.z), false));
                        linkingCurve.AllPoints.Add(new(end.AddZ(-dirt.z), false));
                    }
                    linkingCurve.AllPoints.Add(new(this.Pointers[^1].transform.position, true));
                    this.MeshSourcePairs = linkingCurve.GenerateCurveMeshData(MeshExtension.BuildNormalType.JustDirection, Vector3.right, this.BodySizeCurve);
                    this.MyMeshFilter.RebuildMesh(this.MeshSourcePairs);
                }
                else
                {
                    this.MyMesh = null;
                }
            });
            return this;
        }

    }
}
