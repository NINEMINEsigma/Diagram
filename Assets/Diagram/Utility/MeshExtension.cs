using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Diagram.Arithmetic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Diagram
{
    public static class MeshExtension
    {
        private class MeshVertexData
        {
            public Vector3 Normal;
            public Vector3 Center;

        }

        private static Stack<MeshVertexData> MeshVertexDataPool = new();
        private static MeshVertexData ObtainMeshVertex()
        {
            if(MeshVertexDataPool.Count > 0)
            {
                var result = MeshVertexDataPool.Peek();
                MeshVertexDataPool.Pop();
                return result;
            }
            return new();
        }
        private static void BackMeshVertex([_In_] MeshVertexData data)
        {
            if (data != null)
                MeshVertexDataPool.Push(data);
        }

        public static Mesh BuildMesh([_In_] Vector3[] vertices, [_In_] int[] triangles, [_In_] Vector2[] uvs)
        {
            Mesh result = new Mesh
            {
                triangles = triangles,
                vertices = vertices,
                uv = uvs
            };
            return result;
        }
        public static Mesh BuildMesh([_In_]List<Vector3> vertices, [_In_]List<int> triangles, [_In_] List<Vector2> uvs)
        {
            return BuildMesh(vertices.ToArray(), triangles.ToArray(), uvs.ToArray());
        }
        public static Mesh BuildMeshFromLine([_In_] List<Vector3> center, [_In_] List<Vector3> rightSide, [_In_] List<float> widthFactor,EaseCurveType InterpolationRules= EaseCurveType.Linear)
        {
            Vector3[] vertices = new Vector3[center.Count * 2]; 
            int[] triangles = new int[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];

            Vector3[] sideLX = new Vector3[center.Count];
            float[] widthFactorLX = new float[center.Count];
            EaseCurve curve = new(InterpolationRules);
            for (int i = 0,e= center.Count; i < e; i++)
            {
                float t = (float)i / (float)e;
                float trs = t * rightSide.Count;
                sideLX[i] = Vector3.Lerp(
                    rightSide[Mathf.FloorToInt(trs)],
                    rightSide[Mathf.CeilToInt(trs)],
                    curve.Evaluate(trs - Mathf.Floor(trs))
                    );
                float twf = t * widthFactor.Count;
                widthFactorLX[i] = 0.5f * Mathf.Lerp(
                    widthFactor[Mathf.FloorToInt(twf)],
                    widthFactor[Mathf.CeilToInt(twf)],
                    curve.Evaluate(twf - Mathf.Floor(twf))
                    );
            }
            for (int i = 0, e = center.Count; i < e; i++)
            {
                vertices[i * 2] = center[i] + sideLX[i] * widthFactorLX[i];
                vertices[i * 2+1] = center[i] - sideLX[i] * widthFactorLX[i];
            }
            return BuildMesh(vertices, triangles, uvs);
        }
    }
}
