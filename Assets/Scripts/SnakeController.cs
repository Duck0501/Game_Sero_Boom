using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;
using System.Collections;

public class SnakeController : MonoBehaviour
{
    private enum Direction { Up, Down, Left, Right }

    public GameObject snakeHead;
    public GameObject snakeBody;
    public GameObject snakeTail;
    public GameObject faceMouthObject;
    public GameObject pushEffect1;
    public GameObject pushEffect2;
    public Tilemap[] groundTilemaps;
    public Tilemap[] wallTilemaps;
    public int bodyLength = 4;
    private float moveStepX = 0.7f;
    private float moveStepY = 0.56f;
    public float gridSize2X = 0.23f;
    public float gridSize2Y = 0.19f;

    private Direction currentDirection = Direction.Down;
    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Direction> directionHistory = new List<Direction>();
    private Dictionary<GameObject, Vector2> bananaPositions;
    private Dictionary<GameObject, Vector2> medicinePositions;
    private LevelManager levelManager;

    public Sprite head;
    public Sprite endBody;
    public Sprite bodyHorizontal;
    public Sprite bodyVertical;
    public Sprite cornerTopRight;
    public Sprite cornerTopLeft;
    public Sprite cornerBottomLeft;
    public Sprite cornerBottomRight;

    public SpriteRenderer faceRenderer;
    public Sprite faceNormal;
    public Sprite faceEatBanana;
    public Sprite faceFallFruit;
    public Sprite faceFallOffMap;
    public Sprite faceTakeMedicine;
    public Sprite faceTakeMedicinePush;

    private bool canMove = true;
    private bool isFalling = false;

    private int bananaCount;
    private int medicineCount;

    private HashSet<GameObject> processedItems = new HashSet<GameObject>();
    private Coroutine faceResetCoroutine;
    private bool isTransitioning = false;
    private bool suppressMouthTemporarily = false;
    private Coroutine pushEffectHideCoroutine;

    public void Start()
    {
        bananaPositions = new Dictionary<GameObject, Vector2>();
        medicinePositions = new Dictionary<GameObject, Vector2>();
        levelManager = FindObjectOfType<LevelManager>();

        Vector3 lastPos = transform.position;
        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);
        bodyParts.Add(snakeHead.transform);

        if (GameObject.Find("SnakeParent") == null)
        {
            levelManager.snakeParent = new GameObject("SnakeParent");
        }
        snakeHead.transform.SetParent(levelManager.snakeParent.transform);

        BoxCollider2D headCollider = snakeHead.AddComponent<BoxCollider2D>();
        headCollider.size = new Vector2(moveStepX, moveStepY);
        headCollider.isTrigger = true;
        SpriteRenderer headSr = snakeHead.GetComponent<SpriteRenderer>();
        if (headSr == null) headSr = snakeHead.AddComponent<SpriteRenderer>();
        headSr.sprite = head;

        faceRenderer = snakeHead.transform.Find("face")?.GetComponent<SpriteRenderer>();

        Vector3 spawnDir = Vector3.up;
        for (int i = 0; i < bodyLength; i++)
        {
            lastPos += spawnDir * moveStepY;
            GameObject bodyPart = Instantiate(snakeBody, lastPos, Quaternion.identity);
            bodyPart.transform.SetParent(levelManager.snakeParent.transform);
            BoxCollider2D bodyCollider = bodyPart.AddComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(moveStepX, moveStepY);
            SpriteRenderer sr = bodyPart.GetComponent<SpriteRenderer>();
            if (sr == null) sr = bodyPart.AddComponent<SpriteRenderer>();
            sr.sprite = bodyVertical;
            bodyParts.Add(bodyPart.transform);
            positionHistory.Add(lastPos);
            directionHistory.Add(currentDirection);
        }

