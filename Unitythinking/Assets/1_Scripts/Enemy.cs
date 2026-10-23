using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public Slider healthSlider; // 체력바 UI를 연결할 Slider
    public TextMeshProUGUI hitText;

    [Header("적 데이터")]
    public EnemyData data; // 에디터에서 해당 프리팹에 맞는 EnemyData 할당

    private float currentHealth;
    private float moveSpeed;
    private bool isInvulnerable;   // 인스턴스별 무적 상태 (data 에셋은 건드리지 않음)
    private bool isDead;
    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool isInitialized = false;

    float hackingTimer;

    Vector3 targetPosition;

    SpriteRenderer spriteRenderer;

    // 정보 패널(도감)용 읽기 전용 값
    public float CurrentHealth => currentHealth;
    public float MoveSpeed => moveSpeed;
    public bool IsInvulnerable => isInvulnerable;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (data != null)
        {
            InitData();
        }
    }

    public void InitData()
    {
        currentHealth = data.maxHealth;
        moveSpeed = data.moveSpeed;
        isInvulnerable = data.isInvulnerable;
        isDead = false;
        hitText.gameObject.SetActive(false); // 초기에는 히트 텍스트를 비활성화
        if (data.isStealth)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f); // 반투명 처리
        }
        else
        {
            spriteRenderer.color = Color.white; // 원래 색상으로 복원
        }
        hackingTimer = 0;
    }

    public void SetupPath(List<Vector2Int> cellPath)
    {
        waypoints.Clear();
        foreach (Vector2Int cell in cellPath)
        {
            waypoints.Add(RandomPathGenerator.Instance.GetWorldPosFromCell(cell));
        }

        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0];
            currentWaypointIndex = 1;
            isInitialized = true;
        }


    }

    private void Update()
    {
        if (!isInitialized || waypoints == null || currentWaypointIndex >= waypoints.Count)
            return;

        targetPosition = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        spriteRenderer.sortingOrder = (int)-transform.position.y;

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Count)
            {
                OnReachMainTower();
            }
            else
            {
                targetPosition = waypoints[currentWaypointIndex];
                if (targetPosition.x < transform.position.x && targetPosition.y > transform.position.y)
                    GetComponent<SpriteRenderer>().sprite = data.enemySprite[0]; // 좌상
                else if (targetPosition.x > transform.position.x && targetPosition.y > transform.position.y)
                    GetComponent<SpriteRenderer>().sprite = data.enemySprite[1]; // 우상
                else if (targetPosition.x > transform.position.x && targetPosition.y < transform.position.y)
                    GetComponent<SpriteRenderer>().sprite = data.enemySprite[2]; // 우하
                else if (targetPosition.x < transform.position.x && targetPosition.y < transform.position.y)
                    GetComponent<SpriteRenderer>().sprite = data.enemySprite[3]; // 좌하
            }
        }


        if (data.hacking)
        {
            hackingTimer += Time.deltaTime;
            if (hackingTimer >= 3)
            {
                hackingTimer = 0f;
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 20, LayerMask.GetMask("tower"));
                if (hits.Count() == 0)
                {
                    return;
                }
                Tower selectedTower = hits[Random.Range(0, hits.Length)].gameObject.GetComponent<Tower>();

                if (selectedTower.isHacked)
                {
                    Debug.Log($"{data.enemyName}이(가) {selectedTower.data.towerName} 을 해킹하려 했지만 이미 해킹된 상태입니다.");
                    return;
                }
                selectedTower.Hacking();
                Debug.Log($"{data.enemyName}이(가) {selectedTower.data.towerName} 을 해킹했습니다!");
            }
        }

    }

    private void OnReachMainTower()
    {
        Debug.Log($"{data.enemyName}이(가) 메인프레임에 도달했습니다!");
        GameManager.Instance.TakeDamage(data.attackDamage);
        Destroy(gameObject);
    }

    public void TakeDamage(float damage, bool isArmorReduce = false, Tower attacker = null)
    {
        if (isDead) return;   // 같은 프레임에 여러 발이 맞아도 보상/처치가 중복되지 않게

        if (isInvulnerable)
        {
            if (isArmorReduce)
            {
                Debug.Log($"{data.enemyName}은(는) 무적이 해제됩니다.");
                isInvulnerable = false;
                return;
            }
            else
            {
                Debug.Log($"{data.enemyName}은(는) 무적 상태입니다. 피해를 입지 않았습니다.");
                return;
            }
        }

        float dealt = Mathf.Min(damage, currentHealth);   // 남은 체력 이상은 누적 피해에 넣지 않음
        currentHealth -= damage;
        if (attacker != null) attacker.AddDamage(dealt);

        Debug.Log($"{data.enemyName}이(가) {damage}만큼 피해를 입었습니다. 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            healthSlider.value = 0f;
            if (attacker != null) attacker.AddKill();
            Die();
            return;
        }

        healthSlider.value = currentHealth / data.maxHealth;    // 체력바 업데이트
    }

    public void ShowHitText(float damage)
    {
        hitText.gameObject.SetActive(true);
        hitText.text = $"{damage}";
        Invoke("HideHitText", 0.5f);
    }

    void HideHitText()
    {
        hitText.gameObject.SetActive(false);
    }
    void Die()
    {
        isDead = true;

        // 적이 죽었을 때의 처리 (예: 점수 증가, 아이템 드롭, 재화 획득 등)
        Debug.Log($"{data.enemyName}이(가) 사망했습니다.");

        GameManager.Instance.ChangeCpu(data.cpuReward); // 적을 처치하면 보상으로 CPU를 증가시킴


        Destroy(gameObject);


    }
}