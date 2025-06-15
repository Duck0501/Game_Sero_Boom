using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public GameObject[] levelPrefabs; // Mảng các prefab level
    private int currentLevelIndex = 0;
    private GameObject currentLevel;
    private int bananaCount; // Số lượng chuối trong level hiện tại
    private int medicineCount; // Số lượng medicine trong level hiện tại

    void Start()
    {
        if (levelPrefabs.Length == 0)
        {
            Debug.LogError("No level prefabs assigned!");
            return;
        }
        LoadLevel(0); // Tải level đầu tiên
    }

    void LoadLevel(int index)
    {
        if (index < 0 || index >= levelPrefabs.Length) return;

        // Destroy level cũ nếu có
        if (currentLevel != null)
        {
            Destroy(currentLevel);
        }

        // Tải level mới
        currentLevel = Instantiate(levelPrefabs[index], Vector3.zero, Quaternion.identity);
        currentLevelIndex = index;

        // Đếm số lượng chuối và medicine trong level
        bananaCount = CountItems("Banana");
        medicineCount = CountItems("Medicine");
        Debug.Log($"Level {currentLevelIndex + 1} loaded with {bananaCount} bananas and {medicineCount} medicines");
    }

    int CountItems(string tag)
    {
        if (currentLevel == null) return 0;
        return currentLevel.GetComponentsInChildren<Transform>().Count(t => t.CompareTag(tag));
    }

    public void OnItemEaten(string itemTag)
    {
        if (itemTag == "Banana")
        {
            bananaCount--;
            Debug.Log($"Bananas remaining: {bananaCount}");
        }
        else if (itemTag == "Medicine")
        {
            medicineCount--;
            Debug.Log($"Medicines remaining: {medicineCount}");
        }

        if (bananaCount <= 0 && medicineCount <= 0)
        {
            Debug.Log($"All items eaten in Level {currentLevelIndex + 1}!");
            int nextLevelIndex = currentLevelIndex + 1;
            if (nextLevelIndex < levelPrefabs.Length)
            {
                LoadLevel(nextLevelIndex);
            }
            else
            {
                Debug.Log("All levels completed!");
                // Thêm logic khi hoàn thành tất cả level (nếu cần)
            }
        }
    }
}