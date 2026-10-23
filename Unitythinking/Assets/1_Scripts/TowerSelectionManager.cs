using UnityEngine;
using UnityEngine.EventSystems;

// 타워/적 선택 관리: 호버 = 미리보기, 클릭 = 고정
public class TowerSelectionManager : MonoBehaviour
{
    public static TowerSelectionManager Instance;

    [Header("UI")]
    [SerializeField] private TowerInfoPanel infoPanel;

    [Header("판매")]
    [Range(0f, 1f)]
    [SerializeField] private float sellRefundRatio = 0.5f;   // 판매 시 CPU 환급 비율
    public float SellRefundRatio => sellRefundRatio;

    [Header("표시 설정")]
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color selectColor = new Color(0.3f, 1f, 1f, 1f);
    [SerializeField] private float outlineThickness = 0.04f;
    [SerializeField] private Material outlineMaterial;       // 선택 사항 (단색 출력 머티리얼)

    private Tower selectedTower, hoverTower;
    private Enemy selectedEnemy, hoverEnemy;

    private Camera cam;
    private RangeIndicator rangeIndicator;
    private SelectionOutline outline;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        cam = Camera.main;
        rangeIndicator = RangeIndicator.Create("SelectionRange");
        outline = SelectionOutline.Create(outlineThickness, outlineMaterial);
        if (infoPanel != null) infoPanel.Hide();
    }

    private void Update()
    {
        TowerPlacementManager placement = TowerPlacementManager.Instance;

        // 배치 모드 중에는 선택/호버를 모두 끈다
        if (placement != null && placement.IsPlacing)
        {
            ClearAll();
            return;
        }

        // 우클릭 / ESC → 선택 해제
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            Deselect();

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (overUI)
        {
            hoverTower = null;
            hoverEnemy = null;
        }
        else
        {
            FindUnderMouse();
        }

        // 방금 타워를 설치한 클릭은 선택으로 취급하지 않는다
        bool justPlaced = placement != null && placement.LastPlacementFrame == Time.frameCount;

        if (Input.GetMouseButtonDown(0) && !overUI && !justPlaced)
        {
            if (hoverTower != null)
            {
                selectedTower = hoverTower;
                selectedEnemy = null;
            }
            else if (hoverEnemy != null)
            {
                selectedEnemy = hoverEnemy;
                selectedTower = null;
            }
            else
            {
                Deselect();
            }
        }

        UpdateFocus();
    }

    // 마우스 아래의 타워/적 중 화면에서 가장 앞에 그려진 것을 찾는다
    private void FindUnderMouse()
    {
        hoverTower = null;
        hoverEnemy = null;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 mp = Input.mousePosition;
        mp.z = -cam.transform.position.z;
        Vector2 world = cam.ScreenToWorldPoint(mp);

        int bestOrder = int.MinValue;

        // 레이어 마스크를 쓰지 않음 → 해킹돼서 Enemy 레이어가 된 타워도 선택 가능
        foreach (Collider2D hit in Physics2D.OverlapPointAll(world))
        {
            Tower t = hit.GetComponentInParent<Tower>();
            Enemy e = t != null ? null : hit.GetComponentInParent<Enemy>();
            if (t == null && e == null) continue;

            SpriteRenderer sr = t != null ? t.GetComponent<SpriteRenderer>() : e.GetComponent<SpriteRenderer>();
            int order = sr != null ? sr.sortingOrder : 0;
            if (order <= bestOrder) continue;

            bestOrder = order;
            hoverTower = t;
            hoverEnemy = e;
        }
    }

    // 지금 화면에 보여줄 대상(고정 > 호버)을 정하고 패널/윤곽선/사거리를 갱신
    private void UpdateFocus()
    {
        Tower t = null;
        Enemy e = null;
        bool pinned = false;

        if (selectedTower != null) { t = selectedTower; pinned = true; }
        else if (selectedEnemy != null) { e = selectedEnemy; pinned = true; }
        else if (hoverTower != null) t = hoverTower;
        else if (hoverEnemy != null) e = hoverEnemy;

        Color c = pinned ? selectColor : hoverColor;

        if (t != null)
        {
            if (infoPanel != null) infoPanel.ShowTower(t, pinned);
            outline.Follow(t.GetComponent<SpriteRenderer>(), c);
            float range = t.data != null ? t.data.attackRange : 0f;
            rangeIndicator.Show(t.transform.position, range, c);
        }
        else if (e != null)
        {
            if (infoPanel != null) infoPanel.ShowEnemy(e, pinned);
            outline.Follow(e.GetComponent<SpriteRenderer>(), c);
            rangeIndicator.Hide();
        }
        else
        {
            if (infoPanel != null) infoPanel.Hide();
            outline.Clear();
            rangeIndicator.Hide();
        }
    }

    private void ClearAll()
    {
        selectedTower = null;
        selectedEnemy = null;
        hoverTower = null;
        hoverEnemy = null;
        UpdateFocus();
    }

    public void Deselect()
    {
        selectedTower = null;
        selectedEnemy = null;
    }

    // 정보 패널의 [판매] 버튼에서 호출
    public void SellSelected()
    {
        if (selectedTower == null || !selectedTower.CanSell) return;

        Tower t = selectedTower;
        selectedTower = null;
        t.Sell(sellRefundRatio);
    }
}