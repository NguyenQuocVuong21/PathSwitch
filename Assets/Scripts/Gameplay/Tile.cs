using UnityEngine;
using UnityEngine.InputSystem;
using System;

public enum TileType { Straight, Corner }
public enum TileKind { Pipe, Start, Goal }

public class Tile : MonoBehaviour
{
    [SerializeField] private TileType tileType = TileType.Straight;
    [SerializeField] private TileKind tileKind = TileKind.Pipe;

    private int rotationState;
    private bool canRotate = true;
    private Vector2Int endpointDirection;
    private Camera mainCamera;

    public event Action OnTileRotated;

    private void Awake() { mainCamera = Camera.main; }

    private void Update()
    {
        if (!canRotate || tileKind != TileKind.Pipe) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) TryRotate(Mouse.current.position.ReadValue());
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) TryRotate(Touchscreen.current.primaryTouch.position.ReadValue());
    }

    private void TryRotate(Vector2 screenPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(new Vector2(worldPosition.x, worldPosition.y));

        if (hit != null && hit.transform == transform) RotateTile();
    }

    private void RotateTile() { AdvanceRotationState(); OnTileRotated?.Invoke(); }

    public void RotateForShuffle() { if (tileKind == TileKind.Pipe) AdvanceRotationState(); }

    public void DebugRotate() { if (canRotate && tileKind == TileKind.Pipe) RotateTile(); }

    private void AdvanceRotationState()
    {
        rotationState = (rotationState + 1) % 4;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationState * 90f);
    }

    public void ResetTile()
    {
        rotationState = 0;
        transform.rotation = tileKind == TileKind.Pipe ? Quaternion.identity : Quaternion.Euler(0f, 0f, GetEndpointRotation(endpointDirection));
        canRotate = tileKind == TileKind.Pipe;
    }

    public void SetInitialState(TileType type, int initialRotationState)
    {
        tileKind = TileKind.Pipe;
        tileType = type;
        endpointDirection = Vector2Int.zero;
        rotationState = Mathf.Clamp(initialRotationState, 0, 3);
        transform.rotation = Quaternion.Euler(0f, 0f, rotationState * 90f);
        canRotate = true;
    }

    public void SetEndpoint(TileKind kind, Vector2Int direction)
    {
        if (kind != TileKind.Start && kind != TileKind.Goal) return;
        tileKind = kind;
        endpointDirection = NormalizeDirection(direction);
        rotationState = 0;
        transform.rotation = Quaternion.Euler(0f, 0f, GetEndpointRotation(endpointDirection));
        canRotate = false;
    }

    private static Vector2Int NormalizeDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return Vector2Int.up;
        if (direction == Vector2Int.down) return Vector2Int.down;
        if (direction == Vector2Int.left) return Vector2Int.left;
        if (direction == Vector2Int.right) return Vector2Int.right;
        return Vector2Int.zero;
    }

    private static float GetEndpointRotation(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return 0f;
        if (direction == Vector2Int.down) return 180f;
        if (direction == Vector2Int.right) return -90f;
        if (direction == Vector2Int.left) return 90f;
        return 0f;
    }

    public void LockTile() { canRotate = false; }

    public void UnlockTile() { if (tileKind == TileKind.Pipe) canRotate = true; }

    public bool HasConnection(Vector2Int direction)
    {
        if (tileKind == TileKind.Start || tileKind == TileKind.Goal) return direction == endpointDirection;
        return HasConnectionStatic(tileType, rotationState, direction);
    }

    public static bool HasConnectionStatic(TileType type, int rotationState, Vector2Int direction)
    {
        rotationState = ((rotationState % 4) + 4) % 4;
        return type switch
        {
            TileType.Straight => HasConnectionStraightStatic(rotationState, direction),
            TileType.Corner => HasConnectionCornerStatic(rotationState, direction),
            _ => false
        };
    }

    private static bool HasConnectionStraightStatic(int rotationState, Vector2Int direction)
    {
        return rotationState % 2 == 0 ? direction == Vector2Int.up || direction == Vector2Int.down : direction == Vector2Int.left || direction == Vector2Int.right;
    }

    private static bool HasConnectionCornerStatic(int rotationState, Vector2Int direction)
    {
        return rotationState switch
        {
            0 => direction == Vector2Int.up || direction == Vector2Int.right,
            1 => direction == Vector2Int.up || direction == Vector2Int.left,
            2 => direction == Vector2Int.down || direction == Vector2Int.left,
            3 => direction == Vector2Int.down || direction == Vector2Int.right,
            _ => false
        };
    }

    public int RotationState => rotationState;
    public TileType TileType => tileType;
    public TileKind TileKind => tileKind;
    public Vector2Int EndpointDirection => endpointDirection;
    public bool CanRotate => canRotate;
}