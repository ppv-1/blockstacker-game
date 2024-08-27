using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int position { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public int rotationIndex { get; private set; }

    [Header("controls")]
    public float dasDelay = 0.15f;
    public float moveRepeatDelay = 0.05f; // Delay between repeated moves

    private float dasTimer = 0f;
    private float moveRepeatTimer = 0f;
    private bool isMovingLeft = false;
    private bool isMovingRight = false;
    [SerializeField] private float softDropSpeed = 0.1f; 
    private float softDropTimer;

    public float stepDelay = 1f;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float lockTime;

    public void Initialize(Board board, Vector3Int position, TetrominoData data){
        this.board = board;
        this.position = position;
        this.data = data;
        this.rotationIndex = 0;
        this.stepTime = Time.time + this.stepDelay;
        this.lockTime = 0f;

        if(this.cells == null){
            this.cells = new Vector3Int[data.cells.Length];
        }

        for(int i = 0; i < data.cells.Length; i++){
            this.cells[i] = (Vector3Int) data.cells[i];
        }

    }

    private void Update(){
        this.board.Clear(this);

        this.lockTime += Time.deltaTime;

        if(Input.GetKeyDown(KeyCode.X)){
            Rotate(1);
        } else if(Input.GetKeyDown(KeyCode.Z)){
            Rotate(-1);
        }


        // Initial move to the left
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move(Vector2Int.left);
            isMovingLeft = true;
            isMovingRight = false;
            dasTimer = Time.time + dasDelay;
            moveRepeatTimer = dasTimer;
        }
        // Initial move to the right
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move(Vector2Int.right);
            isMovingRight = true;
            isMovingLeft = false;
            dasTimer = Time.time + dasDelay;
            moveRepeatTimer = dasTimer;
        }

        // Auto-shift to the left
        if (Input.GetKey(KeyCode.LeftArrow) && isMovingLeft)
        {
            if (Time.time >= dasTimer && Time.time >= moveRepeatTimer)
            {
                Move(Vector2Int.left);
                moveRepeatTimer = Time.time + moveRepeatDelay;
            }
        }
        // Auto-shift to the right
        else if (Input.GetKey(KeyCode.RightArrow) && isMovingRight)
        {
            if (Time.time >= dasTimer && Time.time >= moveRepeatTimer)
            {
                Move(Vector2Int.right);
                moveRepeatTimer = Time.time + moveRepeatDelay;
            }
        }

        // Reset movement flags when keys are released
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            isMovingLeft = false;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            isMovingRight = false;
        }

        if (Input.GetKeyDown(KeyCode.R)){
            this.board.GameOver();
        }

        // if (Input.GetKeyDown(KeyCode.LeftArrow)){
        //     Move(Vector2Int.left);
        // } else if (Input.GetKeyDown(KeyCode.RightArrow)){
        //     Move(Vector2Int.right);
        // }


        // Immediate drop on key press
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Move(Vector2Int.down);
            softDropTimer = Time.time + softDropSpeed; // Start the timer for continuous drop
        }

        // Continuous drop while holding the down arrow key
        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (Time.time >= softDropTimer)
            {
                Move(Vector2Int.down);
                softDropTimer = Time.time + softDropSpeed; // Reset the timer for the next drop
            }
        }
        // if(Input.GetKeyDown(KeyCode.DownArrow)){
        //     Move(Vector2Int.down);
        // }

        if(Input.GetKeyDown(KeyCode.Space)){
            HardDrop();
        }

        if(Input.GetKeyDown(KeyCode.LeftShift)){
            Hold();
        }

        if(Time.time >= this.stepTime){
            Step();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        this.board.Set(this);
    }

    private void Hold(){
        //if user pressed hold, set isHeld to true, so user cannot press hold again, 
        // once the piece locks into place, allow user to press hold again
        // special case for first hold
        if(this.board.isHeld == false && this.board.holdPieceData.cells == null){
            this.board.HoldPiece();
            this.board.SpawnPiece();
            this.board.isHeld = true;
        } else if (this.board.isHeld == false){
            this.board.HoldPiece();
            this.board.SpawnHoldPiece();
            this.board.isHeld = true;
        }
    }

    private void Step(){
        this.stepTime = Time.time + this.stepDelay;
        Move(Vector2Int.down);
        if(this.lockTime >= this.lockDelay){
            Lock();
        }
    }

    public void Lock(){
        this.board.Set(this);
        this.board.ClearLines();
        this.board.SpawnPiece();
        this.board.isHeld = false;
        this.board.pieceCount++;
    }

    public void HardDrop()
    {
        while(Move(Vector2Int.down)){
            continue;
        }

        Lock();
    }

    private bool Move(Vector2Int translation){
        Vector3Int newPosition = this.position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;
        bool valid = this.board.IsValidPosition(this, newPosition);
        if(valid){
            this.position = newPosition;
            this.lockTime = 0f;
        }
        return valid;
    }

    private void Rotate(int direction){
        int originalRotation = this.rotationIndex;
        this.rotationIndex = Wrap(this.rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        if(!TestWallKicks(this.rotationIndex, direction)){
            this.rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }       


    }

    private void ApplyRotationMatrix(int direction){
         for(int i = 0; i < this.cells.Length; i++){
            // no integer here because I, O piece needs its rotation pivot point offset by half a unit, need floating points
            Vector3 cell = this.cells[i];
            // new coordinates after rotated
            int x, y;
            switch(this.data.tetromino){
                case Tetromino.I:
                case Tetromino.O:
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                    break;
                default:
                    x = Mathf.RoundToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                    break;

            }
            this.cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection){

        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);
        for(int i = 0; i < this.data.wallKicks.GetLength(1); i++){
            Vector2Int translation = this.data.wallKicks[wallKickIndex, i];
            if(Move(translation)){
                return true;
            }
        }
        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection){
        int wallKickIndex = rotationIndex * 2;
        if(rotationDirection < 0){
            wallKickIndex--;
        }
        return Wrap(wallKickIndex, 0, this.data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max){
        if (input < min){
            return max = (min - input) % (max - min);
        } else{
            return min + (input - min) % (max - min);
        }
    }

}
