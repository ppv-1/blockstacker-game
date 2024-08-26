using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class Board : MonoBehaviour
{
    public TetrominoData[] tetrominos;
    public Piece activePiece { get; private set; }
    public Piece holdPiece { get; private set; }
    public int pieceCount;
    public TetrominoData[] bag;
    public Vector3Int[] holdCells;
    public int holdCellsWidth = 5;
    public int holdCellsHeight = 3;
    public Queue<TetrominoData> pieceQueue = new Queue<TetrominoData>();
    public Tilemap tilemap { get; private set; }
    public Vector3Int spawnPosition;
    public Vector3Int holdPosition = new Vector3Int(-8, -4, 0);
    public Vector3Int holdCellsPosition = new Vector3Int(-8, -4, 0);
    public Vector2Int boardSize = new Vector2Int(10, 22);
    public RectInt Bounds
    {
        get{
            Vector2Int position = new Vector2Int(-5, -10);
            return new RectInt(position, this.boardSize);

        }
    }

    private void Awake(){
        this.tilemap = GetComponentInChildren<Tilemap>();
        this.activePiece = GetComponentInChildren<Piece>();
        for(int i = 0; i < this.tetrominos.Length; i++){
            this.tetrominos[i].Initialize();
        }
        // for bag use
        for(int i = 0; i < this.bag.Length; i++){
            this.bag[i].Initialize();
        }
        // first bag
        RandomizeBag(bag);
        foreach(TetrominoData t in bag){
            this.pieceQueue.Enqueue(t);
        }
        GenerateHoldCells();
        

    }

    private void Start(){
        SpawnPiece();
    }

    private void Update(){
        if(this.pieceCount % 7 == 0){
            ManageQueue();
        }
    }

    static void RandomizeBag<T>(T[] array)
    {
        System.Random rng = new System.Random();
        int n = array.Length;

        // Fisher-Yates shuffle algorithm
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);  // Random index from 0 to i
            // Swap array[i] with array[j]
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    public void GenerateHoldCells()
    {
        holdCells = new Vector3Int[holdCellsWidth * holdCellsHeight];
        int index = 0;

        int halfWidth = holdCellsWidth / 2;
        int halfHeight = holdCellsHeight / 2;

        for (int x = -halfWidth; x <= halfWidth; x++)
        {
            for (int y = -halfHeight; y <= halfHeight; y++)
            {
                holdCells[index] = new Vector3Int(x, y, 0);
                index++;
            }
        }

    }

    public void ManageQueue(){
        RandomizeBag(bag);
        foreach(TetrominoData t in bag){
            this.pieceQueue.Enqueue(t);
        }
    }


    public void SpawnPiece(){        
        TetrominoData data = this.pieceQueue.Dequeue();
        this.activePiece.Initialize(this, spawnPosition, data);
        if(IsValidPosition(this.activePiece, this.spawnPosition)){
            Set(activePiece);
            this.pieceCount++;
        } else{
            GameOver();
        }

    }

    private void GameOver(){
        this.tilemap.ClearAllTiles();
    }

    public void Set(Piece piece){
        for(int i = 0; i < piece.cells.Length; i++){
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            this.tilemap.SetTile(tilePosition, piece.data.tile);
        }
    }
    // clears tile from the function
    public void Clear(Piece piece){
        for(int i = 0; i < piece.cells.Length; i++){
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            this.tilemap.SetTile(tilePosition, null);
        }
    }

    public void ClearHold(){
        Debug.Log(holdCells.Length);
        for(int i = 0; i < holdCells.Length; i++){
            Vector3Int tilePosition = holdCells[i] + spawnPosition + holdPosition;

            Debug.Log(tilePosition);
            this.tilemap.SetTile(tilePosition, null);
        }
    }

    public void HoldPiece(){
        if(this.holdPiece != null){
            ClearHold();
        }
        this.holdPiece = this.activePiece;

        // set the hold piece in the border
        for(int i = 0; i < holdPiece.cells.Length; i++){
            Vector3Int tilePosition = holdPiece.cells[i] + spawnPosition + holdPosition;
            this.tilemap.SetTile(tilePosition, holdPiece.data.tile);
        }
        Clear(this.activePiece);

    }

    public bool IsValidPosition(Piece piece, Vector3Int position){
        RectInt bounds = this.Bounds;
        for(int i = 0; i < piece.cells.Length; i++){
            Vector3Int tilePosition = piece.cells[i] + position;
            if(!bounds.Contains((Vector2Int)tilePosition)){
                return false;
            }
            // if tilemap already has a tile at that posiiton
            if(this.tilemap.HasTile(tilePosition)){
                return false;
            }
            
        }
        return true;
    }

    public void ClearLines(){

        RectInt bounds = this.Bounds;
        int row = bounds.yMin;
        while(row < bounds.yMax){
            if(IsLineFull(row)){
                LineClear(row);
            } else{
                row++;
            }
        }

    }

    private bool IsLineFull(int row){
        RectInt bounds = this.Bounds;
        for(int col = bounds.xMin; col < bounds.xMax; col++){
            Vector3Int position = new Vector3Int(col, row, 0);

            if(!this.tilemap.HasTile(position)){
                return false;
            }
        }
        return true;
    }

    public void LineClear(int row){
        RectInt bounds = this.Bounds;
        for(int col = bounds.xMin; col < bounds.xMax; col++){
            Vector3Int position = new Vector3Int(col, row, 0);
            this.tilemap.SetTile(position, null);
            
        }

        while(row < bounds.yMax){
            for(int col = bounds.xMin; col < bounds.xMax; col++){
                Vector3Int position = new Vector3Int(col, row + 1, 0);
                TileBase above = this.tilemap.GetTile(position);
                position = new Vector3Int(col, row, 0);
                this.tilemap.SetTile(position, above);
            }
            row++;
        }
    }

}
