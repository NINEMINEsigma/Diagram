Shader "Shaders/ToonBound1"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Main Tex", 2D) = "white" {}
        _Ramp ("Ramp Texture", 2D) = "white" {}                  //控制漫反射色调的渐变纹理
        _Outline ("Outline", Range(0, 1)) = 0.1                  //控制轮廓线宽度
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1) //轮廓线颜色
        _Specular ("Specular", Color) = (1, 1, 1, 1)          //高光反色颜色
        _SpecularScale ("Specular Scale", Range(0, 0.1)) = 0.01 //高光反射系数阈值
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 100

        Pass
        {
            //命名Pass块，以便复用
            NAME "OUTLINE"
            //剔除正面
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
                //让描边在观察空间达到最好的效果
                float4 pos = mul(UNITY_MATRIX_MV, v.vertex); 
                float3 normal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);  
                normal.z = -0.5;
                pos = pos + float4(normalize(normal), 0) * _Outline;
                //将顶点从视角空间变换到裁剪空间
                o.pos = mul(UNITY_MATRIX_P, pos);
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target { 
                //轮廓线颜色渲染整个背面
                return float4(_OutlineColor.rgb, 1);               
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
