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
    public Button vsPlayerButton;
    public Button vsComputerButton;
    public Dropdown difficultyDropdown;
    
    [Header("Animation Settings")]
    public float dropHeight = 8f;
    public float dropSpeed = 10f;
    
    [Header("AI Settings")]
    public float aiThinkTime = 0.5f;
    
    private int[,] board;
    private int currentPlayer = 1;
    private bool gameOver = false;
    private GameObject[,] pieceObjects;
    private bool isDropping = false;
    
    // AI settings
    private bool isVsComputer = false;
    private int aiDifficulty = 1; // 0 = Easy, 1 = Medium, 2 = Hard
    private bool gameStarted = false;
    
    void Start()
    {
        board = new int[rows, columns];
        pieceObjects = new GameObject[rows, columns];
        
        // Show game mode selection
        ShowGameModeSelection();
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
            restartButton.gameObject.SetActive(false);
        }
        
        if (vsPlayerButton != null)
        {
            vsPlayerButton.onClick.AddListener(() => StartGame(false));
        }
        
        if (vsComputerButton != null)
        {
            vsComputerButton.onClick.AddListener(() => StartGame(true));
        }
        
        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }
        
        if (turnText != null)
        {
            turnText.gameObject.SetActive(false);
        }
    }
    
    void ShowGameModeSelection()
    {
        if (vsPlayerButton != null)
            vsPlayerButton.gameObject.SetActive(true);
        if (vsComputerButton != null)
            vsComputerButton.gameObject.SetActive(true);
        if (difficultyDropdown != null)
            difficultyDropdown.gameObject.SetActive(true);
    }
    
    void HideGameModeSelection()
    {
        if (vsPlayerButton != null)
            vsPlayerButton.gameObject.SetActive(false);
        if (vsComputerButton != null)
            vsComputerButton.gameObject.SetActive(false);
        if (difficultyDropdown != null)
            difficultyDropdown.gameObject.SetActive(false);
    }
    
    void StartGame(bool vsComputer)
    {
        isVsComputer = vsComputer;
        if (difficultyDropdown != null)
        {
            aiDifficulty = difficultyDropdown.value;
        }
        
        HideGameModeSelection();
        CreateBoard();
        gameStarted = true;
        UpdateTurnText();
        
        if (turnText != null)
        {
            turnText.gameObject.SetActive(true);
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
        if (!gameStarted || gameOver || isDropping) return;
        
        // コンピューターのターンの場合は人間の入力を無視
        if (isVsComputer && currentPlayer == 2) return;
        
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
        
        // AIのターン
        if (isVsComputer && currentPlayer == 2 && !gameOver)
        {
            StartCoroutine(AIMove());
        }
    }
    
    IEnumerator AIMove()
    {
        // 少し待ってから手を指す
        yield return new WaitForSeconds(aiThinkTime);
        
        int bestColumn = GetBestMove();
        int row = GetLowestEmptyRow(bestColumn);
        
        if (row != -1)
        {
            StartCoroutine(DropPieceWithPhysics(row, bestColumn));
        }
    }
    
    int GetBestMove()
    {
        switch (aiDifficulty)
        {
            case 0: // Easy - ランダムに選択
                return GetRandomMove();
            case 1: // Medium - 簡単な戦略
                return GetMediumMove();
            case 2: // Hard - Minimaxアルゴリズム
                return GetMinimaxMove();
            default:
                return GetRandomMove();
        }
    }
    
    int GetRandomMove()
    {
        System.Collections.Generic.List<int> validColumns = new System.Collections.Generic.List<int>();
        
        for (int col = 0; col < columns; col++)
        {
            if (GetLowestEmptyRow(col) != -1)
            {
                validColumns.Add(col);
            }
        }
        
        if (validColumns.Count > 0)
        {
            return validColumns[Random.Range(0, validColumns.Count)];
        }
        
        return 0;
    }
    
    int GetMediumMove()
    {
        // 1. 勝てる手があれば勝つ
        for (int col = 0; col < columns; col++)
        {
            int row = GetLowestEmptyRow(col);
            if (row != -1)
            {
                board[row, col] = 2;
                bool canWin = CheckWin(row, col);
                board[row, col] = 0;
                
                if (canWin)
                    return col;
            }
        }
        
        // 2. 相手の勝ちを阻止
        for (int col = 0; col < columns; col++)
        {
            int row = GetLowestEmptyRow(col);
            if (row != -1)
            {
                board[row, col] = 1;
                bool opponentCanWin = CheckWin(row, col);
                board[row, col] = 0;
                
                if (opponentCanWin)
                    return col;
            }
        }
        
        // 3. 中央を優先
        int center = columns / 2;
        if (GetLowestEmptyRow(center) != -1)
        {
            return center;
        }
        
        // 4. ランダム
        return GetRandomMove();
    }
    
    int GetMinimaxMove()
    {
        int bestScore = int.MinValue;
        int bestColumn = 0;
        int depth = 4; // 探索深さ
        
        for (int col = 0; col < columns; col++)
        {
            int row = GetLowestEmptyRow(col);
            if (row != -1)
            {
                // 手を試す
                board[row, col] = 2;
                int score = Minimax(depth - 1, false, int.MinValue, int.MaxValue);
                board[row, col] = 0;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestColumn = col;
                }
            }
        }
        
        return bestColumn;
    }
    
    int Minimax(int depth, bool isMaximizing, int alpha, int beta)
    {
        // 終了条件チェック
        int winner = CheckWinner();
        if (winner == 2) return 1000 + depth;
        if (winner == 1) return -1000 - depth;
        if (IsBoardFull() || depth == 0) return EvaluateBoard();
        
        if (isMaximizing)
        {
            int maxScore = int.MinValue;
            for (int col = 0; col < columns; col++)
            {
                int row = GetLowestEmptyRow(col);
                if (row != -1)
                {
                    board[row, col] = 2;
                    int score = Minimax(depth - 1, false, alpha, beta);
                    board[row, col] = 0;
                    maxScore = Mathf.Max(maxScore, score);
                    alpha = Mathf.Max(alpha, score);
                    if (beta <= alpha) break;
                }
            }
            return maxScore;
        }
        else
        {
            int minScore = int.MaxValue;
            for (int col = 0; col < columns; col++)
            {
                int row = GetLowestEmptyRow(col);
                if (row != -1)
                {
                    board[row, col] = 1;
                    int score = Minimax(depth - 1, true, alpha, beta);
                    board[row, col] = 0;
                    minScore = Mathf.Min(minScore, score);
                    beta = Mathf.Min(beta, score);
                    if (beta <= alpha) break;
                }
            }
            return minScore;
        }
    }
    
    int EvaluateBoard()
    {
        int score = 0;
        
        // 中央列を優先
        int centerColumn = columns / 2;
        int centerCount = 0;
        for (int row = 0; row < rows; row++)
        {
            if (board[row, centerColumn] == 2)
                centerCount++;
        }
        score += centerCount * 3;
        
        // すべての方向で評価
        score += EvaluateDirection(0, 1);  // 横
        score += EvaluateDirection(1, 0);  // 縦
        score += EvaluateDirection(1, 1);  // 斜め右上
        score += EvaluateDirection(1, -1); // 斜め右下
        
        return score;
    }
    
    int EvaluateDirection(int deltaRow, int deltaCol)
    {
        int score = 0;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                score += EvaluateWindow(row, col, deltaRow, deltaCol, 2);
                score -= EvaluateWindow(row, col, deltaRow, deltaCol, 1);
            }
        }
        
        return score;
    }
    
    int EvaluateWindow(int startRow, int startCol, int deltaRow, int deltaCol, int player)
    {
        int playerCount = 0;
        int emptyCount = 0;
        
        for (int i = 0; i < 4; i++)
        {
            int row = startRow + i * deltaRow;
            int col = startCol + i * deltaCol;
            
            if (row < 0 || row >= rows || col < 0 || col >= columns)
                return 0;
            
            if (board[row, col] == player)
                playerCount++;
            else if (board[row, col] == 0)
                emptyCount++;
        }
        
        if (playerCount == 4) return 100;
        if (playerCount == 3 && emptyCount == 1) return 5;
        if (playerCount == 2 && emptyCount == 2) return 2;
        
        return 0;
    }
    
    int CheckWinner()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (board[row, col] != 0)
                {
                    int tempPlayer = currentPlayer;
                    currentPlayer = board[row, col];
                    
                    if (CheckWin(row, col))
                    {
                        currentPlayer = tempPlayer;
                        return board[row, col];
                    }
                    
                    currentPlayer = tempPlayer;
                }
            }
        }
        return 0;
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
            if (isVsComputer)
            {
                if (currentPlayer == 1)
                {
                    turnText.text = "あなたのターン";
                    turnText.color = player1Color;
                }
                else
                {
                    turnText.text = "コンピューターのターン";
                    turnText.color = player2Color;
                }
            }
            else
            {
                turnText.text = $"プレイヤー {currentPlayer} のターン";
                turnText.color = (currentPlayer == 1) ? player1Color : player2Color;
            }
        }
    }
    
    void ShowWinner()
    {
        if (winnerText != null)
        {
            if (isVsComputer)
            {
                if (currentPlayer == 1)
                {
                    winnerText.text = "あなたの勝ち！";
                }
                else
                {
                    winnerText.text = "コンピューターの勝ち！";
                }
            }
            else
            {
                winnerText.text = $"プレイヤー {currentPlayer} の勝利！";
            }
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
        // ボードをクリア
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
        
        // ボードスロットを削除
        BoardSlot[] slots = FindObjectsOfType<BoardSlot>();
        foreach (BoardSlot slot in slots)
        {
            Destroy(slot.gameObject);
        }
        
        currentPlayer = 1;
        gameOver = false;
        isDropping = false;
        gameStarted = false;
        
        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }
        
        if (turnText != null)
        {
            turnText.gameObject.SetActive(false);
        }
        
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
        }
        
        // ゲームモード選択に戻る
        ShowGameModeSelection();
    }
}