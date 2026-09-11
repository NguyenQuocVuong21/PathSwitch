using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int gridWidth = 3;
    [SerializeField] private int gridHeight = 3;
    [SerializeField] private float tileSpacing = 1f;
    [SerializeField] private Tile straightPrefab;
    [SerializeField] private Tile cornerPrefab;
    [SerializeField] private Tile startPrefab;
    [SerializeField] private Tile goalPrefab;

    [Header("Auto Grid Display")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float horizontalPadding = 0.12f;
    [SerializeField] private float verticalPadding = 0.22f;
    [SerializeField] private float maxDisplayScale = 1f;
    [SerializeField] private float minDisplayScale = 0.55f;

    private readonly Dictionary<Vector2Int, Tile> tileGrid = new Dictionary<Vector2Int, Tile>();
    private Vector3 gridOrigin;
    private bool isInitialized;
    private bool useAutoInitialize = true;
    private LevelData currentLevelData;
    private float displayScale = 1f;
    private float displayTileSpacing = 1f;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (useAutoInitialize)
        {
            InitializeGridDimensions();
            InitializeGrid();
        }
    }

    public void SetGridDimensions(int width, int height)
    {
        useAutoInitialize = false;
        gridWidth = width;
        gridHeight = height;
    }

    private void InitializeGridDimensions()
    {
        CalculateDisplayScale();

        displayTileSpacing = tileSpacing * displayScale;

        float centerOffsetX = -(gridWidth - 1) * displayTileSpacing * 0.5f;
        float centerOffsetY = -(gridHeight - 1) * displayTileSpacing * 0.5f;

        gridOrigin = transform.position + new Vector3(centerOffsetX, centerOffsetY, 0f);
    }

    private void CalculateDisplayScale()
    {
        displayScale = maxDisplayScale;

        if (mainCamera == null)
            return;

        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        float availableWidth = cameraWidth * (1f - horizontalPadding * 2f);
        float availableHeight = cameraHeight * (1f - verticalPadding * 2f);

        float gridWidthWorld = Mathf.Max(1f, gridWidth * tileSpacing);
        float gridHeightWorld = Mathf.Max(1f, gridHeight * tileSpacing);

        float widthScale = availableWidth / gridWidthWorld;
        float heightScale = availableHeight / gridHeightWorld;

        displayScale = Mathf.Min(widthScale, heightScale);
        displayScale = Mathf.Clamp(displayScale, minDisplayScale, maxDisplayScale);
    }

    private void InitializeGrid()
    {
        ClearGrid();
        InitializeGridDimensions();

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                CreateTileAt(new Vector2Int(x, y));

        isInitialized = true;

        Debug.Log($"Grid initialized: {gridWidth}x{gridHeight}, Display Scale: {displayScale:F2}");
    }

    private void ClearGrid()
    {
        if (tileGrid.Count == 0)
            return;

        List<Vector2Int> keysToRemove = new List<Vector2Int>(tileGrid.Keys);

        foreach (Vector2Int key in keysToRemove)
        {
            Tile tile = tileGrid[key];

            if (tile != null && tile.gameObject != null)
                Destroy(tile.gameObject);

            tileGrid.Remove(key);
        }

#if UNITY_EDITOR
        Debug.Log("Grid cleared");
#endif
    }

    private void CreateTileAt(Vector2Int gridPosition)
    {
        Tile prefab = GetPrefabForPosition(gridPosition);

        if (prefab == null)
        {
            Debug.LogError($"GridManager: Prefab không được gán cho ô {gridPosition}!", gameObject);
            return;
        }

        Tile newTile = Instantiate(prefab, GetWorldPosition(gridPosition), Quaternion.identity, transform);
        newTile.name = GetTileName(gridPosition);
        newTile.transform.localScale = Vector3.one * displayScale;

        ConfigureTile(newTile, gridPosition);
        tileGrid[gridPosition] = newTile;
    }

    private Tile GetPrefabForPosition(Vector2Int gridPosition)
    {
        if (currentLevelData == null)
            return straightPrefab;

        if (gridPosition == currentLevelData.start)
            return startPrefab;

        if (gridPosition == currentLevelData.goal)
            return goalPrefab;

        TileType tileType = currentLevelData.tileTypes[gridPosition.x, gridPosition.y];

        return tileType == TileType.Corner ? cornerPrefab : straightPrefab;
    }

    private string GetTileName(Vector2Int gridPosition)
    {
        if (currentLevelData != null)
        {
            if (gridPosition == currentLevelData.start)
                return $"Start_{gridPosition.x}_{gridPosition.y}";

            if (gridPosition == currentLevelData.goal)
                return $"Goal_{gridPosition.x}_{gridPosition.y}";
        }

        return $"Tile_{gridPosition.x}_{gridPosition.y}";
    }

    private void ConfigureTile(Tile tile, Vector2Int gridPosition)
    {
        if (currentLevelData == null)
            return;

        if (gridPosition == currentLevelData.start)
        {
            tile.SetEndpoint(TileKind.Start, currentLevelData.startDirection);
            return;
        }

        if (gridPosition == currentLevelData.goal)
        {
            tile.SetEndpoint(TileKind.Goal, currentLevelData.goalDirection);
            return;
        }

        tile.SetInitialState(
            currentLevelData.tileTypes[gridPosition.x, gridPosition.y],
            currentLevelData.solutionRotations[gridPosition.x, gridPosition.y]
        );
    }

    public void SetTileConfiguration(Vector2Int gridPosition, TileType tileType, int rotationState)
    {
        Tile tile = GetTileAt(gridPosition);

        if (tile == null)
        {
            Debug.LogWarning($"GridManager: Tile at {gridPosition} not found!");
            return;
        }

        if (tile.TileKind == TileKind.Start || tile.TileKind == TileKind.Goal)
            return;

        tile.SetInitialState(tileType, rotationState);
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return gridOrigin + new Vector3(
            gridPosition.x * displayTileSpacing,
            gridPosition.y * displayTileSpacing,
            0f
        );
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 relativePosition = worldPosition - gridOrigin;

        int x = Mathf.RoundToInt(relativePosition.x / displayTileSpacing);
        int y = Mathf.RoundToInt(relativePosition.y / displayTileSpacing);

        return new Vector2Int(x, y);
    }

    public Tile GetTileAt(Vector2Int gridPosition)
    {
        return tileGrid.TryGetValue(gridPosition, out Tile tile) ? tile : null;
    }

    public Tile GetNeighbor(Vector2Int gridPosition, Vector2Int direction)
    {
        return GetTileAt(gridPosition + direction);
    }

    public Tile GetTileAbove(Vector2Int gridPosition) => GetNeighbor(gridPosition, Vector2Int.up);
    public Tile GetTileBelow(Vector2Int gridPosition) => GetNeighbor(gridPosition, Vector2Int.down);
    public Tile GetTileLeft(Vector2Int gridPosition) => GetNeighbor(gridPosition, Vector2Int.left);
    public Tile GetTileRight(Vector2Int gridPosition) => GetNeighbor(gridPosition, Vector2Int.right);

    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        return gridPosition.x >= 0 &&
               gridPosition.x < gridWidth &&
               gridPosition.y >= 0 &&
               gridPosition.y < gridHeight;
    }

    public void ResetLevel()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Grid is not initialized yet");
            return;
        }

        foreach (KeyValuePair<Vector2Int, Tile> pair in tileGrid)
        {
            Tile tile = pair.Value;

            if (tile == null)
                continue;

            if (currentLevelData != null && pair.Key == currentLevelData.start)
            {
                tile.SetEndpoint(TileKind.Start, currentLevelData.startDirection);
                continue;
            }

            if (currentLevelData != null && pair.Key == currentLevelData.goal)
            {
                tile.SetEndpoint(TileKind.Goal, currentLevelData.goalDirection);
                continue;
            }

            tile.ResetTile();
            tile.UnlockTile();
        }

        Debug.Log("Grid reset");
    }

    public void LockAllTiles()
    {
        foreach (Tile tile in tileGrid.Values)
            if (tile != null)
                tile.LockTile();
    }

    public void UnlockAllTiles()
    {
        foreach (Tile tile in tileGrid.Values)
            if (tile != null)
                tile.UnlockTile();
    }

    public void ReloadGrid(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("GridManager: LevelData is null!", gameObject);
            return;
        }

        if (levelData.width <= 0 || levelData.height <= 0)
        {
            Debug.LogError(
                $"GridManager: Invalid grid size {levelData.width}x{levelData.height}!",
                gameObject
            );
            return;
        }

        currentLevelData = levelData;
        gridWidth = levelData.width;
        gridHeight = levelData.height;
        useAutoInitialize = false;

        InitializeGrid();
    }

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public Dictionary<Vector2Int, Tile> TileGrid => tileGrid;
    public float DisplayScale => displayScale;

    private void OnDrawGizmosSelected()
    {
        float centerOffsetX = -(gridWidth - 1) * tileSpacing * 0.5f;
        float centerOffsetY = -(gridHeight - 1) * tileSpacing * 0.5f;

        Vector3 gizmoOrigin = transform.position +
                              new Vector3(centerOffsetX, centerOffsetY, 0f);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 startLine = gizmoOrigin +
                                new Vector3(
                                    (x - 0.5f) * tileSpacing,
                                    -0.5f * tileSpacing,
                                    0f
                                );

            Vector3 endLine = gizmoOrigin +
                              new Vector3(
                                  (x - 0.5f) * tileSpacing,
                                  (gridHeight - 0.5f) * tileSpacing,
                                  0f
                              );

            Gizmos.DrawLine(startLine, endLine);
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 startLine = gizmoOrigin +
                                new Vector3(
                                    -0.5f * tileSpacing,
                                    (y - 0.5f) * tileSpacing,
                                    0f
                                );

            Vector3 endLine = gizmoOrigin +
                              new Vector3(
                                  (gridWidth - 0.5f) * tileSpacing,
                                  (y - 0.5f) * tileSpacing,
                                  0f
                              );

            Gizmos.DrawLine(startLine, endLine);
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                Gizmos.DrawSphere(
                    gizmoOrigin + new Vector3(
                        x * tileSpacing,
                        y * tileSpacing,
                        0f
                    ),
                    0.1f
                );
    }
}