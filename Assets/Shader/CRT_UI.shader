Shader "Custom/CRT_UI_Advanced"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        
        _Distortion ("Distortion (樽型の歪み)", Range(0, 1)) = 0.2
        _RGBShift ("RGB Shift (色ズレ)", Range(0, 0.05)) = 0.01
        _Scanline ("Scanline (細かい横縞の濃さ)", Range(0, 1)) = 0.3
        
        // 流れる「歪み線」の設定
        _RollSpeed ("Roll Speed (線が流れる速さ)", Range(-5, 5)) = 2.0
        _RollAmount ("Roll Amount (横ブレの強さ)", Range(0, 0.1)) = 0.005
        
        // ★新機能：帯の太さを調整するスライダー
        // （値が小さいほど太く、大きいほど細い線になります）
        _RollThickness ("Roll Thickness (帯の細かさ/太さ)", Range(1, 500)) = 20.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Distortion;
            float _RGBShift;
            float _Scanline;
            float _RollSpeed;
            float _RollAmount;
            float _RollThickness; // ★追加した変数を受け取る

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 1. 樽型歪み (Barrel Distortion)
                float2 centeredUV = uv - 0.5; 
                float r2 = dot(centeredUV, centeredUV); 
                uv = uv + centeredUV * (_Distortion * r2);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return fixed4(0, 0, 0, 1);

                // 2. 流れる「歪みの線」 (Roll Band)
                float roll = sin(uv.y * 2.0 + _Time.y * _RollSpeed);
                
                // ★ここを変更：_RollThicknessを使って帯の太さを決める
                float rollBand = pow(abs(roll), _RollThickness); 
                
                uv.x += rollBand * _RollAmount * sin(_Time.y * 50.0); 

                // 3. 色収差
                fixed4 texCol;
                if (_RGBShift > 0.0001) {
                    fixed r = tex2D(_MainTex, uv + float2(_RGBShift, 0)).r;
                    fixed g = tex2D(_MainTex, uv).g;
                    fixed b = tex2D(_MainTex, uv - float2(_RGBShift, 0)).b;
                    fixed a = tex2D(_MainTex, uv).a;
                    texCol = fixed4(r, g, b, a);
                } else {
                    // シフトが0なら普通に1回読む（これでボケが最小限になる）
                    texCol = tex2D(_MainTex, uv);
                }
                fixed4 color = texCol * i.color;

                // 4. 走査線 ＋ 通過中の帯を暗くする
                float scanline = sin(uv.y * 800.0) * 0.05 * _Scanline;
                color.rgb -= scanline;
                color.rgb -= rollBand * 0.2; 

                return color;
            }
            ENDCG
        }
    }
}