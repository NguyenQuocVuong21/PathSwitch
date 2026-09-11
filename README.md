# PathSwitch

A grid-based puzzle game prototype built with Unity and C#.

PathSwitch is a simple puzzle game where players rotate pipe tiles to connect a Start point to a Goal. The project focuses on gameplay programming, puzzle design, level design, and prototyping.

## 🎮 Gameplay

Players rotate pipe tiles to create a continuous path between the Start and Goal points.

Each level introduces a different grid layout and puzzle configuration. Later levels increase the challenge through larger grids and move limits.

### Core Loop

1. Observe the current puzzle layout.
2. Rotate pipe tiles.
3. Connect the Start to the Goal.
4. Complete the level before reaching the move limit.

## ✨ Features

- Grid-based pipe puzzle gameplay
- Rotatable straight and corner tiles
- Fixed Start and Goal points
- Automatic path validation
- BFS-based path detection
- Win and fail conditions
- Move limit system
- Level progression
- Sequential level unlocking
- Retry and next-level flow
- Mouse and touch input
- Responsive UI for Android
- 5 playable puzzle levels

## 🧩 Levels

| Level | Grid | Move Limit | Difficulty |
|------|------|------------|------------|
| 1 | 3 × 3 | Unlimited | Easy |
| 2 | 4 × 4 | Unlimited | Easy |
| 3 | 4 × 4 | 8 | Medium |
| 4 | 5 × 5 | 12 | Medium |
| 5 | 5 × 5 | 15 | Hard |

## 🛠️ Technologies

- Unity
- C#
- Unity Input System
- 2D Game Development
- BFS (Breadth-First Search)
- Android

## 🏗️ Project Architecture

The main gameplay systems are separated into several components:

- `Tile` — Handles tile type, rotation, connections, and player interaction.
- `GridManager` — Creates and manages the puzzle grid.
- `PathDetector` — Validates connections and detects whether the Start can reach the Goal.
- `LevelManager` — Handles level data, level progression, win/fail states, and unlocking.
- `LevelData` — Stores level configuration such as grid size, Start/Goal positions, solution rotations, and move limits.

### Gameplay Flow

```text
Player rotates tile
        ↓
Tile.OnTileRotated
        ↓
PathDetector
        ↓
BFS path validation
        ↓
Path found?
   ↙          ↘
 YES           NO
  ↓             ↓
Complete      Continue
 Level        Puzzle