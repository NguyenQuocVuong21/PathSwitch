using UnityEngine;
using System.Collections.Generic;

public class PathSwitchTester : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PathDetector pathDetector;

    private int levelsPassed;
    private int levelsFailed;

    [ContextMenu("Run Core Gameplay Test")]
    public void RunCoreGameplayTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("PathSwitchTester: Core Gameplay Test chỉ được chạy trong Play Mode.");
            return;
        }

        if (!ValidateReferences()) return;

        int originalLevel = levelManager.CurrentLevel;
        levelsPassed = 0;
        levelsFailed = 0;

        Debug.Log("========== PATHSWITCH CORE GAMEPLAY TEST ==========");

        try
        {
            TestAllLevels();
        }
        finally
        {
            levelManager.LoadLevel(originalLevel);
            PrintFinalSummary();
        }
    }

    private void TestAllLevels()
    {
        for (int level = 1; level <= 5; level++)
        {
            bool passed = TestLevel(level);

            if (passed)
                levelsPassed++;
            else
                levelsFailed++;
        }
    }

    private bool TestLevel(int level)
    {
        Debug.Log($"----- Testing Level {level} -----");

        levelManager.LoadLevel(level);

        bool passed = true;

        passed &= Assert(levelManager.CurrentLevel == level, $"Level {level}: level loaded correctly.");
        passed &= Assert(!levelManager.IsLevelFinished, $"Level {level}: level starts unfinished.");
        passed &= Assert(!pathDetector.IsPathValid, $"Level {level}: shuffled level starts unsolved.");
        passed &= TestMoveCounting(level);
        passed &= TestSolution(level);
        passed &= TestCompleteState(level);
        passed &= TestFailState(level);

        Debug.Log(passed ? $"Level {level}: PASS" : $"Level {level}: FAIL");
        Debug.Log("--------------------------------");

        return passed;
    }

    private bool TestMoveCounting(int level)
    {
        int movesBefore = levelManager.CurrentMoves;
        Tile testTile = FindRotatableTile();

        if (testTile == null)
        {
            Debug.LogError($"[FAIL] Level {level}: no rotatable tile found.");
            return false;
        }

        testTile.DebugRotate();

        int movesAfter = levelManager.CurrentMoves;

        return Assert(movesAfter == movesBefore + 1, $"Level {level}: one rotation increments exactly one move.");
    }

    private bool TestSolution(int level)
    {
        levelManager.RetryLevel();

        if (!Assert(!levelManager.IsLevelFinished, $"Level {level}: retry resets state before solution test."))
            return false;

        levelManager.DebugApplySolutionForTesting();

        bool pathSolved = pathDetector.CheckPath();
        bool pathExists = pathDetector.CurrentPath.Count > 0;
        int pathCount = CountPaths(pathDetector.StartPosition, pathDetector.GoalPosition, 2);

        bool passed = true;

        passed &= Assert(pathSolved, $"Level {level}: solution connects Start to Goal.");
        passed &= Assert(pathExists, $"Level {level}: solution contains a valid path.");
        passed &= Assert(pathCount == 1, $"Level {level}: solution has exactly one Start-to-Goal path.");

        if (pathCount > 1)
            Debug.LogWarning($"Level {level}: multiple routes detected in solution configuration.");

        return passed;
    }

    private bool TestCompleteState(int level)
    {
        levelManager.RetryLevel();

        bool passed = true;

        passed &= Assert(!levelManager.IsLevelFinished, $"Level {level}: retry resets finished state.");

        bool completed = levelManager.DebugForceComplete();

        passed &= Assert(completed, $"Level {level}: force complete succeeds.");
        passed &= Assert(levelManager.IsLevelFinished, $"Level {level}: complete state activated.");
        passed &= Assert(AllTilesLocked(), $"Level {level}: tiles locked after completion.");

        return passed;
    }

    private bool TestFailState(int level)
    {
        levelManager.RetryLevel();

        if (levelManager.MoveLimit <= 0)
        {
            Debug.Log($"Level {level}: move limit disabled, fail test skipped.");
            return true;
        }

        bool failed = levelManager.DebugForceFail();

        bool passed = true;

        passed &= Assert(failed, $"Level {level}: fail state activated.");
        passed &= Assert(levelManager.IsLevelFinished, $"Level {level}: failed level marked finished.");
        passed &= Assert(levelManager.CurrentMoves == levelManager.MoveLimit, $"Level {level}: move count reaches move limit.");
        passed &= Assert(AllTilesLocked(), $"Level {level}: tiles locked after failure.");

        return passed;
    }

    private int CountPaths(Vector2Int start, Vector2Int goal, int maxPaths)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        int pathCount = 0;

        CountPathsRecursive(start, goal, visited, ref pathCount, maxPaths);

        return pathCount;
    }

    private void CountPathsRecursive(Vector2Int current, Vector2Int goal, HashSet<Vector2Int> visited, ref int pathCount, int maxPaths)
    {
        if (pathCount >= maxPaths) return;

        if (current == goal)
        {
            pathCount++;
            return;
        }

        Tile currentTile = gridManager.GetTileAt(current);

        if (currentTile == null) return;

        visited.Add(current);

        foreach (Vector2Int direction in Directions)
        {
            Vector2Int next = current + direction;

            if (!gridManager.IsValidGridPosition(next) || visited.Contains(next))
                continue;

            Tile nextTile = gridManager.GetTileAt(next);

            if (nextTile == null)
                continue;

            if (!currentTile.HasConnection(direction))
                continue;

            if (!nextTile.HasConnection(-direction))
                continue;

            CountPathsRecursive(next, goal, visited, ref pathCount, maxPaths);

            if (pathCount >= maxPaths)
                break;
        }

        visited.Remove(current);
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private Tile FindRotatableTile()
    {
        foreach (Tile tile in gridManager.TileGrid.Values)
        {
            if (tile != null && tile.CanRotate)
                return tile;
        }

        return null;
    }

    private bool AllTilesLocked()
    {
        foreach (Tile tile in gridManager.TileGrid.Values)
        {
            if (tile != null && tile.CanRotate)
                return false;
        }

        return true;
    }

    private bool ValidateReferences()
    {
        if (levelManager == null)
        {
            Debug.LogError("PathSwitchTester: LevelManager is not assigned.");
            return false;
        }

        if (gridManager == null)
        {
            Debug.LogError("PathSwitchTester: GridManager is not assigned.");
            return false;
        }

        if (pathDetector == null)
        {
            Debug.LogError("PathSwitchTester: PathDetector is not assigned.");
            return false;
        }

        return true;
    }

    private bool Assert(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log($"[PASS] {message}");
            return true;
        }

        Debug.LogError($"[FAIL] {message}");
        return false;
    }

    private void PrintFinalSummary()
    {
        int totalLevels = levelsPassed + levelsFailed;

        Debug.Log("========== PATHSWITCH TEST SUMMARY ==========");
        Debug.Log($"Levels tested : {totalLevels}");
        Debug.Log($"Levels passed : {levelsPassed}");
        Debug.Log($"Levels failed : {levelsFailed}");

        Debug.Log(levelsFailed == 0
            ? "========== ALL CORE TESTS PASSED =========="
            : "========== CORE TEST FAILED ==========");
    }
}