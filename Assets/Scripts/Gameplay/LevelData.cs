using UnityEngine;

public class LevelData
{
    public int width;
    public int height;
    public Vector2Int start;
    public Vector2Int goal;
    public Vector2Int startDirection;
    public Vector2Int goalDirection;
    public TileType[,] tileTypes;
    public int[,] solutionRotations;
    public int moveLimit;

    public LevelData(int width, int height, Vector2Int start, Vector2Int goal, int moveLimit)
    {
        this.width = width;
        this.height = height;
        this.start = start;
        this.goal = goal;
        this.startDirection = Vector2Int.zero;
        this.goalDirection = Vector2Int.zero;
        this.moveLimit = moveLimit;
        tileTypes = new TileType[width, height];
        solutionRotations = new int[width, height];
    }
}