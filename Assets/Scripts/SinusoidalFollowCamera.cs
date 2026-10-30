using UnityEngine;

public class SinusoidalFollowCamera : MonoBehaviour
{
    // --- インスペクターから設定する変数 ---
    
    // 追従するターゲットオブジェクト
    public Transform target;
    
    // ターゲットとの水平距離（XZ平面上の距離）
    public float distance = 5.0f; 
    
    // サインカーブの中心となる高さ（ターゲットY座標からのオフセット）
    public float baseHeightOffset = 2.0f;
    
    // サインカーブの振幅（高さ変動の最大幅）
    public float amplitude = 1.0f; 
    
    // 高さ変動の速さ（周波数）。値が大きいほど速く振動します。
    public float frequency = 1.0f; 
    
    // カメラの滑らかな動きの速さ（値が小さいほど滑らかに遅延します）
    public float smoothSpeed = 5.0f; 
    
    // --- 内部変数 ---
    
    // 時間を追跡するためのカウンター
    private float timeCounter = 0f;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("ターゲットが設定されていません。インスペクターで設定してください。");
            return;
        }

        // 1. サインカーブに基づいたY軸の位置計算
        
        // timeCounterを時間によって進める
        timeCounter += Time.deltaTime * frequency;
        
        // サインカーブの値 [-1.0, 1.0] に振幅をかけて変動幅を決定
        float sinMovement = Mathf.Sin(timeCounter) * amplitude;
        
        // ターゲットY座標にベースオフセットと変動幅を加算し、最終的な目標Y座標を決定
        float targetY = target.position.y + baseHeightOffset + sinMovement;


        // 2. カメラの目標位置（ターゲットとの距離を保ちつつ）計算
        
        // カメラからターゲットへのベクトルを取得
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        
        // 水平距離を維持するため、現在のカメラの水平位置を基準に計算
        // XZ平面の角度は維持し、Yだけを後から変更する方式が最も簡単です
        Vector3 currentPosition = transform.position;
        
        // 目標の水平位置 (ターゲットのXZ平面上の位置から距離分戻った位置)
        // ターゲットのXZ位置をベースにする
        Vector3 targetXZPosition = target.position;
        
        // カメラの現在の水平方向（ターゲットの方向から距離を考慮）
        // カメラの現在の位置からターゲットのXZ平面上の方向へ距離分戻す
        Vector3 desiredPosition = targetXZPosition - directionToTarget * distance;
        
        // Y座標は計算したサインカーブの値を使用
        desiredPosition.y = targetY;


        // 3. カメラを目標位置へ滑らかに移動
        
        // Lerp（線形補間）を使って滑らかな追従を実現
        Vector3 smoothedPosition = Vector3.Lerp(currentPosition, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;


        // 4. ターゲットの方向を常に向く（LookAt）
        
        // 常にターゲットの中心を見るように回転
        transform.LookAt(target);
    }
}