        lastPos += spawnDir * moveStepY;
        GameObject tailPart = Instantiate(snakeTail, lastPos, Quaternion.identity);
        tailPart.transform.SetParent(levelManager.snakeParent.transform);
        BoxCollider2D tailCollider = tailPart.AddComponent<BoxCollider2D>();
        tailCollider.size = new Vector2(moveStepX, moveStepY);
        SpriteRenderer tailSr = tailPart.GetComponent<SpriteRenderer>();
        if (tailSr == null) tailSr = tailPart.AddComponent<SpriteRenderer>();
        tailSr.sprite = endBody;
        bodyParts.Add(tailPart.transform);
        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);

        UpdateTailRotation();

        if (levelManager != null && levelManager.currentLevelIndex >= 0 && levelManager.currentLevelIndex < levelManager.bananaCountsPerLevel.Length)
        {
            bananaCount = levelManager.bananaCountsPerLevel[levelManager.currentLevelIndex];
            medicineCount = levelManager.medicineCountsPerLevel[levelManager.currentLevelIndex];
        }
    }

    public void ResetSnake()
    {
        foreach (Transform child in levelManager.snakeParent.transform)
        {
            Destroy(child.gameObject);
        }
        bodyParts.Clear();
        positionHistory.Clear();
        directionHistory.Clear();
        processedItems.Clear();
        isTransitioning = false;
        Start();
        SetHeadFace(faceNormal);
    }

    void Update()
    {
        if (!isFalling && canMove)
        {
            Vector3 moveDir = Vector3.zero;
            float rotationZ = GetRotationZ(currentDirection);
            Direction newDirection = currentDirection;

            if (canMove && !isFalling && faceResetCoroutine == null) 
            {
                Vector3[] checkDirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
                bool foundNearbyFruit = false;

                foreach (var dir in checkDirs)
                {
                    Vector3 checkPos = snakeHead.transform.position + dir * 0.5f; 
                    Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, 0.1f);
                    foreach (var hit in hits)
                    {
                        if (hit.CompareTag("Banana") || hit.CompareTag("Medicine"))
                        {
                            foundNearbyFruit = true;
                            break;
                        }
                    }
                    if (foundNearbyFruit) break;
                }

                SetFaceMouthActive(foundNearbyFruit);
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (currentDirection != Direction.Down)
                {
                    moveDir = Vector3.up;
                    rotationZ = GetRotationZ(Direction.Up);
                    newDirection = Direction.Up;
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (currentDirection != Direction.Up)
                {
                    moveDir = Vector3.down;
                    rotationZ = GetRotationZ(Direction.Down);
                    newDirection = Direction.Down;
                }
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (currentDirection != Direction.Right)
                {
                    moveDir = Vector3.left;
                    rotationZ = GetRotationZ(Direction.Left);
                    newDirection = Direction.Left;
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (currentDirection != Direction.Left)
                {
                    moveDir = Vector3.right;
                    rotationZ = GetRotationZ(Direction.Right);
                    newDirection = Direction.Right;
                }
            }

            if (moveDir != Vector3.zero)
            {
                Vector3 newPosition = snakeHead.transform.position + GetMovementStep(newDirection);
                if (IsCollidingWithAnyWall(newPosition) || WillCollideWithBody(newPosition)) return;

                positionHistory.Insert(0, newPosition);
                if (positionHistory.Count > bodyParts.Count + 1)
                    positionHistory.RemoveAt(positionHistory.Count - 1);

                directionHistory.Insert(0, newDirection);
                if (directionHistory.Count > bodyParts.Count + 1)
                    directionHistory.RemoveAt(directionHistory.Count - 1);

                snakeHead.transform.position = newPosition;
                snakeHead.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
                currentDirection = newDirection;

                for (int i = 1; i < bodyParts.Count; i++)
                {
                    int historyIndex = i;
                    if (historyIndex < positionHistory.Count)
                    {
                        bodyParts[i].position = positionHistory[historyIndex];

                        if (i < bodyParts.Count - 1 && i < directionHistory.Count)
                        {
                            Direction from = directionHistory[i];
                            Direction to = directionHistory[i - 1];
                            SpriteRenderer sr = bodyParts[i].GetComponent<SpriteRenderer>();
                            if (sr) sr.sprite = GetBodySprite(from, to);
                        }
                    }
                }

                UpdateTailRotation();

                if (!IsOnAnyGround())
                {
                    StartFalling();
                }
            }
        }
    }

    public void MoveLeft()
    {
        if (canMove && currentDirection != Direction.Right && !isFalling)
        {
            currentDirection = Direction.Left;
            Vector3 newPosition = snakeHead.transform.position + GetMovementStep(Direction.Left);
            if (!IsCollidingWithAnyWall(newPosition) && !WillCollideWithBody(newPosition))
            {
                MoveSnake(newPosition, Direction.Left, GetRotationZ(Direction.Left));
            }
        }
    }

    public void MoveRight()
    {
        if (canMove && currentDirection != Direction.Left && !isFalling)
        {
            currentDirection = Direction.Right;
            Vector3 newPosition = snakeHead.transform.position + GetMovementStep(Direction.Right);
            if (!IsCollidingWithAnyWall(newPosition) && !WillCollideWithBody(newPosition))
            {
                MoveSnake(newPosition, Direction.Right, GetRotationZ(Direction.Right));
            }
        }
    }

    public void MoveDown()
    {
        if (canMove && currentDirection != Direction.Up && !isFalling)
        {
            currentDirection = Direction.Down;
            Vector3 newPosition = snakeHead.transform.position + GetMovementStep(Direction.Down);
            if (!IsCollidingWithAnyWall(newPosition) && !WillCollideWithBody(newPosition))
            {
                MoveSnake(newPosition, Direction.Down, GetRotationZ(Direction.Down));
            }
        }
    }

    public void MoveUp()
    {
        if (canMove && currentDirection != Direction.Down && !isFalling)
        {
            currentDirection = Direction.Up;
            Vector3 newPosition = snakeHead.transform.position + GetMovementStep(Direction.Up);
            if (!IsCollidingWithAnyWall(newPosition) && !WillCollideWithBody(newPosition))
            {
                MoveSnake(newPosition, Direction.Up, GetRotationZ(Direction.Up));
            }
        }
    }

    private void MoveSnake(Vector3 newPosition, Direction newDirection, float rotationZ)
    {
        positionHistory.Insert(0, newPosition);
        if (positionHistory.Count > bodyParts.Count + 1)
            positionHistory.RemoveAt(positionHistory.Count - 1);

        directionHistory.Insert(0, newDirection);
        if (directionHistory.Count > bodyParts.Count + 1)
            directionHistory.RemoveAt(directionHistory.Count - 1);

        snakeHead.transform.position = newPosition;
        snakeHead.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        currentDirection = newDirection;

        for (int i = 1; i < bodyParts.Count; i++)
        {
            int historyIndex = i;
            if (historyIndex < positionHistory.Count)
            {
                bodyParts[i].position = positionHistory[historyIndex];

                if (i < bodyParts.Count - 1 && i < directionHistory.Count)
                {
                    Direction from = directionHistory[i];
                    Direction to = directionHistory[i - 1];
                    SpriteRenderer sr = bodyParts[i].GetComponent<SpriteRenderer>();
                    if (sr) sr.sprite = GetBodySprite(from, to);
                }
            }
        }

        UpdateTailRotation();

        if (!IsOnAnyGround())
        {
            StartFalling();
        }
    }

    Vector3 GetMovementStep(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Vector3.up * moveStepY;
            case Direction.Down: return Vector3.down * moveStepY;
            case Direction.Left: return Vector3.left * moveStepX;
            case Direction.Right: return Vector3.right * moveStepX;
            default: return Vector3.zero;
        }
    }

    bool WillCollideWithBody(Vector2 headPosition)
    {
        foreach (Transform bodyPart in bodyParts.GetRange(1, bodyParts.Count - 1))
        {
            if (Vector2.Distance(headPosition, bodyPart.position) < 0.1f)
            {
                return true;
            }
        }
        return false;
    }

    bool IsCollidingWithAnyWall(Vector2 position)
    {
        if (wallTilemaps == null || wallTilemaps.Length == 0) return false;
        return wallTilemaps.Any(wall => wall != null && wall.HasTile(wall.WorldToCell(position)));
    }

    bool IsOnAnyGround()
    {
        if (groundTilemaps == null || groundTilemaps.Length == 0) return false;
        foreach (Vector3 pos in positionHistory)
        {
            bool onGround = groundTilemaps.Any(ground => ground != null && ground.HasTile(ground.WorldToCell(pos)));
            if (onGround)
            {
                return true;
            }
        }
        return false;
    }

    void UpdateTailRotation()
    {
        if (bodyParts.Count < 2) return;

        Transform tail = bodyParts[bodyParts.Count - 1];
        Transform beforeTail = bodyParts[bodyParts.Count - 2];

        Vector3 directionToHead = beforeTail.position - tail.position;
        float angle = -90f;

        if (Mathf.Abs(directionToHead.x) > Mathf.Abs(directionToHead.y))
        {
            angle = directionToHead.x > 0 ? 180f : 0f;
        }
        else
        {
            angle = directionToHead.y > 0 ? -90f : 90f;
        }

        tail.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    Sprite GetBodySprite(Direction prevDir, Direction currDir)
    {
        if (prevDir == currDir)
        {
            return (currDir == Direction.Left || currDir == Direction.Right) ? bodyHorizontal : bodyVertical;
        }
        else
        {
            if ((prevDir == Direction.Up && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Down))
                return cornerTopLeft;
            if ((prevDir == Direction.Up && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Down))
                return cornerTopRight;
            if ((prevDir == Direction.Down && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Up))
                return cornerBottomRight;
            if ((prevDir == Direction.Down && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Up))
                return cornerBottomLeft;
        }
        return bodyVertical;
    }

    private float GetRotationZ(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return 180f;
            case Direction.Down: return 0f;
            case Direction.Left: return 270f;
            case Direction.Right: return 90f;
            default: return 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Banana") && !processedItems.Contains(other.gameObject))
        {
            PushBanana(other.gameObject);
        }
        else if (other.CompareTag("Medicine") && !processedItems.Contains(other.gameObject))
        {
            PushMedicine(other.gameObject);
        }
        else if (other.CompareTag("Hole") && !isTransitioning)
        {
            CheckWinCondition();
        }
    }

    void PushBanana(GameObject banana)
    {
        Vector2 bananaPositionBefore = banana.transform.position;
        bananaPositions[banana] = bananaPositionBefore;

        Vector2 pushDirection = GetMovementStep(currentDirection).normalized;
        Vector2 newBananaPosition = (Vector2)banana.transform.position + pushDirection * new Vector2(gridSize2X, gridSize2Y);

        if (IsCollidingWithAnyWall(newBananaPosition))
        {
            EatBanana(banana);
        }
        else
        {
            banana.transform.position = newBananaPosition;

            if (!IsOnAnyGroundForBanana(newBananaPosition))
            {
                StartFallingBanana(banana);
            }
        }
    }

    void PushMedicine(GameObject medicine)
    {
        Vector2 medicinePositionBefore = medicine.transform.position;
        medicinePositions[medicine] = medicinePositionBefore;

        Vector2 pushDirection = GetMovementStep(currentDirection).normalized;
        Vector2 newMedicinePosition = (Vector2)medicine.transform.position + pushDirection * new Vector2(gridSize2X, gridSize2Y);

        if (IsCollidingWithAnyWall(newMedicinePosition))
        {
            EatMedicine(medicine);
        }
        else
        {
            medicine.transform.position = newMedicinePosition;

            if (!IsOnAnyGroundForBanana(newMedicinePosition))
            {
                StartFallingMedicine(medicine);
            }
        }
    }

    void EatBanana(GameObject banana)
    {
        if (processedItems.Contains(banana))
        {
            return;
        }

        Vector2 headPositionBefore = snakeHead.transform.position;

        int tailIndex = bodyParts.Count - 1;
        Vector2 tailPosition = bodyParts[tailIndex].position;
        Vector2 previousPosition = (tailIndex > 0) ? bodyParts[tailIndex - 1].position : bodyParts[0].position;
        Vector2 tailDirection = (tailPosition - previousPosition).normalized;
        Vector2 newTailPosition = tailPosition + tailDirection * moveStepY;

        bodyLength++;
        Transform[] newBodyParts = new Transform[bodyParts.Count + 1];
        bodyParts.CopyTo(0, newBodyParts, 0, bodyParts.Count);

        GameObject newBodyPart = Instantiate(snakeBody, tailPosition, Quaternion.identity);
        newBodyPart.transform.SetParent(bodyParts[0].transform.parent);
        BoxCollider2D newBodyCollider = newBodyPart.AddComponent<BoxCollider2D>();
        newBodyCollider.size = new Vector2(moveStepX, moveStepY);
        SpriteRenderer sr = newBodyPart.GetComponent<SpriteRenderer>();
        if (sr == null) sr = newBodyPart.AddComponent<SpriteRenderer>();
        sr.sprite = bodyVertical;
        bodyParts.Insert(tailIndex, newBodyPart.transform);
        positionHistory.Insert(tailIndex, tailPosition);
        directionHistory.Insert(tailIndex, directionHistory[tailIndex - 1]);

        bodyParts[tailIndex + 1].position = newTailPosition;
        positionHistory[tailIndex + 1] = newTailPosition;

        if (bananaCount > 0)
        {
            bananaCount--;
        }

        processedItems.Add(banana);
        Destroy(banana);
        SetFaceMouthActive(false);
        suppressMouthTemporarily = true;
        StartCoroutine(UnsuppressMouthAfterDelay(1f));
        SetHeadFaceTemporary(faceEatBanana, 1f);
    }

    private IEnumerator EatMedicineCoroutine(GameObject medicine)
    {
        if (processedItems.Contains(medicine))
            yield break;

        processedItems.Add(medicine);
        Destroy(medicine);

        if (medicineCount > 0)
            medicineCount--;

        Direction pushDirection = GetOppositeDirection(currentDirection);
        float rotationZ = GetRotationZ(pushDirection);
        Vector3 moveStep = GetMovementStep(pushDirection);

        GameObject draggedBanana = null;

        while (true)
        {
            SetHeadFace(faceTakeMedicinePush);
            List<Vector3> nextSnakePositions = bodyParts.Select(part => part.position + moveStep).ToList();

            bool blocked = false;

            for (int i = 0; i < nextSnakePositions.Count; i++)
            {
                Vector3 nextPos = nextSnakePositions[i];

                if (IsCollidingWithAnyWall(nextPos))
                {
                    blocked = true;
                    break;
                }

                Collider2D[] hits = Physics2D.OverlapCircleAll(nextPos, 0.1f);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Medicine"))
                    {
                        blocked = true;
                        break;
                    }

                    if (hit.CompareTag("Banana"))
                    {
                        Vector3 bananaNextPos = hit.transform.position + moveStep;
                        if (IsCollidingWithAnyWall(bananaNextPos))
                        {
                            blocked = true;
                            break;
                        }

                        if (draggedBanana == null)
                        {
                            draggedBanana = hit.gameObject;
                        }
                    }
                }

                if (blocked)
                    break;
            }

            if (blocked)
                break;

            levelManager.snakeParent.transform.position += moveStep;

            if (draggedBanana != null)
            {
                draggedBanana.transform.position += moveStep;
            }

            for (int i = 0; i < positionHistory.Count; i++)
            {
                positionHistory[i] += moveStep;
            }

            directionHistory.Insert(0, pushDirection);
            if (directionHistory.Count > bodyParts.Count + 1)
                directionHistory.RemoveAt(directionHistory.Count - 1);

            UpdateTailRotation();

            yield return new WaitForSeconds(0.15f);
        }
        canMove = true;
        SetPushEffectsActive(false);
        SetHeadFace(faceNormal);
    }

    void EatMedicine(GameObject medicine)
    {
        canMove = false;
        SetHeadFace(faceTakeMedicine);
        SetPushEffectsActive(true);
        StartCoroutine(EatMedicineCoroutine(medicine));
    }

    private void CheckWinCondition()
    {
        if (!isTransitioning && bananaCount <= 0 && medicineCount <= 0 && levelManager != null)
        {
            isTransitioning = true; 
            int nextLevelIndex = levelManager.currentLevelIndex + 1;
            if (nextLevelIndex < levelManager.levelPrefabs.Length)
            {
                levelManager.LoadLevel(nextLevelIndex);
                bananaCount = levelManager.bananaCountsPerLevel[nextLevelIndex];
                medicineCount = levelManager.medicineCountsPerLevel[nextLevelIndex];
                processedItems.Clear();
                isTransitioning = false; 
            }
            else
            {
                Debug.Log("All levels completed!");
                isTransitioning = false;
            }
        }
    }

    private Direction GetOppositeDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
            default: return Direction.Down;
        }
    }


    bool IsOnAnyGroundForBanana(Vector2 position)
    {
        if (groundTilemaps == null || groundTilemaps.Length == 0) return false;
        foreach (Tilemap ground in groundTilemaps)
        {
            if (ground != null && ground.HasTile(ground.WorldToCell(position)))
            {
                return true;
            }
        }
        return false;
    }

    void StartFallingBanana(GameObject banana)
    {
        SetHeadFaceTemporary(faceFallFruit, 1f);
        StartCoroutine(FallRoutineBanana(banana));
    }

    void StartFallingMedicine(GameObject medicine)
    {
        SetHeadFaceTemporary(faceFallFruit, 1f);
        StartCoroutine(FallRoutineMedicine(medicine));
    }

    IEnumerator FallRoutineBanana(GameObject banana)
    {
        float fallSpeed = 1f;
        while (true)
        {
            banana.transform.position += Vector3.down * fallSpeed;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator FallRoutineMedicine(GameObject medicine)
    {
        float fallSpeed = 1f;
        while (true)
        {
            medicine.transform.position += Vector3.down * fallSpeed;
            yield return new WaitForSeconds(0.1f);
        }
    }

    void StartFalling()
    {
        isFalling = true;
        SetHeadFace(faceFallOffMap);
        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        float fallSpeed = 1f;
        while (true)
        {
            foreach (Transform part in bodyParts)
            {
                part.position += Vector3.down * fallSpeed;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void SetHeadFace(Sprite faceSprite)
    {
        if (faceRenderer != null && faceSprite != null)
        {
            faceRenderer.sprite = faceSprite;
        }
    }

    private void SetHeadFaceTemporary(Sprite faceSprite, float delay)
    {
        if (faceRenderer != null && faceSprite != null)
        {
            faceRenderer.sprite = faceSprite;

            if (faceResetCoroutine != null)
            {
                StopCoroutine(faceResetCoroutine);
            }
            faceResetCoroutine = StartCoroutine(ResetFaceAfterDelay(delay));
        }
    }

    private IEnumerator ResetFaceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetHeadFace(faceNormal);
    }

    private void SetPushEffectsActive(bool isActive)
    {
        if (isActive)
        {
            if (pushEffect1 != null) pushEffect1.SetActive(true);
            if (pushEffect2 != null) pushEffect2.SetActive(true);

            if (pushEffectHideCoroutine != null)
            {
                StopCoroutine(pushEffectHideCoroutine);
                pushEffectHideCoroutine = null;
            }
        }
        else
        {
            if (pushEffectHideCoroutine == null)
            {
                pushEffectHideCoroutine = StartCoroutine(HidePushEffectsAfterDelay(0.3f));
            }
        }
    }

    private IEnumerator HidePushEffectsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pushEffect1 != null) pushEffect1.SetActive(false);
        if (pushEffect2 != null) pushEffect2.SetActive(false);

        pushEffectHideCoroutine = null;
    }

    private void SetFaceMouthActive(bool isActive)
    {
        if (faceMouthObject != null)
            faceMouthObject.SetActive(isActive);
    }

    private IEnumerator UnsuppressMouthAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        suppressMouthTemporarily = false;
    }
}