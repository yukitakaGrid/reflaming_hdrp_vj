using System;
using UnityEngine;

public class DynamicGenerateIntervalExecutor : MonoBehaviour
{
    public float inputParameter = 0f;

    // 現在のカウント
    private int currentCount = 0;
    
    public SimpleBSPGenerator generator;
    public MidiController midi;

    private void Update()
    {
        inputParameter = midi.GetControlValue(3);
    }

    public void TryExecute()
    {
        // 1. 入力が0以下の場合は何もしない（0回実行）
        if (inputParameter <= 0f)
        {
            currentCount = 0; // カウンターをリセット（お好みで）
            return;
        }

        // 2. 入力0～1を、インターバル4～1に変換する
        // inputが小さい(0に近い) -> 4 (4回に1回)
        // inputが大きい(1に近い) -> 1 (毎回)
        float mappedValue = Mathf.Lerp(4f, 1f, inputParameter);
        
        // 四捨五入して整数にする (例: 3.8 -> 4, 1.2 -> 1)
        int interval = Mathf.RoundToInt(mappedValue);

        // 安全策：インターバルが1未満にならないようにする
        if (interval < 1) interval = 1;

        // 3. カウントアップして判定
        currentCount++;

        if (currentCount >= interval)
        {
            // === ここに実行したい処理を書く ===
            generator.GenerateBSP();
            
            // カウンターをリセット
            currentCount = 0;
        }
    }
}