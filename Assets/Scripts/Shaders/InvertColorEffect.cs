using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/InvertColorEffect")]
public sealed class InvertColorEffect : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    Material m_Material;

    public bool IsActive() => m_Material != null && intensity.value > 0f;

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    const string kShaderName = "Shader Graphs/InvertColor";

    public override void Setup()
    {
        if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume InvertColorEffect is unable to load. To fix this, please edit the 'kShaderName' constant in InvertColorEffect.cs or change the name of your custom post process shader.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;
        
        m_Material.SetVector("_CustomScale", new Vector4(
            source.rtHandleProperties.rtHandleScale.x,
            source.rtHandleProperties.rtHandleScale.y,
            0, 0
        ));

        if (GlobalMidiData.CurrentInvert)
        {
            // TAAリセット
            camera.Reset();
        
            // 1. 反転処理を実行
            m_Material.SetFloat("_Intensity", 1.0f); // 強度を最大に固定（ブール値制御のため）
            cmd.Blit(source, destination, m_Material, 0);
        }
        else
        {
            // 2. 💡 必須: 反転が無効な場合、元の映像をシェーダー処理なしで転送
            //    これにより、destinationバッファがクリアされ、元の映像が表示される
            cmd.Blit(source, destination);
        }
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
    
    public static class GlobalMidiData
    {
        public static bool CurrentInvert { get; set; } = false;
    }
}
