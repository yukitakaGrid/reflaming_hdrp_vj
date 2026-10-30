using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    // --- グリッド設定 ---
    
    // 生成するグリッドの一辺のセル数 (10x10)
    [SerializeField] private int GridResolution = 100; 
    
    // 元の正方形の一辺の長さ (1m)
    [SerializeField] private float ParentSize = 100.0f;

    [SerializeField] private float gridHeight = 1.0f;
    
    [SerializeField] private Vector3 position;
    
    // 各セルのサイズ (1.0m / 10 = 0.1m)
    private float CellSize;

    // --- オブジェクト設定 ---

    // 生成するオブジェクトのプレハブ (InspectorでCubeなどを設定)
    public GameObject CellPrefab;

    void Start()
    {
        CellSize = ParentSize / GridResolution;
        
        GenerateGrid();
        
        //セル生成後に親のトランスフォームを任意更新
        transform.position = position;
    }

    /// <summary>
    /// 10x10のグリッド状にオブジェクトを生成するプロシージャ
    /// </summary>
    void GenerateGrid()
    {
        if (CellPrefab == null)
        {
            Debug.LogError("CellPrefabが設定されていません。CubeなどのGameObjectを設定してください。");
            return;
        }

        // 親オブジェクトのTransformを取得（このスクリプトがアタッチされているオブジェクト）
        Transform parentTransform = transform;

        // グリッド全体の中心を (0, 0) に合わせるためのオフセット
        // 1m x 1m の場合、オフセットは 0.5m です
        float offset = ParentSize / 2.0f; 

        // 外部ループ: 行 (i = Z座標)
        for (int i = 0; i < GridResolution; i++)
        {
            // 内部ループ: 列 (j = X座標)
            for (int j = 0; j < GridResolution; j++)
            {
                // 1. 各セルの中心座標の計算
                
                // X座標の計算: (セルのインデックス + 0.5) * セルサイズ - オフセット
                // 例: i=0, j=0 の中心X = (0 + 0.5) * 0.1 - 0.5 = -0.45
                float x = (j + 0.5f) * CellSize - offset;
                
                // Z座標の計算: (セルのインデックス + 0.5) * セルサイズ - オフセット
                float z = (i + 0.5f) * CellSize - offset;

                // 2. オブジェクトの生成
                
                // 生成時の位置を計算
                Vector3 position = new Vector3(x, 0, z);

                // CellPrefabを生成し、このオブジェクトの子とする
                GameObject cell = Instantiate(CellPrefab, position, Quaternion.identity, parentTransform);

                // 3. オブジェクトの設定（プロシージャルな処理）
                
                // 名前を割り当て
                cell.name = $"Cell_{i}_{j}"; 

                // スケールを設定（Cubeの場合、一辺が1mなので、0.1mに縮小）
                // Y軸の高さは、ランダムにするなどのプロシージャルな処理を加えても良い
                cell.transform.localScale = new Vector3(CellSize, gridHeight, CellSize); 

                // セルの情報をコンソールに出力
                // Debug.Log($"生成: {cell.name} at {position}");
            }
        }
        
        Debug.Log($"1m x 1m の領域に {GridResolution}x{GridResolution} のグリッドを生成しました。");
    }
}