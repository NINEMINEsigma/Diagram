using UnityEngine;

namespace DemoGame.Shader
{
    public class CRLuo_CutPlane : MonoBehaviour
    {
        //平面物体
        public GameObject planeObj;
        //裁切物体
        public GameObject clipObj;
        //裁切物体材质
        private Material clipObjMat;
        //平面法线
        private Vector3 normal;

        void Start()
        {
            //获得裁切物体材质
            clipObjMat = clipObj.GetComponent<MeshRenderer>().material;
        }

        void Update()
        {
            //获取平面法线
            normal = -planeObj.transform.forward;
            //每一帧传递平面坐标
            clipObjMat.SetVector("_PlanePos", planeObj.transform.position);
            //每一帧传递修改后的平面法线
            clipObjMat.SetVector("_PlaneNormal", normal);
        }
    }
}