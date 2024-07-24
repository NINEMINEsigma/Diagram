Shader "Unlit/BoundLight"
{
	Properties
	{
		[Toggle(_True)]_IsOpen("Is Open", float) = 1

    	_MainColor ("MainColor", Color) = (0,0,0,1) //模型主颜色
		_OutSideRimSize("SideRimSize", Float) = 0 //这是外扩大小

		_InSideRimColor ("InSideRimColor", Color) = (1,1,1,1)//内边缘光颜色
		_InSideRimPower("InSideRimPower", Range(0.0,5)) = 0 //边缘光强度  ,这个值可以控制菲涅尔影响范围的大小，这个值越大，效果上越边缘化
		_InSideRimIntensity("InSideRimIntensity", Range(0.0, 10)) = 0  //边缘光强度系数 这个值是反射的强度， 值越大，返回的强度越大，导致边缘的颜色不那么明显 
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Pass  //内边缘光pass
		{
		    Cull Front   //需要正面剔除，否则模型主pass渲染会看不到
			Blend SrcAlpha One // 需要设置成透明叠加
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
            #include "Lighting.cginc"

			uniform float4 _MainColor;
			uniform float  _OutSideRimSize;
			uniform float4 _InSideRimColor;
			uniform float  _InSideRimPower;
			uniform float _InSideRimIntensity;  
			uniform fixed _IsOpen;
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;

			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 normal : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float4 vertexWorld : TEXCOORD2;

			};
			v2f vert (appdata v)
			{
				v2f o;
				o.normal = mul(unity_ObjectToWorld, float4(v.normal,0)).xyz;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertexWorld = mul(unity_ObjectToWorld, v.vertex);
				float3 worldViewDir = smoothstep(0,10,length(_WorldSpaceCameraPos.xyz - o.vertexWorld.xyz));
				v.vertex.xyz += v.normal*_OutSideRimSize*worldViewDir;  //顶点进行外扩
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertexWorld = mul(unity_ObjectToWorld, v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				clip(_IsOpen-0.9); 
			    i.normal = normalize(i.normal);//下面计算方式套用菲涅尔计算
				float3 worldViewDir = normalize(_WorldSpaceCameraPos.xyz - i.vertexWorld.xyz);//获取单位视角方向   相机世界空间位置减去顶点世界空间位置
				half NdotV = max(0, dot(i.normal, worldViewDir));//计算法线方向和视角方向点积,约靠近边缘夹角越大，值约小，那就是会越在圆球中间约亮，越边缘约暗
				NdotV = 1.0-NdotV;//这里需求是越边缘约亮，所以需要反一下，这里用1 减下
				float fresnel =pow(NdotV,_InSideRimPower)*_InSideRimIntensity;//使用上面的属性参数，这里不多说
			    float3  Emissive=_InSideRimColor.rgb*fresnel; //配置上属性里面的内边缘光颜色
				return _MainColor+float4(Emissive,1);//最后加在本体主颜色就即可
			}
			ENDCG
		}
	}
}

