Shader "CRLuo/CRLuo_PlaneCut" 
{

	Properties
	{
		_Color("物体颜色", Color) = (1.0, 1.0, 1.0, 1.0)
		_PlanePos("平面坐标", Vector) = (0, 0, 0, 1)
		_PlaneNormal("平面法线", Vector) = (0, 0, 1, 0)
	}


	//公共函数区
	CGINCLUDE

	float4 _PlanePos;
	float4 _PlaneNormal;

	//任意一点到平面的距离（世界任意点）
	float PosToPlaneDistance(float3 WorldPos) 
	{
		float3 planeNormal = _PlaneNormal.xyz;
		float3 planePos = _PlanePos.xyz;

        float Distance = dot(WorldPos - planePos,planeNormal);

		return Distance;
	}
	ENDCG

	//Shader块
	SubShader
	{
	//渲染执行条件
		Tags{ "Queue" = "Geometry" }


		Pass{
			//正面渲染Pass
			Cull Back

			CGPROGRAM
			
			//指定顶点函数
			#pragma vertex vert

			//指定表面函数
			#pragma fragment frag

			//载入函数库
			#include "UnityCG.cginc"

			//顶点与表面转运结构
			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 worldPos : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				//模型顶点转换为摄像机空间渲染坐标
				o.pos = UnityObjectToClipPos(v.vertex);

				//模型顶点转换为世界空间坐标
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			//获取面板颜色
			float4 _Color;

			float4 frag(v2f i) : SV_Target
			{
				//初始化输出颜色
				float4 col = _Color;
				//依据距离裁切平面
				clip(PosToPlaneDistance(i.worldPos.xyz));
				return col;
			}
			ENDCG
		}
	}
}