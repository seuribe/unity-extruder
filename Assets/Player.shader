  Shader "Custom/Player" {
    Properties {
		_Color ("Color", Color) = (1,1,0,1)
		_ShadowColor ("Shadow Color", Color) = (0.25,0,0.75,1)
		_NoiseTexture ("Noise Texture", 2D) = "white" {}
    }
    SubShader {
      Tags { "RenderType" = "Opaque" }
      Cull Off
      CGPROGRAM
          #pragma surface surf SimpleLambert finalcolor:shadowtint addshadow 
		  
	fixed4 _Color;
	fixed4 _ShadowColor;
	sampler2D _NoiseTexture;


		half4 LightingSimpleLambert (SurfaceOutput s, half3 lightDir, half atten) {
              half NdotL = dot (s.Normal, lightDir);
              half4 c;
			  float val = (NdotL * atten * 2);
              c.rgb = s.Albedo * _LightColor0.rgb * val;
              c.a = s.Alpha;
              return c;
		}

      struct Input {
			float3 worldPos;
      };
	  
	  void shadowtint (Input IN, SurfaceOutput o, inout fixed4 color)
      {
          color += _ShadowColor * 0.5;
      }
	
	float rand(float2 co) {
		return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
	}

      void surf (Input IN, inout SurfaceOutput o) {
		float rnd = tex2D(_NoiseTexture, IN.worldPos.xy*20).ggg;
		float noise = rand(IN.worldPos.xz) * 0.125 + 0.875;
		o.Albedo = (rnd * _Color) * noise;
      }

	  
      ENDCG
    } 
    Fallback "Diffuse"
  }