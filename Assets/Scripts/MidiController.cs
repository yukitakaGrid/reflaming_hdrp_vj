using UnityEngine;
using UnityEngine.InputSystem;

public class MidiController : MonoBehaviour
{
    // 33個のInput ActionをInspectorで設定するための配列
    // Input Action Asset内で、33個のMIDIコントロール（例: CC1～CC33）に対応するアクションを定義し、ここに割り当ててください。
    [Tooltip("33個のInput Action Asset内のアクションを順番に割り当ててください。")]
    [SerializeField]
    private InputAction[] controlActions = new InputAction[33];

    // 33個のMIDIコントロールの現在の値 (0.0f～1.0f) を格納する配列
    private float[] controlValues = new float[33];

    // OnDisableでイベント登録を安全に解除するために、デリゲートを格納する配列
    private System.Action<InputAction.CallbackContext>[] controlDelegates = new System.Action<InputAction.CallbackContext>[33];

    void Awake()
    {
        // 値の配列を初期化（全て0に）
        for (int i = 0; i < controlValues.Length; i++)
        {
            controlValues[i] = 0f;
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < controlActions.Length; i++)
        {
            if (controlActions[i] != null)
            {
                controlActions[i].Enable();
                int index = i; // ⚠️ ラムダ関数内でインデックスを正しくキャプチャするためのローカル変数

                // イベントハンドラー（ラムダ関数）を生成し、デリゲート配列に格納
                // アクションが実行されるたび、OnControlMoved関数に自身のインデックスを渡す
                controlDelegates[i] = context => OnControlMoved(context, index);

                // performedイベントにハンドラーを登録
                controlActions[i].performed += controlDelegates[i];
                
                controlActions[i].canceled += controlDelegates[i];
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < controlActions.Length; i++)
        {
            if (controlActions[i] != null && controlDelegates[i] != null)
            {
                // イベント登録を解除し、アクションを無効化
                controlActions[i].performed -= controlDelegates[i];
                controlActions[i].Disable();
            }
        }
    }

    /// <summary>
    /// MIDIコントロールが操作されたときに呼び出されます。
    /// </summary>
    /// <param name="context">入力コンテキスト</param>
    /// <param name="index">操作されたコントロールの配列インデックス (0～32)</param>
    private void OnControlMoved(InputAction.CallbackContext context, int index)
    {
        // 連続的な値（0.0～1.0）を読み込み、対応する配列の要素を更新
        controlValues[index] = context.ReadValue<float>();
        
        // 確認のためのデバッグ出力
        // Debug.Log($"MIDI Control (Index {index}) の値: {controlValues[index]}");
        
        // 例: 値が変化した際のカスタム処理をここに記述
        // if (index == 0) { ... }
        
        // 🚨 以前の回答で作成したカメラ切り替え処理を呼び出す例
        // (このスクリプトとカメラ切り替えスクリプトが同一GameObjectにアタッチされている場合)
        // if (index == 0)
        // {
        //     GetComponent<InstantCameraSwitcher>()?.SwitchCameraByParameter(controlValues[0]);
        // }
    }

    /// <summary>
    /// 指定されたインデックスのMIDIコントロールの値を取得します。
    /// </summary>
    /// <param name="index">取得したいコントロールの配列インデックス (0～32)</param>
    /// <returns>現在のコントロールの値 (0.0f～1.0f)</returns>
    public float GetControlValue(int index)
    {
        if (index >= 0 && index < controlValues.Length)
        {
            return controlValues[index];
        }
        
        // 無効なインデックスの場合は0を返し、コンソールに警告を出す
        Debug.LogWarning($"GetControlValue: 無効なインデックスが指定されました: {index}。0～32の範囲で指定してください。");
        return 0f;
    }
}