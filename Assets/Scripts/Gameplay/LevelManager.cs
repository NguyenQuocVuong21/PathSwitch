using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PathDetector pathDetector;
    [SerializeField] private int currentLevel = 1;

    private const int FirstLevel = 1;
    private const int LastLevel = 5;

    private int currentMoves;
    private LevelData currentLevelData;
    private bool levelFinished;

    private const string SelectedLevelKey = "PathSwitch_SelectedLevel";
    private const string HighestUnlockedLevelKey = "PathSwitch_HighestUnlockedLevel";

    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnMovesChanged;
    public event Action OnLevelCompleted;
    public event Action OnLevelFailed;

    private void OnEnable() { if (pathDetector != null) pathDetector.OnPathChecked += OnPathChecked; }

    private void OnDisable() { if (pathDetector != null) pathDetector.OnPathChecked -= OnPathChecked; }

    private void Start()
    {
        currentLevel = Mathf.Clamp(
            PlayerPrefs.GetInt(SelectedLevelKey, FirstLevel),
            FirstLevel,
            LastLevel
        );

        LoadLevel(currentLevel);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.nKey.wasPressedThisFrame) NextLevel();
        if (Keyboard.current.rKey.wasPressedThisFrame) RetryLevel();
    }

    public void LoadLevel(int level)
    {
        if (gridManager == null || pathDetector == null)
        {
            Debug.LogError("LevelManager: GridManager or PathDetector is not assigned!", gameObject);
            return;
        }

        LevelData levelData = CreateLevelData(level);

        if (levelData == null)
        {
            Debug.LogError($"Level {level} does not exist!", gameObject);
            return;
        }

        if (!PuzzleValidator.ValidateLevel(levelData, out string validationError))
        {
            Debug.LogError($"Level {level} is invalid: {validationError}", gameObject);
            return;
        }

        currentLevel = level;
        currentLevelData = levelData;
        currentMoves = 0;
        levelFinished = false;

        Debug.Log($"===== LOAD LEVEL {currentLevel} =====");

        gridManager.SetGridDimensions(levelData.width, levelData.height);
        gridManager.ReloadGrid(levelData);

        ApplySolution();
        pathDetector.Initialize(levelData.start, levelData.goal);
        ShuffleLevel();

        Debug.Log($"Level {currentLevel} ready to play.");
        OnLevelChanged?.Invoke(currentLevel);
        OnMovesChanged?.Invoke(currentMoves, currentLevelData.moveLimit);
    }

    private LevelData CreateLevelData(int level)
    {
        switch (level)
        {
            case 1: return CreateLevel1();
            case 2: return CreateLevel2();
            case 3: return CreateLevel3();
            case 4: return CreateLevel4();
            case 5: return CreateLevel5();
            default: return null;
        }
    }

    private LevelData CreateLevel1()
    {
        LevelData level = new LevelData(3, 3, new Vector2Int(0, 1), new Vector2Int(2, 1), 0);
        InitDefaultTiles(level);
        ApplyPathToLevel(level, new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) });
        return level;
    }

    private LevelData CreateLevel2()
    {
        LevelData level = new LevelData(4, 4, new Vector2Int(0, 1), new Vector2Int(3, 2), 0);
        InitDefaultTiles(level);
        ApplyPathToLevel(level, new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(3, 2) });
        return level;
    }

    private LevelData CreateLevel3()
    {
        LevelData level = new LevelData(4, 4, new Vector2Int(0, 0), new Vector2Int(3, 3), 8);
        InitDefaultTiles(level);
        ApplyPathToLevel(level, new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(3, 3) });
        return level;
    }

    private LevelData CreateLevel4()
    {
        LevelData level = new LevelData(5, 5, new Vector2Int(0, 0), new Vector2Int(4, 4), 12);
        InitDefaultTiles(level);
        ApplyPathToLevel(level, new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2), new Vector2Int(4, 3), new Vector2Int(4, 4) });
        return level;
    }

    private LevelData CreateLevel5()
    {
        LevelData level = new LevelData(5, 5, new Vector2Int(0, 0), new Vector2Int(4, 4), 15);
        InitDefaultTiles(level);
        ApplyPathToLevel(level, new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(4, 4) });
        return level;
    }

    private static void InitDefaultTiles(LevelData level)
    {
        for (int x = 0; x < level.width; x++)
            for (int y = 0; y < level.height; y++)
            {
                level.tileTypes[x, y] = TileType.Straight;
                level.solutionRotations[x, y] = 0;
            }
    }

    private static void ApplyPathToLevel(LevelData level, List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) throw new ArgumentException("Path phải có ít nhất Start và Goal.");

        HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];

            if (cell.x < 0 || cell.x >= level.width || cell.y < 0 || cell.y >= level.height)
                throw new ArgumentException($"Path position {cell} nằm ngoài grid.");

            if (!usedPositions.Add(cell))
                throw new ArgumentException($"Path chứa ô trùng: {cell}");

            if (i > 0)
            {
                Vector2Int difference = path[i] - path[i - 1];

                if (Mathf.Abs(difference.x) + Mathf.Abs(difference.y) != 1)
                    throw new ArgumentException($"Path không liên tiếp giữa {path[i - 1]} và {path[i]}.");
            }
        }

        if (path[0] != level.start)
            throw new ArgumentException($"Path phải bắt đầu tại Start {level.start}.");

        if (path[path.Count - 1] != level.goal)
            throw new ArgumentException($"Path phải kết thúc tại Goal {level.goal}.");

        level.startDirection = path[1] - path[0];
        level.goalDirection = path[path.Count - 2] - path[path.Count - 1];

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            Vector2Int? connectionA = i > 0 ? path[i - 1] - cell : (Vector2Int?)null;
            Vector2Int? connectionB = i < path.Count - 1 ? path[i + 1] - cell : (Vector2Int?)null;

            if (!connectionA.HasValue || !connectionB.HasValue)
            {
                level.tileTypes[cell.x, cell.y] = TileType.Straight;
                level.solutionRotations[cell.x, cell.y] = 0;
                continue;
            }

            if (connectionA.Value == -connectionB.Value)
            {
                level.tileTypes[cell.x, cell.y] = TileType.Straight;
                level.solutionRotations[cell.x, cell.y] = GetStraightRotation(connectionB.Value);
            }
            else
            {
                level.tileTypes[cell.x, cell.y] = TileType.Corner;
                level.solutionRotations[cell.x, cell.y] = GetCornerRotation(connectionA.Value, connectionB.Value);
            }
        }
    }

    private static int GetStraightRotation(Vector2Int direction)
    {
        return direction == Vector2Int.up || direction == Vector2Int.down ? 0 : 1;
    }

    private static int GetCornerRotation(Vector2Int dirA, Vector2Int dirB)
    {
        bool hasUp = dirA == Vector2Int.up || dirB == Vector2Int.up;
        bool hasDown = dirA == Vector2Int.down || dirB == Vector2Int.down;
        bool hasLeft = dirA == Vector2Int.left || dirB == Vector2Int.left;
        bool hasRight = dirA == Vector2Int.right || dirB == Vector2Int.right;

        if (hasUp && hasRight) return 0;
        if (hasUp && hasLeft) return 1;
        if (hasDown && hasLeft) return 2;
        if (hasDown && hasRight) return 3;

        throw new ArgumentException($"Không thể tạo Corner hợp lệ từ {dirA} và {dirB}.");
    }

    private void ApplySolution()
    {
        if (currentLevelData == null) return;

        for (int x = 0; x < currentLevelData.width; x++)
            for (int y = 0; y < currentLevelData.height; y++)
                gridManager.SetTileConfiguration(new Vector2Int(x, y), currentLevelData.tileTypes[x, y], currentLevelData.solutionRotations[x, y]);
    }

    public void DebugApplySolutionForTesting()
    {
        if (currentLevelData != null)
            ApplySolution();
    }

    private void ShuffleLevel()
    {
        const int maxShuffleAttempts = 100;

        for (int attempt = 0; attempt < maxShuffleAttempts; attempt++)
        {
            ApplySolution();

            foreach (Tile tile in gridManager.TileGrid.Values)
            {
                if (tile == null) continue;

                int randomRotations = UnityEngine.Random.Range(1, 4);

                for (int i = 0; i < randomRotations; i++)
                    tile.RotateForShuffle();
            }

            currentMoves = 0;

            if (!pathDetector.CheckPath())
                return;
        }

        Debug.LogWarning($"Level {currentLevel}: Không thể tạo trạng thái shuffle hợp lệ sau {maxShuffleAttempts} lần. Giữ trạng thái hiện tại.");
    }

    private void OnPathChecked(bool solved)
    {
        if (levelFinished || currentLevelData == null) return;

        currentMoves++;
        OnMovesChanged?.Invoke(currentMoves, currentLevelData.moveLimit);

        if (solved)
        {
            CompleteLevel();
            return;
        }

        if (currentLevelData.moveLimit > 0 && currentMoves >= currentLevelData.moveLimit)
            FailLevel();
    }

    private void CompleteLevel()
    {
        if (levelFinished)
            return;

        levelFinished = true;

        UnlockNextLevel();

        Debug.Log($"LEVEL {currentLevel} COMPLETE!");

        gridManager.LockAllTiles();

        OnLevelCompleted?.Invoke();
    }

    private void UnlockNextLevel()
    {
        int highestUnlockedLevel = PlayerPrefs.GetInt(HighestUnlockedLevelKey, FirstLevel);

        if (currentLevel >= LastLevel)
            return;

        int nextLevel = currentLevel + 1;

        if (nextLevel > highestUnlockedLevel)
        {
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, nextLevel);
            PlayerPrefs.Save();

            Debug.Log($"LEVEL {nextLevel} UNLOCKED!");
        }
    }

    private void FailLevel()
    {
        if (levelFinished) return;

        levelFinished = true;
        Debug.Log($"LEVEL {currentLevel} FAILED!");
        gridManager.LockAllTiles();
        OnLevelFailed?.Invoke();
    }

    public bool DebugForceComplete()
    {
        if (levelFinished) return false;
        OnPathChecked(true);
        return levelFinished;
    }

    public bool DebugForceFail()
    {
        if (levelFinished || currentLevelData == null || currentLevelData.moveLimit <= 0) return false;

        while (!levelFinished && currentMoves < currentLevelData.moveLimit)
            OnPathChecked(false);

        return levelFinished;
    }

    public void NextLevel()
    {
        if (currentLevel >= LastLevel)
        {
            Debug.Log("ALL LEVELS COMPLETE!");
            return;
        }

        LoadLevel(currentLevel + 1);
    }

    public void RetryLevel()
    {
        LoadLevel(currentLevel);
    }

    [ContextMenu("Validate All Levels")]
    private void ValidateAllLevelsInEditor()
    {
        bool allValid = PuzzleValidator.ValidateLevels(CreateLevelData, FirstLevel, LastLevel, out string report);
        Debug.Log("===== VALIDATE ALL LEVELS =====\n" + report);
    }

    public int CurrentLevel => currentLevel;
    public int CurrentMoves => currentMoves;
    public int MoveLimit => currentLevelData != null ? currentLevelData.moveLimit : 0;
    public bool IsLevelFinished => levelFinished;
    public bool IsLastLevel => currentLevel >= LastLevel;
}