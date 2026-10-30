using System.Collections.Generic;
using UnityEngine;

public class RandomBoolSetter : MonoBehaviour
{
    void Start()
    {
        // 1. リストをすべて false で初期化 (20要素)
        int listSize = 20;
        List<bool> boolList = new List<bool>(listSize);
        for (int i = 0; i < listSize; i++)
        {
            boolList.Add(false);
        }

        // 2. true にする要素の数を計算 (20 * 1/3 = 6.66... なので、ここでは 7個とする)
        int trueCount = Mathf.RoundToInt(listSize / 3.0f); // 7

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
                boolList[randomIndex] = true;
            }
        }

        // 4. 結果の確認 (Consoleに出力)
        Debug.Log("--- ランダム設定結果 ---");
        Debug.Log($"リストサイズ: {listSize}, 設定目標 true数: {trueCount}");

        int actualTrueCount = 0;
        for (int i = 0; i < listSize; i++)
        {
            Debug.Log($"インデックス {i}: {boolList[i]}");
            if (boolList[i])
            {
                actualTrueCount++;
            }
        }
        Debug.Log($"実際の true 数: {actualTrueCount}");
        // 結果は必ず目標の trueCount (7個) になります
    }
}