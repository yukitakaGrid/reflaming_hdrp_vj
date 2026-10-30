using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 平均値計算（Average）に使用

public class AudioReactive: MonoBehaviour
{
    // --- 連携するコンポーネントと設定 ---
    
    [Header("連携設定")]
    public StreetLightControl lightControl; 
    public SimpleBSPGenerator rightSimpleBSPGenerator;
    public SimpleBSPGenerator leftSimpleBSPGenerator;
    public CameraSwitcher cameraSwitcher;
    public DynamicGenerateIntervalExecutor dynamicGenerateIntervalExecutor1;
    public DynamicGenerateIntervalExecutor dynamicGenerateIntervalExecutor2;
    
    [Tooltip("マイク入力を流し込む AudioSource")]
    public AudioSource audioSource;

    [Header("低音域 (FFT) 設定")]
    [Tooltip("分析対象の周波数帯域の最大インデックス (例: 500Hzまでなら 50)")]
    [SerializeField] private int lowFrequencyBandMaxIndex = 50; 
    
    [Tooltip("ライトを切り替えるための低音域エネルギーのしきい値")]
    [SerializeField] private float lowThreshold = 0.5f; 
    
    [Tooltip("連続でのスイッチングを防ぐためのクールダウン時間 (秒)")]
    [SerializeField] private float switchCooldown = 0.5f;
    
    [Tooltip("現在の低周波数エネルギー（計算結果表示用）")]
    public float lowFrequencyEnergy;
    
    [Header("高音域 (FFT) 設定")]
    [Tooltip("分析対象の周波数帯域の最小インデックス (例: 10000Hzからなら 250)")]
    [SerializeField] private int highFrequencyBandMinIndex = 250; 
    [Tooltip("分析対象の周波数帯域の最大インデックス (例: 21000Hzまでなら 500)")]
    [SerializeField] private int highFrequencyBandMaxIndex = 500;
    [Tooltip("ライトを切り替えるための高音域エネルギーのしきい値")]
    [SerializeField] private float highThreshold = 0.5f; 
    
    [Tooltip("現在の高周波数エネルギー（計算結果表示用）")]
    public float highFrequencyEnergy;
    
    [SerializeField] private bool lowFrequencyAnalyser = true;
    
    [Header("BPM 計算設定")]
    [Tooltip("現在のBPM（計算結果表示用）")]
    public float currentBpm;
    
    [Tooltip("BPM計測用: ビートの誤検知を防ぐ最小間隔 (0.2秒 = 300BPM以上は無視など)")]
    [SerializeField] private float bpmMeasureCooldown = 0.2f;
    
    [Tooltip("BPMを安定させるために平均を取るサンプル数")]
    [SerializeField] private int bpmSmoothingCount = 10;
    
    [Header("BPM反映の安定化設定")]
    [Tooltip("BPMがこの値以上変化した時だけ速度を更新する（ノイズ対策）")]
    [SerializeField] private float bpmChangeThreshold = 2.0f; // 例: 2BPM以上の変化で適用

    private float lastAppliedBpm = 0f; // 最後に適用したBPMを記憶する変数

    // --- プライベート変数 ---
    private float[] spectrumData;
    private float lastSwitchTime;
    
    // BPM計算用変数
    private float lastBpmBeatTime;
    private List<float> beatIntervals; // ビート間隔の履歴

    void Start()
    {
        // データの初期化 (AudioListener.GetSpectrumData は 256, 512, 1024, 2048 のいずれかが必要)
        spectrumData = new float[1024]; 
        lastSwitchTime = Time.time;
        
        // BPM初期化
        lastBpmBeatTime = Time.time;
        beatIntervals = new List<float>();

        // マイク入力の開始
        StartMicInput();
    }
    
    void Update()
    {
        // 1. 周波数スペクトルの取得 (FFTの実行)
        // データ取得元は AudioListener (Global) または AudioSource (Targeted) を選択できます
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Blackman);
        
