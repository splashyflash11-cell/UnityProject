using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

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
    private SpriteRenderer spriteRenderer;
    private bool suppressDebuffField;
    private bool isLeased;
    private ObjectPool<Projectile> ownerPool;
    private static readonly Dictionary<GameObject, ObjectPool<Projectile>> Pools = new Dictionary<GameObject, ObjectPool<Projectile>>();

    [Header("장판 (디버프 타워용)")]
    [SerializeField] DamageField fieldPrefab;   // 인스펙터에서 장판 프리팹 연결
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

    }

    // 타워가 총알을 발사할 때 초기화하는 함수
    public static Projectile GetFromPool(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;
        if (!Pools.TryGetValue(prefab, out ObjectPool<Projectile> pool))
        {
            ObjectPool<Projectile> createdPool = null;
            createdPool = new ObjectPool<Projectile>(
                () =>
                {
                    GameObject instance = Instantiate(prefab);
                    Projectile projectile = instance.GetComponent<Projectile>();
                    if (projectile != null) projectile.ownerPool = createdPool;
                    else Destroy(instance);
                    return projectile;
                },
                projectile =>
                {
                    projectile.isLeased = true;
                    projectile.gameObject.SetActive(true);
                },
                projectile =>
                {
                    projectile.ResetState();
                    projectile.isLeased = false;
                    projectile.gameObject.SetActive(false);
                },
                projectile => Destroy(projectile.gameObject),
                true, 4, 64);
            pool = createdPool;
            Pools.Add(prefab, pool);
        }

        Projectile instanceFromPool = pool.Get();
        if (instanceFromPool != null) instanceFromPool.transform.position = position;
        return instanceFromPool;
    }

    public void Setup(Transform targetTransform, int dmg, bool isDebuff, DebuffType type, float val, float duration, float projectileSpeed, bool suppressField = false)
    {
        target = targetTransform;
        damage = dmg;
        isDebuffTower = isDebuff;
        debuffType = type;
        debuffValue = val;
        debuffDuration = duration;
        speed = projectileSpeed;
        suppressDebuffField = suppressField;
    }

    private void Update()
    {
        // 타겟 몬스터가 사라지거나 죽었을 경우 총알 파괴
        if (target == null)
        {
            ReturnToPool();
            return;
        }

        // 타겟 방향으로 이동
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        
        if (spriteRenderer != null) spriteRenderer.sortingOrder = (int)-transform.position.y;

        // 회전 (발사체가 적을 바라보도록 설정)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);


        // 적과 거의 가까워지면 적중 처리
        if ((transform.position - target.position).sqrMagnitude <= 0.01f)
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
                tower.TakeDamage(damage);
                ReturnToPool();
                return;
            }

            if (tower != null && (suppressDebuffField || !isDebuffTower))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TakeDamage(damage);

                ReturnToPool();
                return;
            }
        }

        if (isDebuffTower && !suppressDebuffField && fieldPrefab != null)
        {
            DamageField field = DamageField.GetFromPool(fieldPrefab, transform.position);
            if (field != null) field.Setup(damage, debuffDuration);
        }
        else if (enemy != null)
        {
            enemy.TakeDamage(damage, debuffType == DebuffType.ArmorReduce);
        }


        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!isLeased || ownerPool == null) return;
        ownerPool.Release(this);
    }

    private void ResetState()
    {
        target = null;
        damage = 0;
        speed = 0f;
        isDebuffTower = false;
        debuffType = DebuffType.None;
        debuffValue = 0f;
        debuffDuration = 0f;
        suppressDebuffField = false;
    }
}