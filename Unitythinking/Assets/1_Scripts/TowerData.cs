using UnityEngine;

public enum DebuffType { None, Slow, DamageOverTime, ArmorReduce, RADAR }

[CreateAssetMenu(fileName = "NewTowerData", menuName = "Hacking Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("기본 스탯")]
    public string towerName;
    public int attackDamage;
    public float attackRange;
    public float attackInterval; // 공격 주기 (초)
    public int useCpu; // 타워 건설 비용
    public float useRam; // 타워 차지 비용

    public float projectileSpeed; // 총알 속도 (초당 단위)

    [Header("디버프 설정")]
    public bool isDebuffTower;
    public DebuffType debuffType;
    public float debuffValue;    // 예: 슬로우 비율(0.3 = 30% 감속)
    public float debuffDuration; // 디버프 지속 시간
}