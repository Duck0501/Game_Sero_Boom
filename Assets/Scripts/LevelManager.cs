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
        if (index < 0 || index >= levelPrefabs.Length) return;

        StartCoroutine(TransitionLoadLevel(index));
    }

    private IEnumerator PlayTransitionPanels()
    {
        float moveTime = 0.8f;
        float waitInMiddle = 1f;

        panelLeft.gameObject.SetActive(true);
        panelRight.gameObject.SetActive(true);

        Vector2 leftStartPos = panelLeft.anchoredPosition;
        Vector2 rightStartPos = panelRight.anchoredPosition;

        Sequence sequenceIn = DOTween.Sequence();
        sequenceIn.Join(panelLeft.DOAnchorPos(Vector2.zero, moveTime).SetEase(Ease.InOutSine));
        sequenceIn.Join(panelRight.DOAnchorPos(Vector2.zero, moveTime).SetEase(Ease.InOutSine));
        yield return sequenceIn.WaitForCompletion();

        yield return new WaitForSeconds(waitInMiddle);

        Sequence sequenceOut = DOTween.Sequence();
        sequenceOut.Join(panelLeft.DOAnchorPos(leftStartPos, moveTime).SetEase(Ease.InOutSine));
        sequenceOut.Join(panelRight.DOAnchorPos(rightStartPos, moveTime).SetEase(Ease.InOutSine));
        yield return sequenceOut.WaitForCompletion();

        panelLeft.gameObject.SetActive(false);
        panelRight.gameObject.SetActive(false);
    }

    private IEnumerator HideTransitionPanels()
    {
        float moveTime = 0.8f;

        float leftWidth = panelLeft.rect.width;
        float rightWidth = panelRight.rect.width;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(panelLeft.DOAnchorPos(new Vector2(-leftWidth, 0), moveTime).SetEase(Ease.InOutSine));
        sequence.Join(panelRight.DOAnchorPos(new Vector2(rightWidth, 0), moveTime).SetEase(Ease.InOutSine));

        yield return sequence.WaitForCompletion();

        panelLeft.gameObject.SetActive(false);
        panelRight.gameObject.SetActive(false);
    }

    private IEnumerator LoadLevelInternal(int index)
    {
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
            snakeParent = GameObject.Find("SnakeParent") ?? new GameObject("SnakeParent");
        }

        snakeController.transform.position = Vector3.zero;

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(HideTransitionPanels());
    }

    private IEnumerator TransitionLoadLevel(int nextIndex)
    {
        if (currentLevel != null)
        {
            Destroy(currentLevel);
            currentLevel = null;
        }

        yield return StartCoroutine(PlayTransitionPanels());

        yield return StartCoroutine(LoadLevelInternal(nextIndex));
    }
}