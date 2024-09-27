using UnityEngine;

namespace DemoGame.Shader
{
    public class CRLuo_CutPlane : MonoBehaviour
    {
        //ƽ������
        public GameObject planeObj;
        //��������
        public GameObject clipObj;
        //�����������
        private Material clipObjMat;
        //ƽ�淨��
        private Vector3 normal;

        void Start()
        {
            //��ò����������
            clipObjMat = clipObj.GetComponent<MeshRenderer>().material;
        }

        void Update()
        {
            //��ȡƽ�淨��
            normal = -planeObj.transform.forward;
            //ÿһ֡����ƽ������
            clipObjMat.SetVector("_PlanePos", planeObj.transform.position);
            //ÿһ֡�����޸ĺ��ƽ�淨��
            clipObjMat.SetVector("_PlaneNormal", normal);
        }
    }
}