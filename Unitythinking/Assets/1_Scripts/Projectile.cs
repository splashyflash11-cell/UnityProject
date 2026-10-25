using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("발사체 옵션")]
    float speed; // 총알 이동 속도

    private Transform target;
    public int damage;
    private bool isDebuffTower;
    private DebuffType debuffType;
    private float debuffValue;
    private float debuffDuration;
    private Tower owner;   // 발사한 타워 (통계 집계용)
    SpriteRenderer spriteRenderer;
    [HideInInspector] public HoneyPot lureSource;

    [Header("장판 (디버프 타워용)")]
    [SerializeField] DamageField fieldPrefab;   // 인스펙터에서 장판 프리팹 연결
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

    }

    // 타워가 총알을 발사할 때 초기화하는 함수
    public void Setup(Transform targetTransform, int dmg, bool isDebuff, DebuffType type, float val, float duration, float projectileSpeed, Tower ownerTower = null)
    {
        target = targetTransform;
        damage = dmg;
        isDebuffTower = isDebuff;
        debuffType = type;
        debuffValue = val;
        debuffDuration = duration;
        speed = projectileSpeed;
        owner = ownerTower;
    }

    private void Update()
    {
        // 타겟 몬스터가 사라지거나 죽었을 경우 총알 파괴
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 타겟 방향으로 이동
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        spriteRenderer.sortingOrder = (int)-transform.position.y;

        // 회전 (발사체가 적을 바라보도록 설정)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);


        // 적과 거의 가까워지면 적중 처리
        if (Vector3.Distance(transform.position, target.position) <= 0.1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        Enemy enemy = target.GetComponentInParent<Enemy>();
        if (enemy == null)
        {
            Tower tower = target.GetComponentInParent<Tower>();
            if (tower != null && tower.isHacked)
            {
                tower.TakeDamage(damage, owner);
                Destroy(gameObject);
                return;
            }

            if (tower != null && !isDebuffTower)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TakeDamage(damage);

                Destroy(gameObject);
                return;
            }
        }

        if (enemy != null && lureSource != null) enemy.Lure(lureSource);

        if (isDebuffTower && fieldPrefab != null)
        {
            // 닿은 위치에 장판 생성
            DamageField field = Instantiate(fieldPrefab, transform.position, Quaternion.identity);
            field.Setup(damage, debuffDuration, owner);   // damage = 틱당 데미지, debuffDuration = 장판 지속시간
        }
        else if (enemy != null)
        {
            enemy.TakeDamage(damage, debuffType == DebuffType.ArmorReduce, owner);
        }


        Destroy(gameObject);
    }
}