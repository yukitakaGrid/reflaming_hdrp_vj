using System.Collections.Generic;
using UnityEngine;

public class CityVisualsManager : MonoBehaviour
{
    [Header("Materials")]
    [Tooltip("通常時のマテリアル（障子など）")]
    [SerializeField] private Material syojiMaterial;

    [Tooltip("キラキラ時のマテリアル（優先度：高）")]
    [SerializeField] private Material kirakiraMaterial;

    [Tooltip("ワイヤーフレーム時のマテリアル（優先度：低）")]
    [SerializeField] private Material wireframeMaterial;

    [Header("Debug Settings")]
    [Tooltip("チェックを入れるとInspectorのスライダーで操作できます")]
    [SerializeField] private bool isDebugMode = false;
    
    [Tooltip("キラキラ率")]
    [Range(0f, 1f)] [SerializeField] private float debugKiraRatio = 0f;
    
    [Tooltip("ワイヤーフレーム率")]
    [Range(0f, 1f)] [SerializeField] private float debugWireRatio = 0f;

    [Header("Status (Read Only)")]
    [SerializeField] private int totalObjects = 0;
    [SerializeField] private int kiraCount = 0;
    [SerializeField] private int wireCount = 0;

    // 管理対象のビルリスト
    private List<ProceduralBuildingHybrid> allBuildings = new List<ProceduralBuildingHybrid>();

    // レンダラーのキャッシュ（GetComponentを減らすため）
    private Dictionary<ProceduralBuildingHybrid, Renderer> rendererCache = new Dictionary<ProceduralBuildingHybrid, Renderer>();

    // ランダムな順番を保持するリスト（2種類）
    private List<ProceduralBuildingHybrid> kiraOrder = new List<ProceduralBuildingHybrid>();
    private List<ProceduralBuildingHybrid> wireOrder = new List<ProceduralBuildingHybrid>();

    // 今回変更を加えたビルを記録するセット（優先度管理用）
    private HashSet<ProceduralBuildingHybrid> processedBuildings = new HashSet<ProceduralBuildingHybrid>();

    // 前回値のキャッシュ
    private int lastKiraTarget = -1;
    private int lastWireTarget = -1;

    private void Update()
    {
        if (isDebugMode)
        {
            UpdateVisuals(debugKiraRatio, debugWireRatio);
        }
    }

    /// <summary>
    /// 初期化：マップ生成後に呼んでください
    /// </summary>
    public void Initialize(Dictionary<BSPNode, GameObject> buildingMap)
    {
        allBuildings.Clear();
        rendererCache.Clear();
        kiraOrder.Clear();
        wireOrder.Clear();

        foreach (var obj in buildingMap.Values)
        {
            if (obj != null)
            {
                // 子供も含めて ProceduralBuildingHybrid を探す
                var hybrids = obj.GetComponentsInChildren<ProceduralBuildingHybrid>();
                foreach (var hybrid in hybrids)
                {
                    allBuildings.Add(hybrid);
                    
                    // レンダラーもキャッシュしておく
                    var r = hybrid.GetComponent<Renderer>();
                    if (r != null) rendererCache[hybrid] = r;
                }
            }
        }

        SetupInternal();
    }

    [ContextMenu("Debug: Initialize from Children")]
    public void DebugInitializeFromChildren()
    {
        allBuildings.Clear();
        rendererCache.Clear();
        kiraOrder.Clear();
        wireOrder.Clear();

        var hybrids = GetComponentsInChildren<ProceduralBuildingHybrid>();
        foreach (var hybrid in hybrids)
        {
            allBuildings.Add(hybrid);
            var r = hybrid.GetComponent<Renderer>();
            if (r != null) rendererCache[hybrid] = r;
        }
        
        SetupInternal();
        Debug.Log($"[Debug] {allBuildings.Count} 個のビルで初期化しました。");
    }

    private void SetupInternal()
    {
        // 2つの異なるランダム順序を作成
        kiraOrder.AddRange(allBuildings);
        wireOrder.AddRange(allBuildings);

        ShuffleList(kiraOrder);
        ShuffleList(wireOrder);

        totalObjects = allBuildings.Count;
        lastKiraTarget = -1;
        lastWireTarget = -1;

        // 初期化実行
        UpdateVisuals(0f, 0f);
    }

    /// <summary>
    /// ビルの見た目を更新します。
    /// 優先順位: キラキラ > ワイヤーフレーム > 通常
    /// </summary>
    public void UpdateVisuals(float kiraRatio, float wireRatio)
    {
        if (allBuildings.Count == 0) return;

        // 0.0～1.0に制限
        kiraRatio = Mathf.Clamp01(kiraRatio);
        wireRatio = Mathf.Clamp01(wireRatio);

        // 目標個数を計算
        int targetKiraCount = Mathf.FloorToInt(totalObjects * kiraRatio);
        int targetWireCount = Mathf.FloorToInt(totalObjects * wireRatio);

        // 負荷軽減：前回と目標数が同じなら処理しない
        if (targetKiraCount == lastKiraTarget && targetWireCount == lastWireTarget) return;

        lastKiraTarget = targetKiraCount;
        lastWireTarget = targetWireCount;

        // 処理済みリストをリセット
        processedBuildings.Clear();
        
        int assignedKira = 0;
        int assignedWire = 0;

        // ---------------------------------------------------------
        // 1. 【最優先】キラキラを適用
        // ---------------------------------------------------------
        for (int i = 0; i < targetKiraCount; i++)
        {
            if (i >= kiraOrder.Count) break;

            ProceduralBuildingHybrid building = kiraOrder[i];
            
            // 設定: 線画モードOFF + キラキラマテリアル
            ApplySettings(building, false, kirakiraMaterial);
            
            // 「処理済み」として登録（予約席を埋める）
            processedBuildings.Add(building);
            assignedKira++;
        }

        // ---------------------------------------------------------
        // 2. 【次点】ワイヤーフレームを適用（空いている場所のみ）
        // ---------------------------------------------------------
        foreach (var building in wireOrder)
        {
            // 目標数に達したら終了
            if (assignedWire >= targetWireCount) break;

            // ★重要: すでにキラキラ（処理済み）ならスキップして他を探す
            if (processedBuildings.Contains(building)) continue;

            // 設定: 線画モードON + ワイヤーマテリアル
            ApplySettings(building, true, wireframeMaterial);
            
            processedBuildings.Add(building);
            assignedWire++;
        }

        // ---------------------------------------------------------
        // 3. 【残り】通常マテリアルに戻す
        // ---------------------------------------------------------
        foreach (var building in allBuildings)
        {
            // 処理済みリストに入っていない＝何も割り当てられなかったビル
            if (!processedBuildings.Contains(building))
            {
                // 設定: 線画モードOFF + 障子マテリアル
                ApplySettings(building, false, syojiMaterial);
            }
        }

        // ステータス更新
        kiraCount = assignedKira;
        wireCount = assignedWire;
    }

    /// <summary>
    /// 指定されたビルにモードとマテリアルを適用するヘルパー
    /// </summary>
    private void ApplySettings(ProceduralBuildingHybrid building, bool isWireMode, Material mat)
    {
        // 1. モード設定（変更がある場合のみ）
        if (building.isWireframeMode != isWireMode)
        {
            building.isWireframeMode = isWireMode;
            // ProceduralBuildingHybridのUpdate内で自動的にメッシュ更新されます
        }

        // 2. マテリアル設定
        if (rendererCache.TryGetValue(building, out Renderer r))
        {
            if (r.sharedMaterial != mat)
            {
                r.sharedMaterial = mat;
            }
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}