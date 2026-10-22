using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class DamageField : MonoBehaviour
{
    [Header("장판 옵션")]
    [SerializeField] float radius = 1.5f;        // 장판 반경 (스프라이트 크기와 맞출 것)
    [SerializeField] float tickInterval = 0.5f;  // 데미지 주기(초)
    [SerializeField] LayerMask enemyLayer;       // 적이 속한 레이어

    float tickDamage;
    float lifeTime;
    float tickTimer;
    private bool isLeased;
    private ObjectPool<DamageField> ownerPool;
    private readonly Collider2D[] hitBuffer = new Collider2D[32];
    private ContactFilter2D hitFilter;
    private static readonly Dictionary<DamageField, ObjectPool<DamageField>> Pools = new Dictionary<DamageField, ObjectPool<DamageField>>();

    private void Awake()
    {
        hitFilter = new ContactFilter2D();
        hitFilter.SetLayerMask(enemyLayer);
        hitFilter.useTriggers = true;
    }

    public static DamageField GetFromPool(DamageField prefab, Vector3 position)
    {
        if (prefab == null) return null;
        if (!Pools.TryGetValue(prefab, out ObjectPool<DamageField> pool))
        {
            ObjectPool<DamageField> createdPool = null;
            createdPool = new ObjectPool<DamageField>(
                () =>
                {
                    DamageField instance = Instantiate(prefab);
                    instance.ownerPool = createdPool;
                    return instance;
                },
                field =>
                {
                    field.isLeased = true;
                    field.gameObject.SetActive(true);
                },
                field =>
                {
                    field.ResetState();
                    field.isLeased = false;
                    field.gameObject.SetActive(false);
                },
                field => Destroy(field.gameObject),
                true, 2, 32);
            pool = createdPool;
            Pools.Add(prefab, pool);
        }

        DamageField fieldFromPool = pool.Get();
        if (fieldFromPool != null) fieldFromPool.transform.position = position;
        return fieldFromPool;
    }

    public void Setup(float damagePerTick, float duration)
    {
        tickDamage = damagePerTick;
        lifeTime = duration;
        tickTimer = 0f;
    }

    void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            ReturnToPool();
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickInterval > 0f && tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            DealTickDamage();
        }
    }

    void DealTickDamage()
    {
        // 장판 안에 있는 모든 적을 한 번에 검사
        int hitCount = Physics2D.OverlapCircle(transform.position, radius, hitFilter, hitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null) continue;
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null) enemy.TakeDamage(tickDamage);
        }
    }

    private void ReturnToPool()
    {
        if (!isLeased || ownerPool == null) return;
        ownerPool.Release(this);
    }

    private void ResetState()
    {
        tickDamage = 0f;
        lifeTime = 0f;
        tickTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}