using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int position { get; private set; }
    public Vector3Int[] cells { get; private set; }

    public void Initialize(Board board, Vector3Int position, TetrominoData data){
        this.board = board;
        this.position = position;
        this.data = data;

        if(this.cells == null){
            this.cells = new Vector3Int[data.cells.Length];
        }

        for(int i = 0; i < data.cells.Length; i++){
            this.cells[i] = (Vector3Int) data.cells[i];
        }

    }

    private void Update(){
        this.board.Clear(this);
        if (Input.GetKeyDown(KeyCode.LeftArrow)){
            Move(Vector2Int.left);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)){
            Move(Vector2Int.right);
        }

        if(Input.GetKeyDown(KeyCode.DownArrow)){
            Move(Vector2Int.down);
        }

        if(Input.GetKeyDown(KeyCode.Space)){
            HardDrop();
        }

        this.board.Set(this);
    }

    public void HardDrop()
    {
        while(Move(Vector2Int.down)){
            continue;
        }
    }

    private bool Move(Vector2Int translation){
        Vector3Int newPosition = this.position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;
        bool valid = this.board.isValidPosition(this, newPosition);
        if(valid){
            this.position = newPosition;
        }
        return valid;
    }

}
