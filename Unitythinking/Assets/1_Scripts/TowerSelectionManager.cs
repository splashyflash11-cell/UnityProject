using UnityEngine;
using UnityEngine.EventSystems;

public class TowerSelectionManager : MonoBehaviour
{
    public static TowerSelectionManager Instance;

    [SerializeField] private TowerInfoPanel infoPanel;

    private Tower selectedTower;
    private Camera cam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        cam = Camera.main;
        if (infoPanel != null) infoPanel.Hide();
    }

    private void Update()
    {
        var placement = TowerPlacementManager.Instance;

        // 배치 모드 중에는 선택 해제 + 클릭 무시
        if (placement != null && placement.IsPlacing)
        {
            if (selectedTower != null) Deselect();
            return;
        }

        // 우클릭 / ESC → 선택 해제
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;

        // UI(버튼, 정보 패널) 위 클릭은 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // 방금 타워를 설치한 클릭이면 무시
        if (placement != null && placement.LastPlacementFrame == Time.frameCount) return;

        Tower clicked = GetTowerUnderMouse();
        if (clicked != null) Select(clicked);
        else Deselect();
    }

    private Tower GetTowerUnderMouse()
    {
        if (cam == null) cam = Camera.main;

        Vector3 mp = Input.mousePosition;
        mp.z = -cam.transform.position.z;
        Vector2 world = cam.ScreenToWorldPoint(mp);

        // 레이어 마스크를 쓰지 않음 → 해킹돼서 Enemy 레이어가 된 타워도 선택 가능
        Collider2D[] hits = Physics2D.OverlapPointAll(world);

        Tower best = null;
        int bestOrder = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            Tower t = hit.GetComponentInParent<Tower>();
            if (t == null) continue;

            // 겹쳐 있으면 화면에서 가장 앞에 그려진 타워 선택
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            int order = sr != null ? sr.sortingOrder : 0;
            if (order > bestOrder)
            {
                bestOrder = order;
                best = t;
            }
        }
        return best;
    }

    private void Select(Tower tower)
    {
        selectedTower = tower;
        if (infoPanel != null) infoPanel.Show(tower);
    }

    public void Deselect()
    {
        selectedTower = null;
        if (infoPanel != null) infoPanel.Hide();
    }
}