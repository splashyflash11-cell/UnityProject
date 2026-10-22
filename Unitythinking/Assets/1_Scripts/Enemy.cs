using System.Collections.Generic;
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
    private readonly List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool isInitialized = false;
    private bool isInvulnerable;
    private bool isDead;

    float hackingTimer;

    Vector3 targetPosition;

    SpriteRenderer spriteRenderer;
    private readonly Collider2D[] towerBuffer = new Collider2D[64];
    private ContactFilter2D towerFilter;
    private int towerLayerMask;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        towerLayerMask = LayerMask.GetMask("tower");
        towerFilter = new ContactFilter2D();
        towerFilter.SetLayerMask(towerLayerMask);
        towerFilter.useTriggers = true;
        if (data != null)
        {
            InitData();
        }
    }

    public void InitData()
    {
        if (data == null) return;
        currentHealth = data.maxHealth;
        moveSpeed = data.moveSpeed;
        isInvulnerable = data.isInvulnerable;
        isDead = false;
        isInitialized = false;
        currentWaypointIndex = 0;
        waypoints.Clear();
        if (hitText != null) hitText.gameObject.SetActive(false);
        if (healthSlider != null && data.maxHealth > 0f) healthSlider.value = 1f;
        if (data.isStealth)
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }
        hackingTimer = 0;
    }

    public void SetupPath(List<Vector2Int> cellPath)
    {
        if (cellPath == null || RandomPathGenerator.Instance == null) return;
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
        if (!isInitialized || isDead || data == null || currentWaypointIndex >= waypoints.Count)
            return;

        targetPosition = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        if (spriteRenderer != null) spriteRenderer.sortingOrder = (int)-transform.position.y;

        if ((transform.position - targetPosition).sqrMagnitude < 0.0025f)
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
                SetDirectionSprite(0);
                else if (targetPosition.x > transform.position.x && targetPosition.y > transform.position.y)
                SetDirectionSprite(1);
                else if (targetPosition.x > transform.position.x && targetPosition.y < transform.position.y)
                SetDirectionSprite(2);
                else if (targetPosition.x < transform.position.x && targetPosition.y < transform.position.y)
                SetDirectionSprite(3);
            }
        }


        if (data.hacking)
        {
            hackingTimer += Time.deltaTime;
            if (hackingTimer >= 3)
            {
                hackingTimer = 0f;
                int hitCount = Physics2D.OverlapCircle(transform.position, 20f, towerFilter, towerBuffer);
                int towerCount = 0;
                for (int i = 0; i < hitCount; i++)
                    if (towerBuffer[i] != null && towerBuffer[i].GetComponentInParent<Tower>() != null)
                        towerCount++;
                if (towerCount == 0) return;

                int selectedIndex = Random.Range(0, towerCount);
                Tower selectedTower = null;
                for (int i = 0; i < hitCount; i++)
                {
                    Collider2D hit = towerBuffer[i];
                    Tower candidate = hit != null ? hit.GetComponentInParent<Tower>() : null;
                    if (candidate == null) continue;
                    if (selectedIndex-- == 0)
                    {
                        selectedTower = candidate;
                        break;
                    }
                }
                if (selectedTower == null || selectedTower.data == null) return;
                if (selectedTower.isHacked)
                {
                    GameLog.Info($"{data.enemyName}이(가) {selectedTower.data.towerName} 을 해킹하려 했지만 이미 해킹된 상태입니다.");
                    return;
                }
                selectedTower.Hacking();
                GameLog.Info($"{data.enemyName}이(가) {selectedTower.data.towerName} 을 해킹했습니다!");
            }
        }
    }

    private void SetDirectionSprite(int index)
    {
        if (spriteRenderer != null && data.enemySprite != null && index < data.enemySprite.Length && data.enemySprite[index] != null)
            spriteRenderer.sprite = data.enemySprite[index];
    }

    private void OnReachMainTower()
    {
        if (isDead) return;
        isDead = true;
        GameLog.Info($"{data.enemyName}이(가) 메인프레임에 도달했습니다!");
        if (GameManager.Instance != null) GameManager.Instance.TakeDamage(data.attackDamage);
        Destroy(gameObject);
    }

    public void TakeDamage(float damage, bool isArmorReduce = false)
    {
        if (isDead || data == null || damage <= 0f) return;
        if (isInvulnerable)
        {
            if (isArmorReduce)
            {
                GameLog.Info($"{data.enemyName}은(는) 무적이 해제됩니다.");
                isInvulnerable = false;
                return;
            }
            else
            {
                GameLog.Info($"{data.enemyName}은(는) 무적 상태입니다. 피해를 입지 않았습니다.");
                return;
            }
        }
        currentHealth -= damage;
        GameLog.Info($"{data.enemyName}이(가) {damage}만큼 피해를 입었습니다. 남은 체력: {currentHealth}");
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        if (healthSlider != null && data.maxHealth > 0f)
            healthSlider.value = Mathf.Clamp01(currentHealth / data.maxHealth);
    }

    public void ShowHitText(float damage)
    {
        if (hitText == null || isDead) return;
        hitText.gameObject.SetActive(true);
        hitText.text = $"{damage}";
        Invoke(nameof(HideHitText), 0.5f);
    }

    void HideHitText()
    {
        hitText.gameObject.SetActive(false);
    }
    void Die()
    {
        if (isDead) return;
        isDead = true;
        // 적이 죽었을 때의 처리 (예: 점수 증가, 아이템 드롭, 재화 획득 등)
        GameLog.Info($"{data.enemyName}이(가) 사망했습니다.");

        if (GameManager.Instance != null) GameManager.Instance.ChangeCpu(data.cpuReward);


        Destroy(gameObject);


    }
}