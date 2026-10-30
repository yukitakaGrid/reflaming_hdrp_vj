using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralBuildingHybrid : MonoBehaviour
{
    [Header("Size Settings")]
    public float width = 1f;
    public float depth = 1f;
    public float height = 1f;

    [Header("Mode Settings")]
    [Tooltip("チェックを入れると線画モード、外すと通常モードになります")]
    public bool isWireframeMode = false;

    // 変更検知用キャッシュ
    private float _lastWidth, _lastDepth, _lastHeight;
    private bool _lastMode;

    // メッシュデータ
    private Mesh _mesh;
    private Vector3[] _vertices; // 24頂点（面用）
    
    // インデックス配列のキャッシュ
    private int[] _triangleIndices;
    private int[] _lineIndices;

    void Start()
    {
        InitializeMesh();
        UpdateMeshGeometry();
    }

    void Update()
    {
        // サイズかモードが変わったら再生成
        if (width != _lastWidth || depth != _lastDepth || height != _lastHeight || isWireframeMode != _lastMode)
        {
            UpdateMeshGeometry();
        }
    }

    void OnValidate()
    {
        if (_mesh != null)
        {
            UpdateMeshGeometry();
        }
    }

    private void InitializeMesh()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.name = "ProceduralBuildingHybrid";
            _mesh.MarkDynamic(); // 頻繁な書き換えに最適化
            GetComponent<MeshFilter>().mesh = _mesh;
        }

        // 面用の24頂点構成（UVのために面ごとに頂点を独立させる）
        _vertices = new Vector3[24];
        Vector2[] uvs = new Vector2[24];

        // --- 面モード用のインデックス (Triangles) ---
        _triangleIndices = new int[36]; // 6面 * 2ポリゴン * 3頂点

        // --- 線画モード用のインデックス (Lines) ---
        // 12本の辺を描画したいが、頂点が24個に分かれているため
        // ここでは「各面の枠線」を描くことで網羅する
        // 6面 * 4辺 * 2頂点 = 48インデックス
        _lineIndices = new int[48];

        for (int i = 0; i < 6; i++)
        {
            int offset = i * 4;
            
            // UV設定 (0-1)
            uvs[offset + 0] = new Vector2(0, 0);
            uvs[offset + 1] = new Vector2(1, 0);
            uvs[offset + 2] = new Vector2(0, 1);
            uvs[offset + 3] = new Vector2(1, 1);

            // 三角形インデックス (面)
            int to = i * 6;
            _triangleIndices[to + 0] = offset + 0;
            _triangleIndices[to + 1] = offset + 2;
            _triangleIndices[to + 2] = offset + 1;
            _triangleIndices[to + 3] = offset + 2;
            _triangleIndices[to + 4] = offset + 3;
            _triangleIndices[to + 5] = offset + 1;

            // ラインインデックス (線)
            // 0-1, 1-3, 3-2, 2-0 のように一周させる
            int lo = i * 8;
            _lineIndices[lo + 0] = offset + 0; _lineIndices[lo + 1] = offset + 1; // 下
            _lineIndices[lo + 2] = offset + 1; _lineIndices[lo + 3] = offset + 3; // 右
            _lineIndices[lo + 4] = offset + 3; _lineIndices[lo + 5] = offset + 2; // 上
            _lineIndices[lo + 6] = offset + 2; _lineIndices[lo + 7] = offset + 0; // 左
        }

        _mesh.vertices = _vertices;
        _mesh.uv = uvs;
        
        // 初期状態のトポロジーをセット
        SetTopology();
    }

    /// <summary>
    /// 現在のモードに合わせてトポロジー（面か線か）を切り替える
    /// </summary>
    private void SetTopology()
    {
        _mesh.Clear(); // 一旦クリアしないとトポロジー変更でエラーが出ることがある
        _mesh.vertices = _vertices; // 頂点は再セットが必要
        _mesh.uv = _mesh.uv;        // UVも維持

        if (isWireframeMode)
        {
            _mesh.SetIndices(_lineIndices, MeshTopology.Lines, 0);
        }
        else
        {
            _mesh.SetIndices(_triangleIndices, MeshTopology.Triangles, 0);
        }
        _mesh.RecalculateBounds();
        if (!isWireframeMode) _mesh.RecalculateNormals();
    }

    public void UpdateMeshGeometry()
    {
        if (_mesh == null) InitializeMesh();

        // モード切替の検知
        if (isWireframeMode != _lastMode)
        {
            // モードが変わった時だけトポロジー（インデックス）を再設定
            // これにより SetIndices のコストを最小限にする
             _lastMode = isWireframeMode; // 先に更新
             UpdateVertices(); // 頂点位置計算
             SetTopology();    // トポロジー適用
        }
        else
        {
            // サイズだけの変更なら頂点移動のみ（高速）
            UpdateVertices();
             _mesh.vertices = _vertices; // 頂点更新適用
             _mesh.RecalculateBounds();
        }

        _lastWidth = width;
        _lastDepth = depth;
        _lastHeight = height;
    }

    private void UpdateVertices()
    {
        width = Mathf.Max(0.1f, width);
        depth = Mathf.Max(0.1f, depth);
        height = Mathf.Max(0.1f, height);

        float hw = width * 0.5f;
        float hd = depth * 0.5f;
        float hh = height * 0.5f; // ★高さの半分（Half Height）を計算
        

// 前面
        _vertices[0] = new Vector3(-hw, -hh, -hd); _vertices[1] = new Vector3(hw, -hh, -hd);
        _vertices[2] = new Vector3(-hw, hh, -hd);  _vertices[3] = new Vector3(hw, hh, -hd);
// 背面
        _vertices[4] = new Vector3(hw, -hh, hd);   _vertices[5] = new Vector3(-hw, -hh, hd);
        _vertices[6] = new Vector3(hw, hh, hd);    _vertices[7] = new Vector3(-hw, hh, hd);
// 左面
        _vertices[8] = new Vector3(-hw, -hh, hd);  _vertices[9] = new Vector3(-hw, -hh, -hd);
        _vertices[10]= new Vector3(-hw, hh, hd);   _vertices[11]= new Vector3(-hw, hh, -hd);
// 右面
        _vertices[12]= new Vector3(hw, -hh, -hd);  _vertices[13]= new Vector3(hw, -hh, hd);
        _vertices[14]= new Vector3(hw, hh, -hd);   _vertices[15]= new Vector3(hw, hh, hd);
// 上面
        _vertices[16]= new Vector3(-hw, hh, -hd);  _vertices[17]= new Vector3(hw, hh, -hd);
        _vertices[18]= new Vector3(-hw, hh, hd);   _vertices[19]= new Vector3(hw, hh, hd);
// 底面
        _vertices[20]= new Vector3(-hw, -hh, hd);  _vertices[21]= new Vector3(hw, -hh, hd);
        _vertices[22]= new Vector3(-hw, -hh, -hd); _vertices[23]= new Vector3(hw, -hh, -hd);
    }
    
    /// <summary>
    /// 外部（Wobbleスクリプトなど）から加工済みの頂点配列を受け取り、メッシュに適用します。
    /// </summary>
    public void ApplyWobbledVertices(Vector3[] wobbledVertices)
    {
        if (_mesh == null) return;

        // 頂点数が合っているか安全確認
        if (wobbledVertices == null || wobbledVertices.Length != _vertices.Length)
        {
            // Debug.LogWarning("頂点数が一致しません"); // 頻繁に出るのでコメントアウト推奨
            return;
        }

        // メッシュに適用
        _mesh.vertices = wobbledVertices;
        
        // 面モードの時だけ法線を再計算（線モードでは不要＆エラーになるため）
        if (!isWireframeMode)
        {
            _mesh.RecalculateNormals();
        }
        
        // バウンディングボックスも更新（カリング対策）
        _mesh.RecalculateBounds();
    }

    /// <summary>
    /// 現在の（変形前の）基本頂点情報を返します。
    /// Wobbleスクリプトはこれを元に変形を計算します。
    /// </summary>
    public Vector3[] GetBaseVertices()
    {
        // まだ初期化されてなければ初期化
        if (_vertices == null || _vertices.Length == 0)
        {
            InitializeMesh();
            UpdateMeshGeometry();
        }
        return _vertices;
    }

    /// <summary>
    /// WobbleがOFFになった時に呼び出して、元の形状に戻します。
    /// </summary>
    public void ResetToOriginalPositions()
    {
        if (_mesh == null || _vertices == null) return;
        
        _mesh.vertices = _vertices;
        if (!isWireframeMode) _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}