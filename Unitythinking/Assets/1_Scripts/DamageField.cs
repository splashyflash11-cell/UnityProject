using UnityEngine;

public class DamageField : MonoBehaviour
{
    [Header("장판 옵션")]
    [SerializeField] float radius = 1.5f;        // 장판 반경 (스프라이트 크기와 맞출 것)
    [SerializeField] float tickInterval = 0.5f;  // 데미지 주기(초)
    [SerializeField] LayerMask enemyLayer;       // 적이 속한 레이어

    float tickDamage;
    float lifeTime;
    float tickTimer;

    public void Setup(float damagePerTick, float duration)
    {
        tickDamage = damagePerTick;
        lifeTime = duration;
    }

    void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            DealTickDamage();
        }
    }

    void DealTickDamage()
    {
        // 장판 안에 있는 모든 적을 한 번에 검사
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(tickDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}