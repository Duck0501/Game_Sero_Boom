using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

public class LevelManager : MonoBehaviour
{
    public GameObject[] levelPrefabs;
    public int currentLevelIndex = 0;
    private GameObject currentLevel;
    private SnakeController snakeController;
    public GameObject snakeParent;

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
            Destroy(currentLevel);
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
}