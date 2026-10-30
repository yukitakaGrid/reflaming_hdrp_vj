using UnityEngine;
using System.Collections.Generic;
using System.Collections; // コルーチンを使うために必要
using System.Linq; // ToList() を使うために必要

public class StreetLightControl : MonoBehaviour
{
    [Header("ディミング設定")]
    [Tooltip("AudioReactiveからlowFrequencyEnergyを代入 (0〜1)")]
    public float InputEnergy = 0.5f; 

    [Header("Light Management")]
    [SerializeField]
    private List<ManagedStreetLight> managedLights = new List<ManagedStreetLight>();
    
    // タイマー設定
    private const float SwitchInterval = 1f; // 10秒に1回
    
    private int lightsToSwitchCount = 8;
    
    public MidiController midiController;

    void Start()
    {
        InitializeLights();
        RandomBoolSetter(managedLights);
    }
    
    void Update()
    {
        // 0から1の入力エネルギーを使って、すべての管理対象ライトの強度を調整する
        InputEnergy = midiController.GetControlValue(1);
        
        foreach (var managedLight in managedLights)
        {
            // 1. LightがON状態であるかを確認
            if (managedLight.IsOn)
            {
                // 2. 初期強度とInputEnergyを使って線形補間（Lerp）を計算
                // InputEnergyが1に近いほど初期強度に近づき、0に近いほど暗くなります。
                float targetIntensity = Mathf.Lerp(0f, managedLight.initialIntensity, InputEnergy);
                
                // 3. LightのIntensityを更新
                // ※ ここで Update が実行される度に Intensity が上書きされます。
                managedLight.intensity = targetIntensity;
                managedLight.LightComponent.intensity = targetIntensity;
            }
            else
            {
                // LightがOFFの場合: 強度をゼロに設定 (念のため再度設定)
                managedLight.LightComponent.intensity = 0f;
            }
        }
    }
    
    private void InitializeLights()
    {
        Light[] allLights = GetComponentsInChildren<Light>(true);
        
        // 既存の「initialIntensity = allLights[0].intensity;」はManagedStreetLight内で処理されるため削除
        
        // Debug.Log($"LightDimmer: 初期強度 ({initialIntensity:F2}) を保存しました。"); // ログも削除
        
        managedLights.Clear();
        int count = 0;

        foreach (Light light in allLights)
        {
            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                // ManagedLight インスタンスを作成し、リストに追加 (コンストラクタ内で初期強度が保存される)
                managedLights.Add(new ManagedStreetLight(light));
                count++;
            }
        }

        Debug.Log($"💡 Point Light と Spot Light {count} 個を管理対象に登録しました。");
    }

    // 外部から特定の Light の状態を切り替えるためのパブリックメソッド（例）
    public void SetLightState(int index, bool state)
    {
        if (index >= 0 && index < managedLights.Count)
        {
            // ManagedLight の IsOn プロパティを変更すると、自動的にライトの Intensity が更新されます。
            managedLights[index].IsOn = state;
        }
    }

    private void RandomBoolSetter(List<ManagedStreetLight> lights)
    {
        int listSize = lights.Count;
        // 2. true にする要素の数を計算 (20 * 1/3 = 6.66... なので、ここでは 7個とする)
        int trueCount = Mathf.RoundToInt(listSize / 6.0f);

        // 3. true に設定するインデックスをランダムに選ぶための処理
        
        // 既に true に設定されたインデックスを記録するセット
        HashSet<int> selectedIndices = new HashSet<int>();

        // 目標数に達するまでランダムなインデックスを選び、true に設定する
        while (selectedIndices.Count < trueCount)
        {
            // 0 から listSize - 1 までのランダムなインデックスを生成
            // Random.Range(minInclusive, maxExclusive)
            int randomIndex = Random.Range(0, listSize);

            // 既に選択されていないインデックスであれば、セットに追加し、リストの要素を true にする
            if (selectedIndices.Add(randomIndex))
            {
                lights[randomIndex].IsOn = true;
            }
        }
    }
    
    /// <summary>
    /// 指定した数のライトをランダムで変更
    /// </summary>
    public void RandomLightSwitcher()
    {
        for (int i = 0; i < lightsToSwitchCount; i++)
        {
            // 2. リストの中からランダムなインデックスを決定
            // Random.Range(minInclusive, maxExclusive)
            int randomIndex = Random.Range(0, managedLights.Count);

            // 3. ランダムに選ばれた ManagedLight のインスタンスを取得
            ManagedStreetLight targetLight = managedLights[randomIndex];

            // 4. IsOn プロパティを使って状態を反転させる
            // プロパティの setter 内で Light の Intensity 更新処理が自動で実行されます
            targetLight.IsOn = !targetLight.IsOn; 
        }
    }
}