        //Debug.Log(lowFrequencyEnergy);

        if (lowFrequencyAnalyser)
        {
            // 2. 低音域のエネルギーを計算
            lowFrequencyEnergy = CalculateLowFrequencyEnergy();
            // 3. しきい値判定とスイッチング
            if (lowFrequencyEnergy > lowThreshold && Time.time > lastSwitchTime + switchCooldown)
            {
                float currentTime = Time.time;
            
                //Debug.Log($"🔊 低音域がしきい値 ({threshold:F2}) を超えました: {lowFrequencyEnergy:F2}");
            
                // =========================================================
                // ★★★★★★★★★  リズムに合わせて実行  ★★★★★★★★★
                lightControl.RandomLightSwitcher();
                dynamicGenerateIntervalExecutor1.TryExecute();
                dynamicGenerateIntervalExecutor2.TryExecute();
                // =========================================================
            
                // クールダウン時間を更新
                lastSwitchTime = Time.time;
            
                // --- B. BPM計測 の処理 ---
                // 演出とは別に、より短い間隔でビートを検知してBPMを計算する
                if (currentTime > lastBpmBeatTime + bpmMeasureCooldown)
                {
                    CalculateBPM(currentTime);
                    lastBpmBeatTime = currentTime;

                    // 変化量の絶対値（Mathf.Abs）がしきい値を超えているか判定
                    if (Mathf.Abs(currentBpm - lastAppliedBpm) >= bpmChangeThreshold)
                    {
                        // 「最後に適用したBPM」を更新
                        lastAppliedBpm = currentBpm;
    
                        // 確認用ログ（必要に応じて）
                        // Debug.Log($"BPM Updated: {lastAppliedBpm}");
                    }
                }
            }
        }
        else
        {
            // ★★★★★ 3. 高音域のエネルギーを計算 ★★★★★
            highFrequencyEnergy = CalculateHighFrequencyEnergy();
            // 3. しきい値判定とスイッチング
            if (highFrequencyEnergy > highThreshold && Time.time > lastSwitchTime + switchCooldown)
            {
                float currentTime = Time.time;
            
                //Debug.Log($"🔊 低音域がしきい値 ({threshold:F2}) を超えました: {lowFrequencyEnergy:F2}");
            
                // =========================================================
                // ★★★★★★★★★  リズムに合わせて実行  ★★★★★★★★★
                lightControl.RandomLightSwitcher();
                dynamicGenerateIntervalExecutor1.TryExecute();
                dynamicGenerateIntervalExecutor2.TryExecute();
                // =========================================================
            
                // クールダウン時間を更新
                lastSwitchTime = Time.time;
            
                // --- B. BPM計測 の処理 ---
                // 演出とは別に、より短い間隔でビートを検知してBPMを計算する
                if (currentTime > lastBpmBeatTime + bpmMeasureCooldown)
                {
                    CalculateBPM(currentTime);
                    lastBpmBeatTime = currentTime;

                    // 変化量の絶対値（Mathf.Abs）がしきい値を超えているか判定
                    if (Mathf.Abs(currentBpm - lastAppliedBpm) >= bpmChangeThreshold)
                    {
                        // 「最後に適用したBPM」を更新
                        lastAppliedBpm = currentBpm;
    
                        // 確認用ログ（必要に応じて）
                        // Debug.Log($"BPM Updated: {lastAppliedBpm}");
                    }
                }
            }
        }
    }
    
    // --- カスタムメソッド ---
    
    /// <summary>
    /// ビート間隔からBPMを計算し、平均化して更新する
    /// </summary>
    private void CalculateBPM(float currentTime)
    {
        // 前回のビートからの経過時間を計算
        float interval = currentTime - lastBpmBeatTime;

        // ノイズ除去: 異常に長い間隔（例えば2秒以上音がない場合）は計算に含めない
        if (interval > 2.0f) 
        {
            beatIntervals.Clear(); // 履歴をリセット
            return;
        }

        // 履歴に追加
        beatIntervals.Add(interval);

        // 指定したサンプル数を超えたら古いデータを削除
        if (beatIntervals.Count > bpmSmoothingCount)
        {
            beatIntervals.RemoveAt(0);
        }

        // 平均間隔を計算
        float averageInterval = beatIntervals.Average();

        // 間隔からBPMに変換 (60秒 / 間隔)
        if (averageInterval > 0)
        {
            float calculatedBpm = 60.0f / averageInterval;
            
            // 整数に近い値に丸める（任意）
            currentBpm = Mathf.Round(calculatedBpm * 10f) / 10f;
            
            Debug.Log($"Detect BPM: {currentBpm}");
        }
    }

    /// <summary>
    /// 低音域のエネルギーの合計を計算する
    /// </summary>
    private float CalculateLowFrequencyEnergy()
    {
        float totalEnergy = 0f;
        // FFTの結果配列の最初から lowFrequencyBandMaxIndex までの要素（低周波数）を合計
        // ※ 1024 サンプルで 44100Hz の場合、インデックス1つあたり約 43Hz に対応します。
        //    インデックス50は 50 * 43Hz = 約 2150Hz までをカバーすることになります。
        //    純粋な低音域 (200Hz～500Hz) に絞るには、lowFrequencyBandMaxIndex を 5～12 程度に設定してください。
        for (int i = 0; i < lowFrequencyBandMaxIndex; i++)
        {
            // spectrumData の値は線形ではないため、対数的なスケールで扱うことが多いですが、
            // シンプルな制御では線形合計でも動作します。
            totalEnergy += spectrumData[i]; 
        }
        
        // 平均エネルギーを返す
        return totalEnergy / lowFrequencyBandMaxIndex;
    }
    
    // ★★★★★ 高音域を計算する新しい関数 ★★★★★
    /// <summary>
    /// 高音域のエネルギーの合計を計算する
    /// </summary>
    private float CalculateHighFrequencyEnergy()
    {
        float totalEnergy = 0f;
        int bandCount = highFrequencyBandMaxIndex - highFrequencyBandMinIndex;

        // エラーチェック（インデックスが妥当か、配列の範囲内か）
        if (bandCount <= 0 || highFrequencyBandMaxIndex > spectrumData.Length / 2) 
        {
            // Debug.LogError("高音域のインデックス設定が不正です。");
            return 0f;
        }

        for (int i = highFrequencyBandMinIndex; i < highFrequencyBandMaxIndex; i++)
        {
            // ※ 周波数データは線形ではないため、高いインデックスほど音が小さいです。
            // 　必要であれば、ここで強調処理（例: Math.Logや乗算）を追加できますが、
            // 　シンプルな合計で動作します。
            totalEnergy += spectrumData[i];
        }
        
        // 帯域の平均エネルギーを返す
        return totalEnergy / bandCount;
    }
    
    /// <summary>
    /// マイク入力を開始する設定
    /// </summary>
    private void StartMicInput()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSourceが設定されていません。マイク入力はできません。");
            return;
        }
        
        // マイクデバイス名を取得 (最初のデバイスを使用)
        string micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;

        if (micDevice == null)
        {
            Debug.LogError("マイクデバイスが見つかりません。");
            return;
        }

        // マイク入力を開始し、AudioSourceにクリップを流し込む
        audioSource.clip = Microphone.Start(micDevice, true, 10, AudioSettings.outputSampleRate);
        audioSource.loop = true; // ループ再生
        
        // マイク入力が準備できるまで少し待つ
        while (!(Microphone.GetPosition(micDevice) > 0)) { }
        
        audioSource.Play();
        Debug.Log($"マイク入力開始: デバイス '{micDevice}'");
    }
}
