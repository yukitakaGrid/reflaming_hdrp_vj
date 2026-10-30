using UnityEngine;

[ExecuteInEditMode]
public class HalftoneEffect : MonoBehaviour
{
    public Material effectMaterial; // シェーダーを適用したマテリアル
    
    // カメラがシーンのレンダリングを終える直前に呼ばれる
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (effectMaterial != null)
        {
            // source（レンダリング結果）を effectMaterial を使って destination に描画
            Graphics.Blit(source, destination, effectMaterial);
        }
        else
        {
            // マテリアルがない場合は何もせずにそのまま描画
            Graphics.Blit(source, destination);
        }
    }
}