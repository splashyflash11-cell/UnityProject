using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("타워 데이터")]
    public TowerData data;

    [Header("체력 설정")]
    public int maxHp = 100;
    public int currentHp;

    [Header("발사 설정")]
    public GameObject projectilePrefab; // 총알 프리팹
    public Transform firePoint;          // 총알이 발사될 위치 (선택 사항, 없으면 타워 위치)

    [Header("해킹 경고 아이콘")]
    [SerializeField] private float warningIconOffset = 0.2f; // 스프라이트 위쪽 여백

    private float attackTimer;
    private Transform target;

    private SpriteRenderer spriteRenderer;

    public bool isHacked = false;
    private Firewall firewall;
    // ── 배치 정보 / 통계 ──
    [HideInInspector] public Vector2Int cell;   // 설치된 셀 (TowerPlacementManager 가 설정)
    private float placedTime;
    private bool released;                      // 환급/제거 처리가 끝났는지

    // ── 랜섬웨어 감염 ──
    public bool isLocked { get; private set; }    // 감염(잠김) 상태: 복호화 전까지 공격 불가
    public int DecryptCost { get; private set; }  // 복호화에 필요한 CPU
    private Color baseColor = Color.white;
    private GameObject lockIcon;

    public float TotalDamage { get; private set; }
    public int KillCount { get; private set; }
    public float AliveTime => Time.time - placedTime;

    public bool IsMainTower =>
        TowerPlacementManager.Instance != null && TowerPlacementManager.Instance.mainTower == gameObject;

    // 판매 가능: 해킹/감염되지 않은 일반 타워
    public bool CanSell => !isHacked && !isLocked && !IsMainTower && data != null;

    // 감염 가능: 이미 감염/해킹되지 않은 일반 타워
    public bool CanInfect => !isLocked && !isHacked && !IsMainTower;

    private void Start()
    {
        firewall = GetComponent<Firewall>();
        currentHp = maxHp;
        placedTime = Time.time;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = (int)-transform.position.y;
        }
    }

    private void Update()
    {
        if (firewall != null) return;
        // 랜섬웨어에 감염되면 복호화 전까지 공격하지 않는다
        if (isLocked)
        {
            attackTimer = 0f;
            target = null;
            return;
        }

        attackTimer += Time.deltaTime;

        FindTarget();

        // 타겟이 존재하고 공격 주기가 되면 발사
        if (target != null)
        {
            if (attackTimer >= (!isHacked ? data.attackInterval : data.attackInterval * 2f))
            {
                Attack();
                attackTimer = 0f;
            }
        }
    }

    // 아이소메트릭 2:1 타원 사거리 감지
    private void FindTarget()
    {
        if (data == null)
        {
            target = null;
            return;
        }

        Transform closestEnemy = null;
        float minNormalizedDistSq = float.MaxValue;

        if (isHacked)
        {
            target = TowerPlacementManager.Instance != null && TowerPlacementManager.Instance.mainTower != null
                ? TowerPlacementManager.Instance.mainTower.transform
                : null;
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, data.attackRange, LayerMask.GetMask("Enemy"));

        foreach (Collider2D hit in hits)
        {
            Vector3 diff = hit.transform.position - transform.position;

            // 2:1 아이소메트릭 비율 적용
            float scaledY = diff.y * 2.0f;
            float normalizedDistSq = (diff.x * diff.x) + (scaledY * scaledY);

            if (normalizedDistSq <= data.attackRange * data.attackRange)
            {
                if (normalizedDistSq < minNormalizedDistSq)
                {
                    Enemy enemy = hit.GetComponentInParent<Enemy>();
                    Tower hackedTower = hit.GetComponentInParent<Tower>();
                    if (enemy == null && (hackedTower == null || !hackedTower.isHacked))
                        continue;

                    if (enemy != null && enemy.data != null && enemy.data.isStealth)
                    {
                        if (data.debuffType != DebuffType.RADAR)
                            continue;
                    }
                    minNormalizedDistSq = normalizedDistSq;
                    closestEnemy = enemy != null ? enemy.transform : hackedTower.transform;
                }
            }
        }

        target = closestEnemy;
    }

    // 실제 총알 생성 및 발사 함수
    private void Attack()
    {
        if (projectilePrefab == null || target == null) return;

        // 발사 위치 설정 (firePoint가 없으면 타워 자신 위치)
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        // 총알 생성
        GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile projectile = projObj.GetComponent<Projectile>();

        projectile.lureSource = GetComponent<HoneyPot>();

        if (projectile != null)
        {
            if (isHacked)
            {
                projectile.Setup(
                    target,
                    (int)(data.attackDamage * 0.1f),
                    data.isDebuffTower,
                    data.debuffType,
                    data.debuffValue,
                    data.debuffDuration,
                    data.projectileSpeed,
                    this
                );
            }
            else
            {
                // 총알에 적 정보 및 스탯 전달
                projectile.Setup(
                    target,
                    (int)data.attackDamage,
                    data.isDebuffTower,
                    data.debuffType,
                    data.debuffValue,
                    data.debuffDuration,
                    data.projectileSpeed,
                    this
                );
            }
        }
    }

    // ───────────── 통계 ─────────────
    public void AddDamage(float amount)
    {
        if (amount > 0f) TotalDamage += amount;
    }

    public void AddKill()
    {
        KillCount++;
    }

    // ───────────── 랜섬웨어 감염 / 복호화 ─────────────
    public void Infect(int decryptCost)
    {
        if (!CanInfect) return;

        isLocked = true;
        DecryptCost = decryptCost;
        attackTimer = 0f;
        target = null;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.7f, 0.4f, 1f, 1f);   // 보라색 틴트

        CreateLockIcon();
    }

    // 복호화 비용(CPU)을 지불하면 잠금 해제. RAM은 감염 중에도 계속 점유되므로 건드리지 않는다.
    public bool TryDecrypt()
    {
        if (!isLocked) return false;

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.cpu < DecryptCost) return false;

        gm.ChangeCpu(-DecryptCost);
        RemoveLock();
        return true;
    }

    private void RemoveLock()
    {
        isLocked = false;
        attackTimer = 0f;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
        if (lockIcon != null) Destroy(lockIcon);
        lockIcon = null;
    }

    private void CreateLockIcon()
    {
        float topY = spriteRenderer != null ? spriteRenderer.bounds.max.y : transform.position.y + 0.5f;
        Vector3 pos = new Vector3(transform.position.x, topY + warningIconOffset, 0f);
        HackWarningIcon icon = HackWarningIcon.Create(transform, pos, "LockIcon", "LOCK", new Color(0.75f, 0.5f, 1f), 2.5f, false);
        lockIcon = icon.gameObject;
    }

    // ───────────── 해킹 ─────────────
    public void Hacking()
    {
        if (isHacked) return;

        if (isLocked) RemoveLock();   // 감염 상태에서 해킹되면 해킹 상태가 우선
        isHacked = true;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red; // 해킹 시 색상 변경
        gameObject.layer = LayerMask.NameToLayer("Enemy"); // 해킹된 타워 레이어 변경

        CreateWarningIcon();
    }

    private void CreateWarningIcon()
    {
        float topY = spriteRenderer != null ? spriteRenderer.bounds.max.y : transform.position.y + 0.5f;
        Vector3 pos = new Vector3(transform.position.x, topY + warningIconOffset, 0f);
        HackWarningIcon.Create(transform, pos);
    }

    // 해킹된 타워만 피해를 받는다. 공격으로 파괴되면 cost 전액 CPU 환급 + 사용 중이던 RAM 반환
    public void TakeDamage(int damage, Tower attacker = null)
    {
        if (!isHacked || damage <= 0 || released)
            return;

        int dealt = Mathf.Min(damage, Mathf.Max(currentHp, 0));
        currentHp -= damage;

        if (attacker != null) attacker.AddDamage(dealt);

        if (currentHp <= 0)
        {
            currentHp = 0;
            if (attacker != null) attacker.AddKill();
            Release(data != null ? data.useCpu : 0);
        }
    }

    // ───────────── 판매 ─────────────
    public int GetSellValue(float ratio)
    {
        return data != null ? Mathf.RoundToInt(data.useCpu * ratio) : 0;
    }

    public void Sell(float refundRatio)
    {
        if (!CanSell) return;
        Release(GetSellValue(refundRatio));
    }

    // CPU 환급 + RAM 반환 + 설치 셀 해제 + 오브젝트 제거 (중복 호출 방지)
    private void Release(int cpuRefund)
    {
        if (released) return;
        released = true;

        GameManager gm = GameManager.Instance;
        if (gm != null && data != null)
        {
            gm.ChangeCpu(cpuRefund);
            gm.SetRam(Mathf.Max(0f, gm.ram - data.useRam));
        }

        if (TowerPlacementManager.Instance != null)
            TowerPlacementManager.Instance.ReleaseCell(cell);

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (data == null) return;

        Gizmos.color = Color.blue;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(1f, 0.5f, 1f));
        Gizmos.DrawWireSphere(Vector3.zero, data.attackRange);
        Gizmos.matrix = oldMatrix;
    }
}