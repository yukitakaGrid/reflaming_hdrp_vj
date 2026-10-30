Shader "Custom/InvertColor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // レンダリング結果が自動的に入る
    }
    SubShader
    {
        // 画面全体に適用するポストプロセス用のタグ設定
        Tags { "RenderType"="Opaque" "Queue"="Geometry" } 
        LOD 100

        Pass
        {
            // ポストプロセス用の設定
            ZTest Always 
            Cull Off 
            ZWrite Off 

            CGPROGRAM
            #pragma vertex vert_img // ポストプロセス用の頂点シェーダー
            #pragma fragment frag
            #include "UnityCG.cginc" // Unityの組み込み変数を読み込む

            sampler2D _MainTex;

            // フラグメントシェーダーに渡される構造体
            struct v2f 
            {
                float4 pos : SV_POSITION; // 頂点位置
                float2 uv : TEXCOORD0;    // テクスチャUV座標
            };

            // フラグメントシェーダー
            fixed4 frag (v2f i) : SV_Target
            {
                // _MainTexから現在のピクセルの色を取得
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // R, G, B成分を反転（1.0から引く）
                col.rgb = 1.0 - col.rgb;

                return col;
            }
            ENDCG
        }
    }
}