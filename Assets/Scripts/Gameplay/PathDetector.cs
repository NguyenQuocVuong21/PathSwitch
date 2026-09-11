using UnityEngine;
using System;
using System.Collections.Generic;

public class PathDetector : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Vector2Int startPosition = new Vector2Int(0, 1);
    [SerializeField] private Vector2Int goalPosition = new Vector2Int(2, 1);

    private readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    private readonly List<Vector2Int> currentPath = new List<Vector2Int>();
    private bool isPathValid;

    public event Action<bool> OnPathChecked;

    private void OnDisable()
    {
        UnsubscribeFromTileEvents();
    }

    public void Initialize(Vector2Int start, Vector2Int goal)
    {
        UnsubscribeFromTileEvents();

        startPosition = start;
        goalPosition = goal;

        if (!ValidateSetup())
            return;

        SubscribeToTileEvents();
        CheckPath();
    }

    private bool ValidateSetup()
    {
        if (gridManager == null)
        {
            Debug.LogError("PathDetector: GridManager is not assigned!", gameObject);
            return false;
        }

        if (!gridManager.IsValidGridPosition(startPosition))
        {
            Debug.LogError($"PathDetector: Invalid Start Position: {startPosition}", gameObject);
            return false;
        }

        if (!gridManager.IsValidGridPosition(goalPosition))
        {
            Debug.LogError($"PathDetector: Invalid Goal Position: {goalPosition}", gameObject);
            return false;
        }

        return true;
    }

    public bool CheckPath()
    {
        currentPath.Clear();
        isPathValid = false;

        if (gridManager == null)
        {
            Debug.LogError("PathDetector: GridManager is not assigned!");
            return false;
        }

        if (startPosition == goalPosition)
        {
            currentPath.Add(startPosition);
            isPathValid = true;
            return true;
        }

        isPathValid = FindPathBFS();

        if (isPathValid)
        {
            Debug.Log($"PathDetector: PATH SOLVED! Path length: {currentPath.Count}");
            LogPath();
        }

        return isPathValid;
    }

    private bool FindPathBFS()
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(startPosition);
        visited.Add(startPosition);
        parentMap[startPosition] = startPosition;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            Tile currentTile = gridManager.GetTileAt(current);

            if (currentTile == null)
                continue;

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (!gridManager.IsValidGridPosition(next) || visited.Contains(next))
                    continue;

                if (!CanConnect(current, next, direction))
                    continue;

                visited.Add(next);
                parentMap[next] = current;

                if (next == goalPosition)
                {
                    ReconstructPath(parentMap, next);
                    return true;
                }

                queue.Enqueue(next);
            }
        }

        return false;
    }

    private bool CanConnect(Vector2Int fromPosition, Vector2Int toPosition, Vector2Int direction)
    {
        Tile fromTile = gridManager.GetTileAt(fromPosition);
        Tile toTile = gridManager.GetTileAt(toPosition);

        if (fromTile == null || toTile == null)
            return false;

        return fromTile.HasConnection(direction) && toTile.HasConnection(-direction);
    }

    private void ReconstructPath(Dictionary<Vector2Int, Vector2Int> parentMap, Vector2Int goal)
    {
        currentPath.Clear();

        Vector2Int current = goal;

        while (current != startPosition)
        {
            currentPath.Add(current);
            current = parentMap[current];
        }

        currentPath.Add(startPosition);
        currentPath.Reverse();
    }

    private void SubscribeToTileEvents()
    {
        if (gridManager == null)
            return;

        foreach (Tile tile in gridManager.TileGrid.Values)
        {
            if (tile != null)
                tile.OnTileRotated += OnAnyTileRotated;
        }
    }

    private void UnsubscribeFromTileEvents()
    {
        if (gridManager == null)
            return;

        foreach (Tile tile in gridManager.TileGrid.Values)
        {
            if (tile != null)
                tile.OnTileRotated -= OnAnyTileRotated;
        }
    }

    private void OnAnyTileRotated()
    {
        bool solved = CheckPath();
        OnPathChecked?.Invoke(solved);
    }

    private void LogPath()
    {
        string pathString = "Path: ";

        for (int i = 0; i < currentPath.Count; i++)
        {
            pathString += currentPath[i];

            if (i < currentPath.Count - 1)
                pathString += " → ";
        }

        Debug.Log(pathString);
    }

    public void ResetPathDetector()
    {
        UnsubscribeFromTileEvents();
        currentPath.Clear();
        isPathValid = false;
        SubscribeToTileEvents();
        CheckPath();
    }

    public Vector2Int StartPosition => startPosition;
    public Vector2Int GoalPosition => goalPosition;
    public bool IsPathValid => isPathValid;
    public List<Vector2Int> CurrentPath => new List<Vector2Int>(currentPath);
}