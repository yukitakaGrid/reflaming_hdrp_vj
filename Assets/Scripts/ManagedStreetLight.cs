using UnityEngine;
using System; // Funcデリゲートを使うために必要 (ここでは未使用だが、拡張時に役立つ)

// [System.Serializable] をつけると、Unityのインスペクター上でこのクラスのリストを確認できます。
[Serializable]
public class ManagedStreetLight
{
    // 結びついている Light コンポーネント
    public Light LightComponent { get; private set; }
    
    // 最初に保存した Intensity の値
    public float initialIntensity;
    public float intensity;
    
    // 現在のON/OFF状態 (bool)
    [SerializeField] private bool _isOn;

    // IsOn プロパティ: 状態変更と Intensity 更新ロジックをカプセル化
    public bool IsOn
    {
        get => _isOn;
        set
        {
            // 状態が変わった場合のみ処理を実行
            if (_isOn != value)
            {
                _isOn = value;
                ApplyState();
            }
        }
    }

    // コンストラクタ：Lightを受け取り、初期 Intensity を保存
    public ManagedStreetLight(Light light)
    {
        LightComponent = light;
        initialIntensity = light.intensity;
        // 初期状態を false に設定し、初期化時に必ず消灯させる
        _isOn = false;
        LightComponent.intensity = 0f;
    }

    // 状態（bool）に基づいて Light の Intensity を更新する
    private void ApplyState()
    {
        if (_isOn)
        {
            // true の場合、保存しておいた初期 Intensity を適用
            LightComponent.intensity = intensity;
        }
        else
        {
            // false の場合、Intensity を 0 にして消灯
            LightComponent.intensity = 0f;
        }
    }
}