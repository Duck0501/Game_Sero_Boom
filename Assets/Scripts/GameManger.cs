using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Button buttonPlay, buttonHelp, buttonToHome, buttonReset;
    private LevelManager levelManager; 

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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCurrentLevel();
        }
    }

    void ResetCurrentLevel()
    {
        if (levelManager != null)
        {
            levelManager.LoadLevel(levelManager.currentLevelIndex); 
        }
    }
}