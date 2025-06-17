using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Button buttonPlay, buttonHelp, buttonToHome, 
        buttonReset, buttonRestartFromLevel1, buttonNextLevel;
    private LevelManager levelManager;
    public GameObject canvasWin;
    public GameObject canvasLose;

    void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();

        if (buttonPlay != null)
        {
            buttonPlay.onClick.AddListener(() => SceneManager.LoadScene("Play"));
        }

        if (buttonHelp != null)
        {
            buttonHelp.onClick.AddListener(() => SceneManager.LoadScene("Help"));
        }

        if (buttonToHome != null)
        {
            buttonToHome.onClick.AddListener(() => SceneManager.LoadScene("Home"));
        }

        if (buttonReset != null)
        {
            buttonReset.onClick.AddListener(ResetCurrentLevel);
        }

        if (buttonRestartFromLevel1 != null)
        {
            buttonRestartFromLevel1.onClick.AddListener(RestartFromLevel1);
        }

        if (buttonNextLevel != null)
        {
            buttonNextLevel.onClick.AddListener(GoToNextLevel);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCurrentLevel();
        }
        if (buttonNextLevel != null)
        {
            bool isWinShown = canvasWin != null && canvasWin.activeSelf;
            bool isLoseShown = canvasLose != null && canvasLose.activeSelf;
            buttonNextLevel.interactable = !(isWinShown || isLoseShown);
        }
    }

    void ResetCurrentLevel()
    {
        if (levelManager != null)
        {
            levelManager.LoadLevel(levelManager.currentLevelIndex); 
        }
    }

    void RestartFromLevel1()
    {
        if (levelManager != null)
        {
            levelManager.LoadLevel(0); 
        }
    }

    void GoToNextLevel()
    {
        if (levelManager != null)
        {
            int nextIndex = levelManager.currentLevelIndex + 1;
            if (nextIndex < levelManager.levelPrefabs.Length)
            {
                levelManager.LoadLevel(nextIndex);
            }
        }
    }
}