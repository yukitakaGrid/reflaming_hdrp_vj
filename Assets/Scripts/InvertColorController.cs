using UnityEngine;
using UnityEngine.Rendering;

public class InvertColorController : MonoBehaviour
{
    public MidiController midi;
    
    // InvertColorEffectのインスタンスを格納する変数
    private InvertColorEffect invertComponent;
    
    public Volume globalVolume;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            // 🚨 TryGet<T> で Volume Profile から目的のコンポーネントを取得する
            if (globalVolume.profile.TryGet<InvertColorEffect>(out invertComponent))
            {
                Debug.Log("InvertColorEffect インスタンスの取得に成功しました。");
            }
            else
            {
                Debug.LogError("Volume Profile に InvertColorEffect オーバーライドが見つかりません。");
            }
        }
        else
        {
            Debug.LogError("Volume Componentが設定されていないか、Profileがありません。");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (invertComponent != null && midi != null)
        {
            float midiValue = midi.GetControlValue(6);
            bool shouldBeActive = midiValue > 0.5f; // MIDIボタンが押されているか？
            
            InvertColorEffect.GlobalMidiData.CurrentInvert = shouldBeActive;
        }
    }
}
