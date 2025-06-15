using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManger : MonoBehaviour
{
    public Button buttonPlay, buttonHelp, buttonToHome, buttonReset;
    public Button buttonLeft, buttonRight, buttonUp, buttonDown, buttonUndo;

    void Start()
    {
        if (buttonPlay != null)
            buttonPlay.onClick.AddListener(() => SceneManager.LoadScene("Play"));

        if (buttonHelp != null)
            buttonHelp.onClick.AddListener(() => SceneManager.LoadScene("Help"));

        if (buttonToHome != null)
            buttonToHome.onClick.AddListener(() => SceneManager.LoadScene("Home"));

        if (buttonReset != null)
            buttonReset.onClick.AddListener(ResetScene);
    }

    void Update()
    {
        // Khi nhấn phím R, cũng reset scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetScene();
        }
    }

    void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
