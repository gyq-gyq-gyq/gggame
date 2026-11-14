using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Title screen, pause menu, audio settings, and a lightweight local continue save.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class ForestAdventureMenu : MonoBehaviour
{
    private const string SaveFlag = "ForestAdventure.Save.Exists";
    private const string SaveScene = "ForestAdventure.Save.Scene";
    private const string SaveX = "ForestAdventure.Save.X";
    private const string SaveY = "ForestAdventure.Save.Y";
    private const string SaveZ = "ForestAdventure.Save.Z";
    private const string VolumeKey = "ForestAdventure.Audio.Volume";
    private const string MusicKey = "ForestAdventure.Audio.Music";

    private static ForestAdventureMenu instance;
    private Texture2D titleScreen;
    private Font uiFont;
    private GUIStyle hitStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelTitleStyle;
    private GUIStyle panelBodyStyle;
    private GUIStyle smallStyle;
    private bool menuOpen = true;
    private bool pauseOpen;
    private bool settingsOpen;
    private bool hasStarted;
    private bool musicEnabled = true;
    private bool loadForNewGame;
    private bool loadForContinue;
    private float masterVolume = 1f;
    private string toastMessage;
    private float toastUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject root = new GameObject("Forest Adventure UI");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<ForestAdventureMenu>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        titleScreen = Resources.Load<Texture2D>("ForestTitleScreen");
        masterVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        musicEnabled = PlayerPrefs.GetInt(MusicKey, 1) != 0;
        CreateStyles();
        EnsureMusicSource();
        ApplyAudioSettings();
        menuOpen = true;
        pauseOpen = false;
        settingsOpen = false;
        Time.timeScale = 0f;
        Cursor.visible = true;
        SetPlayerControllerEnabled(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (settingsOpen)
        {
            settingsOpen = false;
            PlayerPrefs.Save();
            return;
        }

        if (!hasStarted) return;
        if (pauseOpen) ResumeGame();
        else if (!menuOpen) PauseGame();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveProgress();
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
        Time.timeScale = 1f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureMusicSource();
        ApplyAudioSettings();

        if (loadForNewGame || loadForContinue)
        {
            StartCoroutine(CompleteSceneLoad(loadForContinue));
        }
        else if (!hasStarted)
        {
            Time.timeScale = 0f;
            SetPlayerControllerEnabled(false);
        }
    }

    private IEnumerator CompleteSceneLoad(bool restoreSave)
    {
        yield return null;
        if (restoreSave) RestoreProgress();
        loadForContinue = false;
        loadForNewGame = false;
        menuOpen = false;
        pauseOpen = false;
        settingsOpen = false;
        hasStarted = true;
        SetPlayerControllerEnabled(true);
        Time.timeScale = 1f;
        Cursor.visible = false;
    }

    private void CreateStyles()
    {
        try
        {
            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 26);
        }
        catch
        {
            uiFont = null;
        }

        hitStyle = new GUIStyle();
        hitStyle.normal.background = null;
        hitStyle.hover.background = null;
        hitStyle.active.background = null;
        hitStyle.focused.background = null;
        hitStyle.border = new RectOffset(0, 0, 0, 0);
        hitStyle.padding = new RectOffset(0, 0, 0, 0);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            fontSize = 23,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(12, 12, 5, 5)
        };
        buttonStyle.normal.background = Texture2D.whiteTexture;
        buttonStyle.hover.background = Texture2D.whiteTexture;
        buttonStyle.active.background = Texture2D.whiteTexture;
        buttonStyle.normal.textColor = new Color(1f, 0.96f, 0.79f);
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;

        panelTitleStyle = MakeLabelStyle(31, new Color(1f, 0.9f, 0.58f), FontStyle.Bold);
        panelBodyStyle = MakeLabelStyle(21, new Color(1f, 0.97f, 0.85f), FontStyle.Normal);
        smallStyle = MakeLabelStyle(17, new Color(1f, 0.97f, 0.85f), FontStyle.Normal);
    }

    private GUIStyle MakeLabelStyle(int size, Color color, FontStyle fontStyle)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = size,
            fontStyle = fontStyle,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        style.normal.textColor = color;
        return style;
    }

    private void OnGUI()
    {
        if (!menuOpen && !pauseOpen) return;

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;
        GUI.depth = -1000;
        float scaleX = Screen.width / 1024f;
        float scaleY = Screen.height / 576f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scaleX, scaleY, 1f));
        GUI.color = Color.white;

        if (menuOpen)
        {
            DrawMainMenu();
            if (settingsOpen) DrawSettingsPanel();
        }
        else if (pauseOpen)
        {
            DrawPausePanel();
            if (settingsOpen) DrawSettingsPanel();
        }

        if (!settingsOpen && Time.unscaledTime < toastUntil) DrawToast();
        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    private void DrawMainMenu()
    {
        if (titleScreen != null)
        {
            GUI.DrawTexture(new Rect(0, 0, 1024, 576), titleScreen, ScaleMode.StretchToFill);
        }
        else
        {
            GUI.color = new Color(0.42f, 0.77f, 0.88f);
            GUI.DrawTexture(new Rect(0, 0, 1024, 576), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(180, 55, 664, 90), "郭盖大冒险", panelTitleStyle);
            GUI.Label(new Rect(260, 142, 504, 42), "森林跑酷", panelBodyStyle);
        }

        if (settingsOpen) return;

        // These transparent hit areas sit directly over the buttons in the supplied title art.
        if (GUI.Button(new Rect(362, 194, 300, 88), GUIContent.none, hitStyle)) StartNewGame();
        if (GUI.Button(new Rect(362, 280, 300, 83), GUIContent.none, hitStyle)) ContinueGame();
        if (GUI.Button(new Rect(361, 364, 302, 82), GUIContent.none, hitStyle)) ExitGame();
        if (GUI.Button(new Rect(400, 456, 108, 56), GUIContent.none, hitStyle)) settingsOpen = true;
        if (GUI.Button(new Rect(516, 456, 108, 56), GUIContent.none, hitStyle)) ToggleMusic();
    }

    private void DrawSettingsPanel()
    {
        GUI.color = new Color(0.02f, 0.08f, 0.06f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, 1024, 576), Texture2D.whiteTexture);
        GUI.color = new Color(0.28f, 0.15f, 0.07f, 0.98f);
        GUI.DrawTexture(new Rect(300, 128, 424, 320), Texture2D.whiteTexture);
        GUI.color = new Color(0.62f, 0.37f, 0.16f, 1f);
        GUI.DrawTexture(new Rect(308, 136, 408, 304), Texture2D.whiteTexture);
        GUI.color = new Color(0.34f, 0.52f, 0.20f, 1f);
        GUI.DrawTexture(new Rect(316, 143, 392, 7), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(316, 426, 392, 7), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(332, 158, 360, 46), "声音设置", panelTitleStyle);
        GUI.Label(new Rect(352, 220, 160, 34), "游戏音量", panelBodyStyle);
        float adjustedVolume = GUI.HorizontalSlider(new Rect(500, 231, 145, 20), masterVolume, 0f, 1f);
        if (Mathf.Abs(adjustedVolume - masterVolume) > 0.001f)
        {
            masterVolume = adjustedVolume;
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            AudioListener.volume = masterVolume;
        }
        GUI.Label(new Rect(645, 218, 55, 34), Mathf.RoundToInt(masterVolume * 100f) + "%", smallStyle);

        GUI.Label(new Rect(352, 270, 160, 34), "背景音乐", panelBodyStyle);
        GUI.Label(new Rect(500, 270, 130, 34), musicEnabled ? "已开启" : "已关闭", smallStyle);
        if (DrawStyledButton(new Rect(615, 270, 72, 36), musicEnabled ? "关闭" : "开启")) ToggleMusic();

        if (DrawStyledButton(new Rect(355, 355, 140, 48), "恢复默认"))
        {
            masterVolume = 1f;
            musicEnabled = true;
            PlayerPrefs.SetFloat(VolumeKey, masterVolume);
            PlayerPrefs.SetInt(MusicKey, 1);
            ApplyAudioSettings();
        }
        if (DrawStyledButton(new Rect(529, 355, 140, 48), "完成"))
        {
            settingsOpen = false;
            PlayerPrefs.Save();
        }
        GUI.Label(new Rect(332, 407, 360, 26), "按 Esc 或点击“完成”返回", smallStyle);
    }

    private void DrawPausePanel()
    {
        GUI.color = new Color(0.02f, 0.08f, 0.06f, 0.68f);
        GUI.DrawTexture(new Rect(0, 0, 1024, 576), Texture2D.whiteTexture);
        GUI.color = new Color(0.32f, 0.17f, 0.08f, 0.98f);
        GUI.DrawTexture(new Rect(326, 118, 372, 340), Texture2D.whiteTexture);
        GUI.color = new Color(0.66f, 0.41f, 0.18f, 1f);
        GUI.DrawTexture(new Rect(334, 126, 356, 324), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(350, 145, 324, 54), "冒险暂停", panelTitleStyle);

        if (DrawStyledButton(new Rect(392, 216, 240, 54), "继续冒险")) ResumeGame();
        if (DrawStyledButton(new Rect(392, 281, 240, 54), "声音设置")) settingsOpen = true;
        if (DrawStyledButton(new Rect(392, 346, 240, 54), "返回标题")) ReturnToTitle();
        GUI.Label(new Rect(350, 405, 324, 34), "按 Esc 可继续冒险", smallStyle);
    }

    private void DrawToast()
    {
        GUI.color = new Color(0.17f, 0.10f, 0.05f, 0.90f);
        GUI.DrawTexture(new Rect(350, 516, 324, 42), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(362, 520, 300, 34), toastMessage, smallStyle);
    }

    private bool DrawStyledButton(Rect rect, string label)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0.42f, 0.22f, 0.09f, 1f);
        bool clicked = GUI.Button(rect, label, buttonStyle);
        GUI.color = oldColor;
        return clicked;
    }

    private void StartNewGame()
    {
        ClearSave();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            loadForNewGame = true;
            loadForContinue = false;
            Time.timeScale = 0f;
            SetPlayerControllerEnabled(false);
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        hasStarted = true;
        menuOpen = false;
        Time.timeScale = 1f;
        SetPlayerControllerEnabled(true);
        Cursor.visible = false;
    }

    private void ContinueGame()
    {
        if (PlayerPrefs.GetInt(SaveFlag, 0) == 0)
        {
            ShowToast("还没有冒险记录，请先开始游戏");
            return;
        }

        string savedScene = PlayerPrefs.GetString(SaveScene, SceneManager.GetActiveScene().name);
        Scene activeScene = SceneManager.GetActiveScene();
        if (savedScene != activeScene.name && Application.CanStreamedLevelBeLoaded(savedScene))
        {
            loadForContinue = true;
            loadForNewGame = false;
            Time.timeScale = 0f;
            SceneManager.LoadScene(savedScene);
            return;
        }

        RestoreProgress();
        hasStarted = true;
        menuOpen = false;
        pauseOpen = false;
        settingsOpen = false;
        Time.timeScale = 1f;
        SetPlayerControllerEnabled(true);
        Cursor.visible = false;
    }

    private void PauseGame()
    {
        SaveProgress();
        pauseOpen = true;
        settingsOpen = false;
        Time.timeScale = 0f;
        Cursor.visible = true;
        SetPlayerControllerEnabled(false);
    }

    private void ResumeGame()
    {
        settingsOpen = false;
        menuOpen = false;
        pauseOpen = false;
        hasStarted = true;
        SetPlayerControllerEnabled(true);
        Time.timeScale = 1f;
        Cursor.visible = false;
    }

    private void ReturnToTitle()
    {
        SaveProgress();
        pauseOpen = false;
        settingsOpen = false;
        menuOpen = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        SetPlayerControllerEnabled(false);
    }

    private void ToggleMusic()
    {
        musicEnabled = !musicEnabled;
        PlayerPrefs.SetInt(MusicKey, musicEnabled ? 1 : 0);
        ApplyMusicSetting();
        ShowToast(musicEnabled ? "背景音乐：已开启" : "背景音乐：已关闭");
    }

    private void ExitGame()
    {
        SaveProgress();
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureMusicSource()
    {
        BackgroundMusic[] sources = FindObjectsOfType<BackgroundMusic>(true);
        if (sources.Length == 0) gameObject.AddComponent<BackgroundMusic>();
    }

    private void ApplyAudioSettings()
    {
        AudioListener.volume = masterVolume;
        ApplyMusicSetting();
    }

    private void ApplyMusicSetting()
    {
        EnsureMusicSource();
        BackgroundMusic[] sources = FindObjectsOfType<BackgroundMusic>(true);
        foreach (BackgroundMusic source in sources)
        {
            AudioSource audioSource = source.GetComponent<AudioSource>();
            if (audioSource != null) audioSource.mute = !musicEnabled;
        }
    }

    private static PlayerController FindPlayerController()
    {
        return FindObjectOfType<PlayerController>(true);
    }

    private static void SetPlayerControllerEnabled(bool enabled)
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>(true);
        foreach (PlayerController player in players) player.enabled = enabled;
    }

    private void SaveProgress()
    {
        if (!hasStarted || menuOpen) return;
        PlayerController player = FindPlayerController();
        if (player == null) return;

        Vector3 position = player.transform.position;
        PlayerPrefs.SetInt(SaveFlag, 1);
        PlayerPrefs.SetString(SaveScene, SceneManager.GetActiveScene().name);
        PlayerPrefs.SetFloat(SaveX, position.x);
        PlayerPrefs.SetFloat(SaveY, position.y);
        PlayerPrefs.SetFloat(SaveZ, position.z);
        PlayerPrefs.Save();
    }

    private void RestoreProgress()
    {
        if (PlayerPrefs.GetInt(SaveFlag, 0) == 0) return;
        PlayerController player = FindPlayerController();
        if (player == null) return;

        Vector3 savedPosition = new Vector3(
            PlayerPrefs.GetFloat(SaveX, player.transform.position.x),
            PlayerPrefs.GetFloat(SaveY, player.transform.position.y),
            PlayerPrefs.GetFloat(SaveZ, player.transform.position.z));
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.position = new Vector2(savedPosition.x, savedPosition.y);
        player.transform.position = savedPosition;
        Physics2D.SyncTransforms();
    }

    private void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveFlag);
        PlayerPrefs.DeleteKey(SaveScene);
        PlayerPrefs.DeleteKey(SaveX);
        PlayerPrefs.DeleteKey(SaveY);
        PlayerPrefs.DeleteKey(SaveZ);
        PlayerPrefs.Save();
    }

    private void ShowToast(string message)
    {
        toastMessage = message;
        toastUntil = Time.unscaledTime + 2.2f;
    }
}


