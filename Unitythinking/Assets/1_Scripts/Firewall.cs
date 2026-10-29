using System.Collections.Generic;
using UnityEngine;

// 도로 위에 설치되어 닿은 적에게 직접 피해 + 슬로우 스택을 주는 접촉형 타워
[RequireComponent(typeof(Tower))]
public class Firewall : MonoBehaviour
{
    [Header("접촉 판정")]
    [SerializeField] private float detectRadius = 0.6f;   // 후보 탐색 반경 (셀 일치 여부로 최종 판정)

    [Header("정지 설정")]
    [SerializeField] private int stopStacks = 3;          // 이 스택에 도달하면 정지
    [SerializeField] private float stopDuration = 2f;     // 정지 시간(초)

    private Tower tower;
    private int enemyMask;
    private readonly Dictionary<Enemy, float> nextHitTime = new Dictionary<Enemy, float>();
    private readonly List<Enemy> stale = new List<Enemy>();
    private float cleanTimer;

    private void Awake()
    {
        tower = GetComponent<Tower>();
        enemyMask = LayerMask.GetMask("Enemy");
    }

    private void Update()
    {
        // 배치 미리보기(Tower 비활성) / 해킹 / 감염 상태에서는 작동 안 함
        if (!tower.enabled || tower.data == null || tower.isHacked || tower.isLocked) return;

        RandomPathGenerator path = RandomPathGenerator.Instance;
        if (path == null) return;

        TowerData d = tower.data;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius, enemyMask);

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            // 이 방화벽이 깔린 셀 위에 있는 적만 대상
            if (path.GetCellFromWorldPos(enemy.transform.position) != tower.cell) continue;

            // 적마다 쿨타임(attackInterval) 적용
            if (nextHitTime.TryGetValue(enemy, out float t) && Time.time < t) continue;
            nextHitTime[enemy] = Time.time + Mathf.Max(0.05f, d.attackInterval);

            enemy.ApplySlow(d.debuffValue, d.debuffDuration, stopStacks, stopDuration);
            enemy.TakeDamage(d.attackDamage, false, tower);
        }

        // 죽은 적 기록 정리 (1초마다)
        cleanTimer += Time.deltaTime;
        if (cleanTimer >= 1f)
        {
            cleanTimer = 0f;
            stale.Clear();
            foreach (var kv in nextHitTime)
                if (kv.Key == null) stale.Add(kv.Key);
            foreach (Enemy e in stale) nextHitTime.Remove(e);
        }
    }
}