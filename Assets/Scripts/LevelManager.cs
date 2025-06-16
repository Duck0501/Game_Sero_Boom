using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Linq;
using UnityEngine.Tilemaps;

public class LevelManager : MonoBehaviour
{
    public GameObject[] levelPrefabs;
    public int currentLevelIndex = 0;
    private GameObject currentLevel;
    private SnakeController snakeController;
    public GameObject snakeParent;

    public RectTransform panelLeft;
    public RectTransform panelRight;

    public int[] bananaCountsPerLevel;
    public int[] medicineCountsPerLevel;

    void Start()
    {
        if (levelPrefabs.Length == 0)
        {
            return;
        }
        if (bananaCountsPerLevel == null || medicineCountsPerLevel == null ||
            bananaCountsPerLevel.Length != levelPrefabs.Length || medicineCountsPerLevel.Length != levelPrefabs.Length)
        {
            return;
        }
        snakeController = FindObjectOfType<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found!");
            return;
        }
        LoadLevel(currentLevelIndex);
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levelPrefabs.Length)
        {
            return;
        }

        if (currentLevel != null)
        {
            StartCoroutine(HideLevelThenLoadNext(currentLevel, index));
            return;
        }

        currentLevel = Instantiate(levelPrefabs[index], Vector3.zero, Quaternion.identity);
        currentLevelIndex = index;

        Tilemap[] groundTilemaps = currentLevel.GetComponentsInChildren<Tilemap>().Where(t => t.gameObject.tag == "Ground").ToArray();
        Tilemap[] wallTilemaps = currentLevel.GetComponentsInChildren<Tilemap>().Where(t => t.gameObject.tag == "Wall").ToArray();
        if (snakeController != null)
        {
            snakeController.groundTilemaps = groundTilemaps;
            snakeController.wallTilemaps = wallTilemaps;
            snakeController.ResetSnake(); 
        }

        if (snakeParent == null)
        {
            snakeParent = GameObject.Find("SnakeParent");
            if (snakeParent == null)
            {
                snakeParent = new GameObject("SnakeParent");
            }
        }

        Vector3 startPosition = new Vector3(0, 0, 0);
        snakeController.transform.position = startPosition;
    }

    private IEnumerator HideLevelThenLoadNext(GameObject levelToHide, int nextIndex)
    {
        float duration = 0.1f;
        float delayStep = 0.1f;

        SpriteRenderer[] renderers = levelToHide.GetComponentsInChildren<SpriteRenderer>();
        var sorted = renderers.OrderBy(r => r.transform.position.x).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var sr = sorted[i];
            sr.transform.DOScaleX(0f, duration).SetEase(Ease.InOutSine).SetDelay(i * delayStep);
        }

        float totalDelay = sorted.Count * delayStep + duration;
        yield return new WaitForSeconds(totalDelay);

        yield return StartCoroutine(PlayTransitionPanels());

        Destroy(levelToHide);
        LoadLevel(nextIndex);

        yield return StartCoroutine(HideTransitionPanels());
    }

    private IEnumerator PlayTransitionPanels()
    {
        float moveTime = 0.8f;
        panelLeft.gameObject.SetActive(true);
        panelRight.gameObject.SetActive(true);

        panelLeft.anchoredPosition = new Vector2(-Screen.width, 0);
        panelRight.anchoredPosition = new Vector2(Screen.width, 0);

        panelLeft.DOAnchorPos(Vector2.zero, moveTime).SetEase(Ease.InOutSine);
        panelRight.DOAnchorPos(Vector2.zero, moveTime).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(moveTime);
    }

    private IEnumerator HideTransitionPanels()
    {
        float moveTime = 1f;

        panelLeft.DOAnchorPos(new Vector2(-Screen.width, 0), moveTime).SetEase(Ease.InOutSine);
        panelRight.DOAnchorPos(new Vector2(Screen.width, 0), moveTime).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(moveTime);

        panelLeft.gameObject.SetActive(false);
        panelRight.gameObject.SetActive(false);
    }
}