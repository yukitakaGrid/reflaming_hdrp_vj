using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraSwitcher : MonoBehaviour
{
    // インスペクターから設定するためのカメラのリスト
    // 必ず4つのカメラをドラッグ＆ドロップしてください
    public List<GameObject> cameras;
    
    // カメラを切り替える間隔（秒）-> 【待機時間】として利用
    public float waitInterval = 5.0f; 
    
    // 💡 カメラが移動にかける時間（秒）-> 【アニメーション時間】として利用
    [Tooltip("カメラが次の位置に移動するのにかける時間")]
    public float animationTime = 1.0f;

    // カメラを切り替える間隔（秒）
    public float switchIntervalSpeed = 1.0f;

    // 現在アクティブなカメラのインデックス
    private int currentCameraIndex = 0;
    private int nextCameraIndex = 1;
    
    public MidiController midi;
    
    // カメラの初期位置と回転を保存するための構造体
    private struct CameraState
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    // カメラGameObjectと初期状態を対応付ける辞書
    private Dictionary<GameObject, CameraState> initialStates = new Dictionary<GameObject, CameraState>();
    
    // 現在実行中のコルーチンを保持する変数 (重複開始防止用)
    private Coroutine switchCoroutine;

    // Start関数でコルーチンを開始
    void Start()
    {
        // --- 初期位置の保存 ---
        foreach (GameObject cam in cameras)
        {
            // 初期状態を保存
            initialStates.Add(cam, new CameraState 
            {
                position = cam.transform.position,
                rotation = cam.transform.rotation
            });
        }
        // ----------------------

        // 全カメラを非アクティブにしてから、最初のカメラをアクティブにする
        DisableAllCameras();
        SetCameraActive(currentCameraIndex, true);

        // 時間切り替えのためのコルーチンを開始
        StartCoroutine(SwitchCameraCoroutine());
    }

    void Update()
    {
        waitInterval = (1-midi.GetControlValue(7)) * switchIntervalSpeed;
    }
    
    /// <summary>
    /// オブジェクトがアクティブになった瞬間に実行される処理
    /// </summary>
    void OnEnable()
    {
        // ユーザーが要求した初期化処理を OnEnable で実行
        // 1. 全カメラを非アクティブにする
        DisableAllCameras(); 

        // 2. 最初のカメラ (インデックス 0) をアクティブにする
        SetCameraActive(0, true); 

        // 3. 時間切り替えのためのコルーチンを開始
        // 既にコルーチンが実行中の場合は停止し、新しいものを開始します
        if (switchCoroutine != null)
        {
            StopCoroutine(SwitchCameraCoroutine());
        }
        switchCoroutine = StartCoroutine(SwitchCameraCoroutine());
    }

    /// <summary>
    /// オブジェクトが非アクティブになった瞬間に実行される処理
    /// </summary>
    void OnDisable()
    {
        // コルーチンが残っている場合は停止
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }
    }

    // すべてのカメラのGameObjectを非アクティブにする関数
    private void DisableAllCameras()
    {
        foreach (GameObject cam in cameras)
        {
            // nullチェックも行う
            if (cam != null)
            {
                cam.SetActive(false);
            }
        }
    }

    // 特定のインデックスのカメラのアクティブ/非アクティブを設定する関数
    private void SetCameraActive(int index, bool isActive)
    {
        if (index >= 0 && index < cameras.Count && cameras[index] != null)
        {
            cameras[index].SetActive(isActive);
        }
    }

    IEnumerator SwitchCameraCoroutine()
    {
        while (true)
        {
            // --- 1. アニメーション前の準備 ---

            GameObject currentCam = cameras[currentCameraIndex];

            // 次のカメラのインデックスを計算
            nextCameraIndex = (currentCameraIndex + 1) % cameras.Count;
            GameObject nextCam = cameras[nextCameraIndex];

            // ターゲット（次のカメラ）の初期位置を取得
            Vector3 startPos = currentCam.transform.position;
            Quaternion startRot = currentCam.transform.rotation;
            Vector3 endPos = initialStates[nextCam].position;
            Quaternion endRot = initialStates[nextCam].rotation;

            float elapsedTime = 0f;

            // 💡 MIDI制御された時間を待機時間として使用 (既存のロジック)
            float currentWaitTime = Mathf.Max(0.1f, waitInterval); // switchInterval を waitInterval に読み替えてください

            // --- 2. 移動アニメーションループ ---

            // animationTime が 0 より大きい場合のみアニメーションを行う
            if (animationTime > 0f)
            {
                while (elapsedTime < animationTime) // 💡 アニメーション時間だけループ
                {
                    // 0から1へ変化する割合を計算
                    float t = elapsedTime / animationTime;

                    // Easing関数（オプション）: スムーズな移動のため
                    // t = Mathf.SmoothStep(0f, 1f, t);

                    // 現在のカメラの位置と回転を補間
                    currentCam.transform.position = Vector3.Lerp(startPos, endPos, t);
                    currentCam.transform.rotation = Quaternion.Lerp(startRot, endRot, t);

                    elapsedTime += Time.deltaTime;
                    yield return null; // 1フレーム待機
                }
            }

            // 💡 アニメーション終了時の処理（カメラの切り替え） ---

            // アニメーションが完了しきっていない場合、強制的にターゲット位置へ移動させる
            currentCam.transform.position = endPos;
            currentCam.transform.rotation = endRot;

            // 1. 現在のカメラを非アクティブにする
            SetCameraActive(currentCameraIndex, false);

            // 2. ***元の位置に戻す処理***
            CameraState originalState = initialStates[currentCam];
            currentCam.transform.position = originalState.position;
            currentCam.transform.rotation = originalState.rotation;

            // 3. 次のカメラをアクティブにする（次のサイクルでアニメーションの開始点になる）
            currentCameraIndex = nextCameraIndex;
            SetCameraActive(currentCameraIndex, true);

            // --- 3. 待機 ---

            // 💡 アニメーション後に、MIDI制御された時間だけ待機する
            yield return new WaitForSeconds(currentWaitTime);
        }
    }
}