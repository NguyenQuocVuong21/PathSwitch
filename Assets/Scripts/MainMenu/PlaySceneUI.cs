using UnityEngine;
using UnityEngine.SceneManagement;

public class PlaySceneUI : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MenuScene";

    public void OnPlayAndBackButtonPressed()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public void OnAudioButtonPressed()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.ToggleMusic();
    }
}