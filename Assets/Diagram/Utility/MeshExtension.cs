using System;
using System.Collections;
using System.Collections.Generic;
using Diagram.Arithmetic;
using UnityEngine;

namespace Diagram
{
    public static class MeshExtension
    {
        private class MeshVertexData
        {

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

        public static Mesh BuildMesh([_In_]List<Vector3> vertices, [_In_]List<int> triangles, [_In_] List<Vector2> uvs)
        {
            Mesh result = new Mesh
            {
                triangles = triangles.ToArray(),
                vertices = vertices.ToArray(),
                uv = uvs.ToArray()
            };
            return result;
        }
    }
}
