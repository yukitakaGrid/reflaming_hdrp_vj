using UnityEngine;

public class InstantCameraSwitcher : MonoBehaviour
{
    [Tooltip("切り替えたいカメラのGameObjectを2つ設定してください。")]
    [SerializeField]
    private GameObject[] cameras = new GameObject[2]; // 💡 配列サイズを 2 に変更

    private int activeCameraIndex = -1; // 現在アクティブなカメラのインデックス

    // 範囲を分割するための定数 (中央の境界線 1/2 = 0.5)
    private const float Threshold = 0.5f; // 💡 閾値を 0.5 に変更
    
    // MIDI制御は外部から行うため、Start() の検索ロジックは省略
    public MidiController midi; 

    void Start()
    {
        if (cameras.Length != 2) // 💡 検査するカメラの数を 2 に変更
        {
            Debug.LogError("カメラはちょうど2つ設定してください。現在の設定数: " + cameras.Length);
            return;
        }
        
        // 初期状態で全てのカメラを非アクティブにする
        foreach (GameObject cam in cameras)
        {
            if (cam != null)
            {
                cam.SetActive(false);
            }
        }
        
        // 初期状態として t=0.0f での切り替えを実行
        SwitchCameraByParameter(0); 
    }
    
    // ※ 外部から値を更新するため、Update() の自動呼び出しは削除または調整が必要です
    
    void Update()
    {
        SwitchCameraByParameter(midi.GetControlValue(5)); 
    }
    

    /// <summary>
    /// 0.0fから1.0fのパラメータ t に応じてカメラを瞬時に切り替えます。
    /// </summary>
    /// <param name="t">カメラ切り替えパラメータ (0.0f ～ 1.0f)。</param>
    public void SwitchCameraByParameter(float t)
    {
        if (cameras.Length != 2) return; // 💡 カメラ数が 2 かどうかをチェック

        // パラメータ t をクランプ (0.0f ～ 1.0f の範囲に制限)
        t = Mathf.Clamp01(t);

        int newIndex;

        // 💡 0.0f ～ 0.5f の区間
        if (t < Threshold)
        {
            newIndex = 0; // カメラ1
        }
        // 💡 0.5f ～ 1.0f の区間
        else
        {
            newIndex = 1; // カメラ2
        }

        // 現在のカメラと切り替え先のカメラが異なる場合にのみ処理を実行
        if (newIndex != activeCameraIndex)
        {
            // 古いカメラを非アクティブにする
            if (activeCameraIndex != -1 && cameras[activeCameraIndex] != null)
            {
                cameras[activeCameraIndex].SetActive(false);
            }

            // 新しいカメラをアクティブにする
            if (cameras[newIndex] != null)
            {
                cameras[newIndex].SetActive(true);
                activeCameraIndex = newIndex;
            }
        }
    }
    
    /* 💡 MIDI制御を組み込む場合の例（外部クラスが呼び出す場合は不要）
    private void FindMidiController()
    {
        if (midi == null)
        {
            midi = FindObjectOfType<MidiController>();
        }
    }
    */
}