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

    private float attackTimer;
    private Transform target;

    private SpriteRenderer spriteRenderer;

    public bool isHacked = false;

    private void Start()
    {
        currentHp = maxHp;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = (int)-transform.position.y;
    }
    private void Update()
    {
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
                    data.projectileSpeed
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
                    data.projectileSpeed
                );
            }
        }
    }

    public void Hacking()
    {
        isHacked = true;
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red; // 해킹 시 색상 변경
        gameObject.layer = LayerMask.NameToLayer("Enemy"); // 해킹된 타워 레이어 변경

    }

    public void TakeDamage(int damage)
    {
        if (!isHacked || damage <= 0)
            return;

        currentHp -= damage;
        if (currentHp <= 0)
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