using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject audioPanel;

    [Header("Level Buttons")]
    [SerializeField] private Button[] levelButtons;

    [Header("Game")]
    [SerializeField] private string gameSceneName = "LevelScene";

    private const int FirstLevel = 1;
    private const int LastLevel = 5;
    private const string HighestUnlockedLevelKey = "PathSwitch_HighestUnlockedLevel";
    private const string SelectedLevelKey = "PathSwitch_SelectedLevel";

    private void Start()
    {
        ShowLevelPanel();
    }

    public void ShowMainPanel()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (levelPanel != null)
            levelPanel.SetActive(false);

        if (audioPanel != null)
            audioPanel.SetActive(false);
    }

    public void ShowLevelPanel()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);

        if (levelPanel != null)
            levelPanel.SetActive(true);

        if (audioPanel != null)
            audioPanel.SetActive(false);

        UpdateLevelButtons();
    }

    public void ShowAudioPanel()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);

        if (levelPanel != null)
            levelPanel.SetActive(false);

        if (audioPanel != null)
            audioPanel.SetActive(true);
    }

    public void SelectLevel(int level)
    {
        int highestUnlockedLevel = GetHighestUnlockedLevel();

        if (level < FirstLevel || level > LastLevel)
            return;

        if (level > highestUnlockedLevel)
        {
            Debug.LogWarning($"Level {level} chưa được mở khóa.");
            return;
        }

        PlayerPrefs.SetInt(SelectedLevelKey, level);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }

    public void ToggleAudio()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.ToggleMusic();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    private void UpdateLevelButtons()
    {
        if (levelButtons == null)
            return;

        int highestUnlockedLevel = GetHighestUnlockedLevel();

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i + FirstLevel;

            if (level > LastLevel)
            {
                levelButtons[i].interactable = false;
                continue;
            }

            levelButtons[i].interactable = level <= highestUnlockedLevel;
        }
    }

    private int GetHighestUnlockedLevel()
    {
        return Mathf.Clamp(
            PlayerPrefs.GetInt(HighestUnlockedLevelKey, FirstLevel),
            FirstLevel,
            LastLevel
        );
    }

    public void ResetProgressForTesting()
    {
        PlayerPrefs.DeleteKey(HighestUnlockedLevelKey);
        PlayerPrefs.DeleteKey(SelectedLevelKey);
        PlayerPrefs.Save();

        UpdateLevelButtons();

        Debug.Log("PathSwitch progress đã được reset về Level 1.");
    }
}