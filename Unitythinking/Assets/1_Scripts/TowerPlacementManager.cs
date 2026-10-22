using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance;

    [Header("2:1 아이소메트릭 타일 설정")]
    public float tileWidth = 1.0f;        // 타일 가로 폭
    public int mapWidth = 14;             // 타일맵 가로 크기
    public int mapHeight = 14;            // 타일맵 세로 크기

    [Header("레이어 설정")]
    public LayerMask unplaceableLayer;    // 장애물/기존 타워 레이어

    [Header("상태 관리")]
    private GameObject currentTowerPrefab;
    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private bool isPlacing = false;
    private bool isPlacingMainTower = false; // 현재 메인 타워 배치 중인지 여부
    bool isWall = false; // 현재 선택한 타워가 벽인지 여부
    private readonly HashSet<Vector2Int> towerPositions = new HashSet<Vector2Int>();
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> previewPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private Camera mainCamera;
    private TowerData selectedTowerData;
    public GameObject mainTower;
    public bool IsPlacing => isPlacing;
    public int LastPlacementFrame { get; private set; } = -1;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        CancelPlacement();
        foreach (ObjectPool<GameObject> pool in previewPools.Values) pool.Clear();
        previewPools.Clear();
    }

    private void Update()
    {
        if (!isPlacing) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2Int cellIndex = GetCellIndex(mouseWorldPos);
        Vector3 snappedPos = GetWorldPosFromCell(cellIndex);

        if (previewObject != null)
        {
            previewObject.transform.position = snappedPos;

            // 배치 조건 검사
            bool isValid = false;
            if (isPlacingMainTower)
            {
                // 메인 타워: 맵 안이기만 하면 배치 가능하나, 이미 메인 타워가 설치된 경우 불가
                isValid = IsInMapBounds(cellIndex) && !(RandomPathGenerator.Instance != null && RandomPathGenerator.Instance.isMainTowerPlaced);
            }
            else
            {
                // 일반 타워: 길(Path)을 제외하고 맵 내부인 곳만 배치 가능
                if (GameManager.Instance == null || selectedTowerData == null ||
                    GameManager.Instance.cpu < selectedTowerData.useCpu ||
                    GameManager.Instance.ram + selectedTowerData.useRam > GameManager.Instance.maxRam)
                {
                    isValid = false; // CPU 부족 시 배치 불가
                }
                else
                {
                    isValid = RandomPathGenerator.Instance != null && RandomPathGenerator.Instance.CanPlaceNormalTower(cellIndex, isWall, towerPositions);
                }
            }

            // 색상 피드백
            if (previewRenderer != null)
            {
                previewRenderer.color = isValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            }

            // 클릭 배치 처리
            if (Input.GetMouseButtonDown(0) && isValid)
            {
                GameObject placedTower = Instantiate(currentTowerPrefab, snappedPos, Quaternion.identity);
                TowerData towerData = selectedTowerData;
                if (towerData == null || GameManager.Instance == null)
                GameManager.Instance.ChangeCpu(-towerData.useCpu); // CPU 차감
                towerPositions.Add(cellIndex); // 배치된 타워 위치 저장

                if (!isPlacingMainTower)
                {
                    GameManager.Instance.SetRam(GameManager.Instance.ram + towerData.useRam);
                }
                else
                {
                    // 메인 타워를 놓았다면 모서리 1~3곳에서 길을 생성
                    mainTower = placedTower; // 메인 타워 참조 저장
                    if (RandomPathGenerator.Instance != null) RandomPathGenerator.Instance.OnPlaceMainTower(cellIndex);
                }

                CancelPlacement();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    // 마우스 월드 좌표 -> 셀 인덱스(cellX, cellY) 계산
    private Vector2Int GetCellIndex(Vector3 position)
    {
        float W = tileWidth;
        float H = tileWidth * 0.5f;

        float isoX = (position.x / (W * 0.5f) + position.y / (H * 0.5f)) * 0.5f;
        float isoY = (position.y / (H * 0.5f) - position.x / (W * 0.5f)) * 0.5f;

        return new Vector2Int(Mathf.FloorToInt(isoX), Mathf.FloorToInt(isoY));
    }

    // 셀 인덱스 -> 셀 중심 월드 좌표 계산
    private Vector3 GetWorldPosFromCell(Vector2Int cell)
    {
        float W = tileWidth;
        float H = tileWidth * 0.5f;

        float snapX = (cell.x - cell.y) * (W * 0.5f);
        float snapY = (cell.x + cell.y + 1f) * (H * 0.5f);

        return new Vector3(snapX, snapY, 0f);
    }

    // (0,0) 중심 14x14 범위 검사 (-7 <= index <= 6)
    private bool IsInMapBounds(Vector2Int cell)
    {
        int minX = -mapWidth / 2;               // -7
        int maxX = minX + mapWidth - 1;         // 6
        int minY = -mapHeight / 2;              // -7
        int maxY = minY + mapHeight - 1;        // 6

        return (cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY);
    }

    // 충돌 장애물 검사
    private bool CanPlaceTower(Vector3 position)
    {
        float H = tileWidth * 0.5f;
        Collider2D hit = Physics2D.OverlapBox(position, new Vector2(tileWidth * 0.5f, H * 0.5f), 0f, unplaceableLayer);
        return hit == null;
    }

    public void StartPlacement(GameObject towerPrefab, bool isMainTower = false)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        // 메인 타워가 이미 설치되어 있으면 추가 배치 불가
        if (isMainTower && RandomPathGenerator.Instance != null && RandomPathGenerator.Instance.isMainTowerPlaced)
        {
            GameLog.Info("Main tower already placed. Cannot start another main tower placement.");
            return;
        }

        if (isPlacing) CancelPlacement();

        if (towerPrefab == null) return;
        Tower tower = towerPrefab.GetComponent<Tower>();
        if (tower == null || tower.data == null) return;
        currentTowerPrefab = towerPrefab;
        selectedTowerData = tower.data;
        isPlacing = true;
        isWall = towerPrefab.CompareTag("Wall"); // 벽 타워인지 여부 확인
        isPlacingMainTower = isMainTower; // 메인 타워 설치 여부 저장

        previewObject = GetPreview(towerPrefab);
        previewRenderer = previewObject.GetComponent<SpriteRenderer>();

        Tower t = previewObject.GetComponent<Tower>();
        if (t) t.enabled = false;

        Collider2D col = previewObject.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;
        return worldPos;
    }

    private void PlaceTower(Vector3 position)
    {
        Instantiate(currentTowerPrefab, position, Quaternion.identity);
        CancelPlacement();
    }

    public void CancelPlacement()
    {
        LastPlacementFrame = Time.frameCount;   // 설치 클릭이 선택 클릭으로 겹치는 것 방지

        if (previewObject != null)
        {
            if (currentTowerPrefab != null && previewPools.TryGetValue(currentTowerPrefab, out ObjectPool<GameObject> pool))
                pool.Release(previewObject);
            else
                Destroy(previewObject);
        }
        isPlacing = false;
        isPlacingMainTower = false; // 추가
        currentTowerPrefab = null;
        selectedTowerData = null;
        previewObject = null;
        previewRenderer = null;
    }

    private GameObject GetPreview(GameObject prefab)
    {
        if (!previewPools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
        {
            ObjectPool<GameObject> createdPool = null;
            createdPool = new ObjectPool<GameObject>(
                () =>
                {
                    GameObject instance = Instantiate(prefab, transform);
                    instance.SetActive(false);
                    return instance;
                },
                instance => instance.SetActive(true),
                instance => instance.SetActive(false),
                instance => Destroy(instance), true, 1, 4);
            pool = createdPool;
            previewPools.Add(prefab, pool);
        }
        return pool.Get();
    }
}