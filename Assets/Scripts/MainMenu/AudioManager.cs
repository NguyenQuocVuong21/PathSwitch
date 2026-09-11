using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    private const string MusicKey = "MusicEnabled";

    private bool musicEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;

        ApplyMusicState();
    }

    private void Start()
    {
        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    public void ToggleMusic()
    {
        musicEnabled = !musicEnabled;

        PlayerPrefs.SetInt(MusicKey, musicEnabled ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMusicState();
    }

    private void ApplyMusicState()
    {
        if (musicSource == null)
            return;

        musicSource.mute = !musicEnabled;

        if (musicEnabled && !musicSource.isPlaying)
            musicSource.Play();

        if (!musicEnabled && musicSource.isPlaying)
            musicSource.Pause();
    }

    public bool IsMusicEnabled => musicEnabled;
}