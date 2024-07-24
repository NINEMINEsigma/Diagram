Shader "Shaders/ToonBound1"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Main Tex", 2D) = "white" {}
        _Ramp ("Ramp Texture", 2D) = "white" {}                  //����������ɫ���Ľ�������
        _Outline ("Outline", Range(0, 1)) = 0.1                  //���������߿��
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1) //��������ɫ
        _Specular ("Specular", Color) = (1, 1, 1, 1)          //�߹ⷴɫ��ɫ
        _SpecularScale ("Specular Scale", Range(0, 0.1)) = 0.01 //�߹ⷴ��ϵ����ֵ
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 100

        Pass
        {
            //����Pass�飬�Ա㸴��
            NAME "OUTLINE"
            //�޳�����
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            float _Outline;
            fixed4 _OutlineColor;

            struct a2v {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            }; 
            
            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (a2v v) {
                v2f o;
                //������ڹ۲�ռ�ﵽ��õ�Ч��
                float4 pos = mul(UNITY_MATRIX_MV, v.vertex); 
                float3 normal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);  
                normal.z = -0.5;
                pos = pos + float4(normalize(normal), 0) * _Outline;
                //��������ӽǿռ�任���ü��ռ�
                o.pos = mul(UNITY_MATRIX_P, pos);
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target { 
                //��������ɫ��Ⱦ��������
                return float4(_OutlineColor.rgb, 1);               
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
