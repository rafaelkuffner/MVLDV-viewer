// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Depth Billboard"
{
	Properties 
	{
		_Size ("Size", Range(0,10)) =5 //patch size
		_ColorTex ("Texture", 2D) = "white" {}
		_DepthTex ("TextureD", 2D) = "white" {}
	}

	SubShader 
	{
		Pass
		{
			Tags { "RenderType"="Opaque" }
			

			CGPROGRAM
				#pragma target 3.0
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				//#pragma geometry GS_Main
				#include "UnityCG.cginc" 

				// **************************************************************
				// Data structures												*
				// **************************************************************
			

				struct FS_INPUT
				{
					float4	pos		: POSITION;
					float2  tex0	: TEXCOORD0;
					float4 color	: COLOR;
					half psize : PSIZ;
				};


				// **************************************************************
				// Vars															*
				// **************************************************************
				float _Brightness;
				half _Size;
				sampler2D _ColorTex;				
				sampler2D _DepthTex; 
				float4 _Color; 

				// **************************************************************
				// Shader Programs												*
				// **************************************************************

				// Vertex Shader ------------------------------------------------
				FS_INPUT VS_Main(appdata_full v)
				{
					FS_INPUT output = (FS_INPUT)0;

						float4 c = tex2Dlod(_ColorTex,float4(v.vertex.x,1-v.vertex.y,0,0));
						float4 d = tex2Dlod(_DepthTex,float4(v.vertex.x,v.vertex.y,0,0));
						int dr = d.r*255;
						int dg = d.g*255;
						int db = d.b*255;
						int da = d.a*255;
						int dValue = (int)(db | (dg << 0x8) | (dr << 0x10) | (da << 0x18));
						float4 pos;

						pos.z = dValue / 1000.0;
						int x = 512*v.vertex.x;
						int y = 424*v.vertex.y;
						float vertx = float(x);
						float verty = float(424 -y);
						pos.x =  pos.z*(vertx- 255.5)/351.001462;
						pos.y =  pos.z*(verty-  211.5)/351.001462;
						pos.w = 1;	

					
					output.pos = UnityObjectToClipPos(pos); //pos;
					if(dValue ==0)
					output.pos.z = 500000;
					output.color = c;
					output.psize = _Size;
					return output;
				}


				// Fragment Shader -----------------------------------------------
				float4 FS_Main(FS_INPUT input) : COLOR
				{
					if(input.color.a == 0) discard;
					return input.color;
				}

			ENDCG
		}
	} 
}
