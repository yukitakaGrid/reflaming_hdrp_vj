using UnityEngine;
using System.Collections.Generic;

public class CityVisualizer : MonoBehaviour
{
    [Header("References")]
    public SimpleBSPGenerator bspGenerator; // 前回のスクリプトをアタッチ
    public GameObject buildingPrefab;       // Cubeのプレハブ
    public CityVisualsManager cityVisualsManager;
    public MidiController midi;

    [Header("Building Settings")]
    public bool onlyLeafNodes = true;       // trueなら「一番細かい部屋」だけビルにする（推奨）
    public float heightMultiplier = 5000f;  // 高さの係数（大きいほど背が高くなる）
    public float minHeight = 1.0f;          // 建物の最低の高さ
    public float maxHeight = 50.0f;         // 建物の最高の高さ
    public float padding = 0.5f;            // 建物の隙間（マージン）
    public Vector3 cityGeneratePosition;

    // ノードとゲームオブジェクトを紐付ける辞書
    private Dictionary<BSPNode, GameObject> buildingMap = new Dictionary<BSPNode, GameObject>();
    
    void Update()
    {
        // ジェネレーターの準備ができていなければ何もしない
        if (bspGenerator == null || bspGenerator.GetAllNodes() == null) return;

        UpdateBuildingTransforms();
        
        cityVisualsManager.UpdateVisuals(midi.GetControlValue(2),midi.GetControlValue(4));
    }
    
    /// <summary>
    /// 1. ビルを最初に一括生成するメソッド
    /// （マップ生成の最後に1回だけ呼ぶ）
    /// </summary>
    public void GenerateBuildings()
    {
        // まず既存のビルを全削除（リセット）
        ClearAllBuildings();

        List<BSPNode> nodes = bspGenerator.GetAllNodes();

        foreach (var node in nodes)
        {
            // 「末端ノード（子供がいない）」だけを対象にする場合のチェック
            if (onlyLeafNodes && (node.left != null || node.right != null))
            {
                continue;
            }

            // 生成 (Instantiate)
            GameObject building = Instantiate(buildingPrefab, transform);
            
            // 名前をわかりやすくしておく（デバッグ用）
            building.name = $"Building_{node.rect.x}_{node.rect.y}";

            // Dictionaryに登録
            buildingMap.Add(node, building);
        }

        // 生成が終わったので、一度位置とサイズを適用する
        UpdateBuildingTransforms();

        // ★ここでマテリアル入れ替えスクリプトを初期化！★
        // これで「生成完了 -> Dictionary確定 -> マテリアル初期化」の順序が守られます
        if (cityVisualsManager != null)
        {
            cityVisualsManager.Initialize(buildingMap);
        }
    }

    /// <summary>
    /// 2. ビルの形状（位置・高さ・サイズ）だけを更新するメソッド
    /// （Updateで呼べばリアルタイムに街が変形するアニメーションになる）
    /// </summary>
    public void UpdateBuildingTransforms()
    {
        // Dictionaryに登録されている全てのビルに対して処理
        foreach (var kvp in buildingMap)
        {
            BSPNode node = kvp.Key;
            GameObject building = kvp.Value;

            if (building == null) continue;

            // === 建物の形状更新ロジック ===
            Rect r = node.rect;

            // 面積を計算
            float area = r.width * r.height;
            if (area <= 0.1f) area = 0.1f; // ゼロ除算防止

            // 【高さの計算】
            float targetHeight = heightMultiplier / area;
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

            // サイズ計算（padding考慮）
            float w = Mathf.Max(0, r.width - padding);
            float d = Mathf.Max(0, r.height - padding);

            // トランスフォーム反映
            // 位置: 中心座標 + 高さの半分
            // Rect.centerは (x + w/2, y + h/2)
            Vector3 localPos = new Vector3(r.center.x, targetHeight / 2f, r.center.y) + cityGeneratePosition;
            
            building.transform.localPosition = localPos;
            building.transform.localScale = new Vector3(w, targetHeight, d);
        }
    }

    /// <summary>
    /// 全てのビルを削除してDictionaryをクリアする
    /// </summary>
    private void ClearAllBuildings()
    {
        foreach (var building in buildingMap.Values)
        {
            if (building != null)
            {
                Destroy(building);
            }
        }
        buildingMap.Clear();
    }
}