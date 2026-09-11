using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    private bool levelCompleted;

    private void OnEnable()
    {
        if (levelManager == null) return;

        levelManager.OnLevelChanged += HandleLevelChanged;
        levelManager.OnMovesChanged += HandleMovesChanged;
        levelManager.OnLevelCompleted += HandleLevelCompleted;
        levelManager.OnLevelFailed += HandleLevelFailed;
    }

    private void Start()
    {
        if (levelManager == null)
        {
            Debug.LogError("GameUI: LevelManager is not assigned!", gameObject);
            return;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (levelManager == null) return;

        levelManager.OnLevelChanged -= HandleLevelChanged;
        levelManager.OnMovesChanged -= HandleMovesChanged;
        levelManager.OnLevelCompleted -= HandleLevelCompleted;
        levelManager.OnLevelFailed -= HandleLevelFailed;
    }

    private void HandleLevelChanged(int level)
    {
        levelCompleted = false;
        RefreshUI();
    }

    private void HandleMovesChanged(int moves, int moveLimit)
    {
        UpdateMovesText(moves, moveLimit);
    }

    private void HandleLevelCompleted()
    {
        levelCompleted = true;
        ShowStatus("LEVEL COMPLETE!");
        UpdateActionButton();
    }

    private void HandleLevelFailed()
    {
        levelCompleted = false;
        ShowStatus("LEVEL FAILED!");
        UpdateActionButton();
    }

    private void RefreshUI()
    {
        levelText.text = $"LEVEL {levelManager.CurrentLevel}";
        UpdateMovesText(levelManager.CurrentMoves, levelManager.MoveLimit);
        HideStatus();
        UpdateActionButton();
    }

    private void UpdateMovesText(int moves, int moveLimit)
    {
        movesText.text = moveLimit > 0 ? $"MOVES: {moves} / {moveLimit}" : $"MOVES: {moves}";
    }

    private void UpdateActionButton()
    {
        if (actionButton == null) return;

        bool showNext = levelCompleted && !levelManager.IsLastLevel;
        actionButton.gameObject.SetActive(!levelManager.IsLastLevel || !levelCompleted);

        if (actionButtonText != null)
            actionButtonText.text = showNext ? "NEXT" : "RETRY";
    }

    private void ShowStatus(string message)
    {
        if (statusText == null) return;

        statusText.text = message;
        statusText.gameObject.SetActive(true);
    }

    private void HideStatus()
    {
        if (statusText == null) return;

        statusText.text = string.Empty;
        statusText.gameObject.SetActive(false);
    }

    public void OnActionButtonPressed()
    {
        if (levelManager == null) return;

        if (levelCompleted && !levelManager.IsLastLevel)
            levelManager.NextLevel();
        else
            levelManager.RetryLevel();
    }
}