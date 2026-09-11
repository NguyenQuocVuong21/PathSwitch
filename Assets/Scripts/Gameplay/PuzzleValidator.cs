using UnityEngine;
using System;
using System.Collections.Generic;

public static class PuzzleValidator
{
    private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    public static bool ValidateLevel(LevelData level, out string error)
    {
        error = string.Empty;
        if (level == null) { error = "LevelData is null."; return false; }
        if (level.width <= 0 || level.height <= 0) { error = $"Invalid grid size: {level.width}x{level.height}."; return false; }
        if (!IsValidPosition(level, level.start)) { error = $"Start position {level.start} is outside the grid."; return false; }
        if (!IsValidPosition(level, level.goal)) { error = $"Goal position {level.goal} is outside the grid."; return false; }
        if (level.start == level.goal) { error = "Start and Goal cannot occupy the same position."; return false; }
        if (!IsValidDirection(level.startDirection)) { error = $"Invalid Start direction: {level.startDirection}."; return false; }
        if (!IsValidDirection(level.goalDirection)) { error = $"Invalid Goal direction: {level.goalDirection}."; return false; }
        if (level.tileTypes == null || level.solutionRotations == null) { error = "Tile data is missing."; return false; }
        if (level.tileTypes.GetLength(0) != level.width || level.tileTypes.GetLength(1) != level.height) { error = "tileTypes dimensions do not match level dimensions."; return false; }
        if (level.solutionRotations.GetLength(0) != level.width || level.solutionRotations.GetLength(1) != level.height) { error = "solutionRotations dimensions do not match level dimensions."; return false; }
        if (!IsSolvable(level, out int pathLength)) { error = $"No valid path from {level.start} to {level.goal}."; return false; }
        return true;
    }

    public static bool IsSolvable(LevelData level, out int pathLength)
    {
        pathLength = 0;
        if (level == null || level.start == level.goal) return false;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(level.start);
        visited.Add(level.start);
        parentMap[level.start] = level.start;

        int[,] distances = new int[level.width, level.height];
        distances[level.start.x, level.start.y] = 1;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = current + direction;
                if (!IsValidPosition(level, next) || visited.Contains(next)) continue;
                if (!HasConnection(level, current, direction)) continue;
                if (!HasConnection(level, next, -direction)) continue;

                visited.Add(next);
                parentMap[next] = current;
                distances[next.x, next.y] = distances[current.x, current.y] + 1;

                if (next == level.goal)
                {
                    pathLength = distances[next.x, next.y];
                    return true;
                }

                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static bool HasConnection(LevelData level, Vector2Int position, Vector2Int direction)
    {
        if (position == level.start) return direction == level.startDirection;
        if (position == level.goal) return direction == level.goalDirection;
        return Tile.HasConnectionStatic(level.tileTypes[position.x, position.y], level.solutionRotations[position.x, position.y], direction);
    }

    private static bool IsValidPosition(LevelData level, Vector2Int position)
    {
        return position.x >= 0 && position.x < level.width && position.y >= 0 && position.y < level.height;
    }

    private static bool IsValidDirection(Vector2Int direction)
    {
        return direction == Vector2Int.up || direction == Vector2Int.down || direction == Vector2Int.left || direction == Vector2Int.right;
    }

    public static bool ValidateLevels(Func<int, LevelData> levelFactory, int firstLevel, int lastLevel, out string report)
    {
        report = string.Empty;
        bool allValid = true;

        for (int levelNumber = firstLevel; levelNumber <= lastLevel; levelNumber++)
        {
            LevelData level = levelFactory(levelNumber);

            if (level == null)
            {
                report += $"Level {levelNumber}: KHÔNG TỒN TẠI\n";
                allValid = false;
                continue;
            }

            if (ValidateLevel(level, out string error))
            {
                IsSolvable(level, out int pathLength);
                report += $"Level {levelNumber}: OK - path length = {pathLength}\n";
            }
            else
            {
                report += $"Level {levelNumber}: LỖI - {error}\n";
                allValid = false;
            }
        }

        report += allValid ? "==> TẤT CẢ LEVEL HỢP LỆ" : "==> CÓ LEVEL BỊ LỖI";
        return allValid;
    }
}