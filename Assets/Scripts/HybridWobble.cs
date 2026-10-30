using UnityEngine;

// ProceduralBuildingHybrid が必要
[RequireComponent(typeof(ProceduralBuildingHybrid))]
public class HybridWobble : MonoBehaviour
{
    [Header("Wobble Settings")]
    [SerializeField] private float speed = 2.0f;
    [SerializeField] private float noiseScale = 1.0f;
    [SerializeField] private float maxIntensity = 0.5f;

    [Header("Control")]
     public float wobbleRatio = 0f;
     public float wobbleScale = 3f;

    // 連携対象
    private ProceduralBuildingHybrid _targetBuilding;
    
    // 頂点キャッシュ
    private Vector3[] _baseVertices;     // 変形元（参照用）
    private Vector3[] _workingVertices;  // 変形作業用（コピー）
    
    private bool _isInitialized = false;
    private bool _needsReset = false; // リセットが必要かどうかのフラグ
    
    public MidiController midi;
    
    // 💡 各インスタンスの初期ノイズ開始時間オフセット
    private float _timeOffset; 
    
    // 💡 ノイズのシード（オフセット）値
    private Vector3 _seedOffset;

    void Start()
    {
        _targetBuilding = GetComponent<ProceduralBuildingHybrid>();
        // ※Start時点ではまだ相手の頂点が生成されていない可能性があるので、
        //  ここでは初期化せず、最初のUpdateで初期化を試みます。
        
        // 💡 外部の MidiController コンポーネントをシーン全体から検索し、参照を設定する
        //    これにより、インスペクターでの手動設定や、プリハブの制約を回避します。
        if (midi == null)
        {
            midi = FindAnyObjectByType<MidiController>();
        
            if (midi == null)
            {
                Debug.LogError("シーン内に MidiController コンポーネントが見つかりません。");
            }
        }
        
        // 💡 実行時にランダムな時間オフセットを設定
        //    これにより、各インスタンスが異なるノイズの場所からスタートする
        _timeOffset = Random.Range(0f, 1000f); 
        
        // 開始時に初期シードを設定
        GenerateNewSeed();
    }

    // 初期化を試みるメソッド
    private bool TryInitialize()
    {
        if (_targetBuilding == null) return false;
        
        // ハイブリッド側から基本頂点を取得
        _baseVertices = _targetBuilding.GetBaseVertices();
        
        if (_baseVertices == null || _baseVertices.Length == 0)
        {
             // まだ準備ができてない
             return false;
        }

        // 作業用配列を確保してコピー
        _workingVertices = new Vector3[_baseVertices.Length];
        System.Array.Copy(_baseVertices, _workingVertices, _baseVertices.Length);
        
        _isInitialized = true;
        return true;
    }

    void Update()
    {
        // 初期化がまだなら試みる
        if (!_isInitialized)
        {
            if (!TryInitialize()) return; // まだ無理なら諦めて次のフレームへ
        }
        
        // サイズ変更などで頂点数が変わった場合の再初期化チェック
        if (_baseVertices != _targetBuilding.GetBaseVertices())
        {
            // 参照先が変わった（＝再生成された）ので初期化し直す
            _isInitialized = false;
            if (!TryInitialize()) return;
        }

        // --- Wobble処理 ---
        
        if (midi.GetControlValue(6) == 1)
        {
            wobbleRatio = 1;
        }
        else
        {
            wobbleRatio = 0;
        }
        
        // wobbleRatioがほぼ0なら
        if (wobbleRatio <= 0.001f)
        {
            // リセットが必要な状態なら、一度だけリセット命令を送る
            if (_needsReset)
            {
                _targetBuilding.ResetToOriginalPositions();
                _needsReset = false;
            }
            return;
        }

        // 変形実行
        AnimateVertices();
        // 変形したので、次回0になったらリセットが必要
        _needsReset = true;
    }

    private void AnimateVertices()
    {
        // 💡 Time.time にインスタンスごとの時間オフセットを加算
        float time = (Time.time + _timeOffset) * speed; 
        float currentIntensity = maxIntensity * wobbleRatio;

        // 作業用配列に変形結果を書き込む
        for (int i = 0; i < _baseVertices.Length; i++)
        {
            Vector3 v = _baseVertices[i]; // 元の位置を基準にする

            // 3軸ノイズ計算（シードオフセットと時間オフセットを加算）
            float noiseX = (Mathf.PerlinNoise(v.y * noiseScale + time + _seedOffset.x, v.z * noiseScale + time + 10f) - 0.5f) * currentIntensity;
            float noiseY = (Mathf.PerlinNoise(v.z * noiseScale + time + _seedOffset.y, v.x * noiseScale + time + 20f) - 0.5f) * currentIntensity;
            float noiseZ = (Mathf.PerlinNoise(v.x * noiseScale + time + _seedOffset.z, v.y * noiseScale + time + 30f) - 0.5f) * currentIntensity;

            _workingVertices[i].x = v.x + noiseX;
            _workingVertices[i].y = v.y + noiseY;
            _workingVertices[i].z = v.z + noiseZ;
        }

        // 加工した頂点をハイブリッドスクリプトに適用してもらう
        _targetBuilding.ApplyWobbledVertices(_workingVertices);
    }
    
    // 外部制御用
    public void SetWobbleRatio(float ratio)
    {
        wobbleRatio = Mathf.Clamp01(ratio);
    }
    
    // 💡 ランダムな新しいシードオフセットを生成するメソッド
    private void GenerateNewSeed()
    {
        // 非常に大きなランダム値（Perlinノイズは大きなオフセットでパターンが大きく変わる）
        _seedOffset = new Vector3(
            Random.Range(100f, 10000f),
            Random.Range(100f, 10000f),
            Random.Range(100f, 10000f)
        );
        Debug.Log($"New Noise Seed Generated: {_seedOffset.x}");
    }
}