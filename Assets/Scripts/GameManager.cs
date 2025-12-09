using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 6;
    public int columns = 7;
    public float spacing = 1.1f;
    
    [Header("Prefabs")]
    public GameObject piecePrefab;
    public GameObject boardSlotPrefab;
    
    [Header("Colors")]
    public Color player1Color = Color.red;
    public Color player2Color = Color.yellow;
    public Color boardColor = Color.blue;
    
    [Header("UI")]
    public Text turnText;
    public Text winnerText;
    public Button restartButton;
    
    [Header("Animation Settings")]
    public float dropHeight = 8f;
    public float dropSpeed = 10f;
    
    private int[,] board;
    private int currentPlayer = 1;
    private bool gameOver = false;
    private GameObject[,] pieceObjects;
    private bool isDropping = false;
    
    void Start()
    {
        board = new int[rows, columns];
        pieceObjects = new GameObject[rows, columns];
        CreateBoard();
        UpdateTurnText();
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
            restartButton.gameObject.SetActive(false);
        }
        
        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }
    }
    
    void CreateBoard()
    {
        // ボードの中心位置を計算
        float boardWidth = (columns - 1) * spacing;
        float boardHeight = (rows - 1) * spacing;
        Vector3 startPos = new Vector3(-boardWidth / 2, -boardHeight / 2, 0);
        
        // ボードのスロットを作成
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 position = startPos + new Vector3(col * spacing, row * spacing, 0);
                GameObject slot = Instantiate(boardSlotPrefab, position, Quaternion.identity);
                slot.transform.parent = transform;
                slot.name = $"Slot_{row}_{col}";
                
                // スロットの色を設定
                SpriteRenderer sr = slot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = boardColor;
                }
                
                // クリック可能にする
                BoardSlot slotScript = slot.AddComponent<BoardSlot>();
                slotScript.column = col;
                slotScript.gameManager = this;
            }
        }
    }
    
    public void DropPiece(int column)
    {
        if (gameOver || isDropping) return;
        
        // その列で一番下の空いている行を探す
        int row = GetLowestEmptyRow(column);
        
        if (row == -1)
        {
            Debug.Log("この列は満杯です！");
            return;
        }
        
        // ピースを物理演算で配置
        StartCoroutine(DropPieceWithPhysics(row, column));
    }
    
    int GetLowestEmptyRow(int column)
    {
        for (int row = 0; row < rows; row++)
        {
            if (board[row, column] == 0)
            {
                return row;
            }
        }
        return -1;
    }
    
    IEnumerator DropPieceWithPhysics(int targetRow, int column)
    {
        isDropping = true;
        board[targetRow, column] = currentPlayer;
        
        // ピースの最終位置を計算
        float boardWidth = (columns - 1) * spacing;
        float boardHeight = (rows - 1) * spacing;
        Vector3 startPos = new Vector3(-boardWidth / 2, -boardHeight / 2, 0);
        Vector3 targetPosition = startPos + new Vector3(column * spacing, targetRow * spacing, -0.1f);
        
        // ピースをドロップ位置に作成
        Vector3 dropPosition = new Vector3(targetPosition.x, targetPosition.y + dropHeight, targetPosition.z);
        GameObject piece = Instantiate(piecePrefab, dropPosition, Quaternion.identity);
        piece.transform.parent = transform;
        piece.name = $"Piece_P{currentPlayer}_{targetRow}_{column}";
        
        // SpriteRendererの色を設定
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = (currentPlayer == 1) ? player1Color : player2Color;
            sr.sortingOrder = 1; // ボードより前面に表示
        }
        
        pieceObjects[targetRow, column] = piece;
        
        // スムーズに落下（アニメーション）
        float currentY = dropPosition.y;
        while (currentY > targetPosition.y)
        {
            currentY -= dropSpeed * Time.deltaTime;
            if (currentY < targetPosition.y)
            {
                currentY = targetPosition.y;
            }
            piece.transform.position = new Vector3(targetPosition.x, currentY, targetPosition.z);
            yield return null;
        }
        
        // ぴったり最終位置に配置
        piece.transform.position = targetPosition;
        
        isDropping = false;
        
        // 勝利判定
        if (CheckWin(targetRow, column))
        {
            gameOver = true;
            ShowWinner();
            yield break;
        }
        
        // 引き分け判定
        if (IsBoardFull())
        {
            gameOver = true;
            ShowDraw();
            yield break;
        }
        
        // プレイヤー交代
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        UpdateTurnText();
    }
    
    bool CheckWin(int row, int column)
    {
        // 横方向チェック
        if (CheckDirection(row, column, 0, 1)) return true;
        // 縦方向チェック
        if (CheckDirection(row, column, 1, 0)) return true;
        // 斜め方向チェック（右上）
        if (CheckDirection(row, column, 1, 1)) return true;
        // 斜め方向チェック（右下）
        if (CheckDirection(row, column, -1, 1)) return true;
        
        return false;
    }
    
    bool CheckDirection(int row, int column, int dirRow, int dirCol)
    {
        int count = 1;
        
        // 正方向にカウント
        count += CountInDirection(row, column, dirRow, dirCol);
        // 逆方向にカウント
        count += CountInDirection(row, column, -dirRow, -dirCol);
        
        return count >= 4;
    }
    
    int CountInDirection(int row, int column, int dirRow, int dirCol)
    {
        int count = 0;
        int r = row + dirRow;
        int c = column + dirCol;
        
        while (r >= 0 && r < rows && c >= 0 && c < columns)
        {
            if (board[r, c] == currentPlayer)
            {
                count++;
                r += dirRow;
                c += dirCol;
            }
            else
            {
                break;
            }
        }
        
        return count;
    }
    
    bool IsBoardFull()
    {
        for (int col = 0; col < columns; col++)
        {
            if (board[rows - 1, col] == 0)
            {
                return false;
            }
        }
        return true;
    }
    
    void UpdateTurnText()
    {
        if (turnText != null)
        {
            turnText.text = $"プレイヤー {currentPlayer} のターン";
            turnText.color = (currentPlayer == 1) ? player1Color : player2Color;
        }
    }
    
    void ShowWinner()
    {
        if (winnerText != null)
        {
            winnerText.text = $"プレイヤー {currentPlayer} の勝利！";
            winnerText.color = (currentPlayer == 1) ? player1Color : player2Color;
            winnerText.gameObject.SetActive(true);
        }
        
        if (turnText != null)
        {
            turnText.gameObject.SetActive(false);
        }
        
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }
    
    void ShowDraw()
    {
        if (winnerText != null)
        {
            winnerText.text = "引き分け！";
            winnerText.color = Color.white;
            winnerText.gameObject.SetActive(true);
        }
        
        if (turnText != null)
        {
            turnText.gameObject.SetActive(false);
        }
        
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }
    
    public void RestartGame()
    {
        // ボードをリセット
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                board[row, col] = 0;
                if (pieceObjects[row, col] != null)
                {
                    Destroy(pieceObjects[row, col]);
                    pieceObjects[row, col] = null;
                }
            }
        }
        
        currentPlayer = 1;
        gameOver = false;
        isDropping = false;
        
        UpdateTurnText();
        
        if (turnText != null)
        {
            turnText.gameObject.SetActive(true);
        }
        
        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }
        
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }
    }
}