using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public int column;
    public GameManager gameManager;
    
    void OnMouseDown()
    {
        if (gameManager != null)
        {
            gameManager.DropPiece(column);
        }
    }
}