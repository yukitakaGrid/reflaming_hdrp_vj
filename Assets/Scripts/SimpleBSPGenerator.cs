using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class SimpleBSPGenerator : MonoBehaviour
{
    public MidiController midiController;
    public CityVisualizer cityVisualizer;
    
    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;

    [Header("BSP Settings")]
    public int minNodeSize = 20; // 分割するエリアの最小サイズ
    public int maxIterations = 10; // 分割試行回数の上限（安全装置）
    
    public bool animateRoot = true;     // 自動で動かすかどうか
    public float animateSpeed = 2f;

    public float denseSplitThresholdArea = 500; //この面積以下だったら片方のみの分割開始
    public float splitRatio = 1.25f; // 縦横比の制限（1.25倍以上長ければ分割方向を強制）

    // データを保持するリスト
    private List<BSPNode> allNodes = new List<BSPNode>();
    private BSPNode rootNode;

    void Start()
    {
        GenerateBSP();
    }

    void Update()
    {
        // スペースキーで再生成
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateBSP();
        }

        if (animateRoot)
        {
            allNodes.Clear(); //アニメーション前に描画用データリセット
            ResizeNode(rootNode);
        }
    }

    public void GenerateBSP()
    {
        allNodes.Clear();

        // 1. ルートノード（マップ全体）を作成
        rootNode = new BSPNode(new Rect(0, 0, mapWidth, mapHeight));
        allNodes.Add(rootNode);

        // 2. 再帰的に分割を実行
        SplitNode(rootNode, 0);
        
        cityVisualizer.GenerateBuildings();
    }

    void SplitNode(BSPNode node, int iteration)
    {
        if (iteration >= maxIterations) return;

        // 分割を試みる
        // 縦に切るか横に切るかをランダム決定（ただし細長くなりすぎないように調整しても良い）
        bool splitVertical = Random.value > 0.5f;

        // もしノードが大きすぎる場合は、分割方向を強制する
        float width = node.rect.width;
        float height = node.rect.height;

        if (width > height * splitRatio)
        {
            // 横に長すぎる -> 強制的に縦に切る
            splitVertical = true;
        }
        else if (height > width * splitRatio)
        {
            // 縦に長すぎる -> 強制的に横に切る
            splitVertical = false;
        }
        else
        {
            // どちらでもない（正方形に近い） -> ランダム
            splitVertical = Random.value > 0.5f;
        }
        
        //データ構造として次迷子にならないように縦と横どっちで分割したか記録する
        node.splitVertical = splitVertical;

        // 分割できる十分な大きさがあるかチェック
        int maxSplitSize = (splitVertical ? (int)node.rect.width : (int)node.rect.height) - minNodeSize;
        
        // 分割するには小さすぎる場合、ここで終了（リーフノードになる）
        if (maxSplitSize <= minNodeSize) return; 

        // 1. まず 0.58 〜 0.86 のような「比率」を作る
        node.baseSplitRadian = Random.Range(-Mathf.PI/6, Mathf.PI/6);
        float ratio = Mathf.Sin(node.baseSplitRadian)*0.5f+0.5f; 

        // 2. 「比率」に「そのエリアの幅」を掛けて、「実際の切断位置」にする
        // splitVerticalがtrueならwidth、falseならheightを使う想定
        float currentSize = splitVertical ? node.rect.width : node.rect.height;

        // これで 「幅100 * 0.8 = 80メートル地点」 になる！
        var splitPoint = currentSize * ratio;

        BSPNode leftChild = null;
        BSPNode rightChild = null;

        if (splitVertical)
        {
            // 縦分割 (左右に分かれる)
            leftChild = new BSPNode(new Rect(node.rect.x, node.rect.y,splitPoint, node.rect.height));
            rightChild = new BSPNode(new Rect(node.rect.x + splitPoint, node.rect.y, node.rect.width - splitPoint, node.rect.height));
        }
        else
        {
            // 横分割 (上下に分かれる)
            leftChild = new BSPNode(new Rect(node.rect.x, node.rect.y, node.rect.width, splitPoint));
            rightChild = new BSPNode(new Rect(node.rect.x, node.rect.y + splitPoint, node.rect.width, node.rect.height - splitPoint));
        }

        // 親ノードに子を登録
        node.left = leftChild;
        node.right = rightChild;

        // リストに追加
        allNodes.Add(leftChild);
        allNodes.Add(rightChild);
        
        var currentArea = node.rect.width *  node.rect.height;
        if (currentArea > denseSplitThresholdArea)
        {
            // さらに両方子ノードを分割（再帰）
            SplitNode(leftChild, iteration + 1);
            SplitNode(rightChild, iteration + 1);
        }
        else
        {
            bool splitRight = Random.value > 0.5f;
            //TODO:分割しない方のnodeに正方形を生成するアルゴリズムを実装する
            if(splitRight) SplitNode(rightChild, iteration + 1);
            else SplitNode(leftChild, iteration + 1);
        }
    }
    
    void ResizeNode(BSPNode node)
    {
        allNodes.Add(node);
        
        // 1. 時間カウンターを現在の速度で更新
        // deltaTimeを使うことで、フレームレートに依存しない時間の流れを作る
        node.timeCounter += Time.deltaTime * midiController.GetControlValue(0) * animateSpeed;

        var ratio = Mathf.Sin(node.baseSplitRadian + node.timeCounter)*0.5f*node.moveRemit+0.5f;
        
        // 2. 「比率」に「そのエリアの幅」を掛けて、「実際の切断位置」にする
        // splitVerticalがtrueならwidth、falseならheightを使う想定
        float currentSize = node.splitVertical ? node.rect.width : node.rect.height;

        // これで 「幅100 * 0.8 = 80メートル地点」 になる！
        var splitPoint = currentSize * ratio;
        
        // 位置とサイズを再計算
        if (node.splitVertical)
        {
            // 【ここが修正ポイント】
            // node.left = new BSPNode(...) ではなく、
            // node.left.rect = new Rect(...) にする！
        
            if (node.left != null)
            {
                // 既存のノードの「rect」だけ書き換える
                node.left.rect = new Rect(
                    node.rect.x, 
                    node.rect.y, 
                    splitPoint, 
                    node.rect.height
                );
            }

            if (node.right != null)
            {
                node.right.rect = new Rect(
                    node.rect.x + splitPoint, 
                    node.rect.y, 
                    node.rect.width - splitPoint,
                    node.rect.height
                );
            }
        }
        else
        {
            if(node.left != null)
            {
                node.left.rect = new Rect(
                    node.rect.x, 
                    node.rect.y, 
                    node.rect.width, 
                    splitPoint
                );
            }
            
            if (node.right != null)
            {
                node.right.rect = new Rect(
                    node.rect.x, 
                    node.rect.y + splitPoint, 
                    node.rect.width, 
                    node.rect.height - splitPoint
                );
            }
        }
        
        if(node.left != null) ResizeNode(node.left);
        if(node.right != null) ResizeNode(node.right);
    }

    // デバッグ描画（Sceneビューで確認用）
    void OnDrawGizmos()
    {
        // 分割ライン（区画）の描画：白
        Gizmos.color = Color.white;
        foreach (var node in allNodes)
        {
            // 枠線を描画
            Gizmos.DrawWireCube(node.rect.center, node.rect.size);
        }
    }
    
    public List<BSPNode> GetAllNodes()
    {
        return allNodes;
    }
}

// BSPの各エリアを管理する単純なクラス
public class BSPNode
{
    public Rect rect;
    public BSPNode left;
    public BSPNode right;
    public float  baseSplitRadian;
    public bool splitVertical;
    public float moveRemit;
    // 時間を追跡するためのカウンター（スクリプトのフィールドとして宣言）
    public float timeCounter = 0f;

    public BSPNode(Rect r)
    {
        rect = r;
        moveRemit = Random.Range(-1.0f, 1.0f);
    }
}