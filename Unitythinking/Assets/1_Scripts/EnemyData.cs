using UnityEngine;

public enum EnemyType
{
    Normal,         // 일반 패킷
    DDoS,           // 고속 물량
    Trojan,         // 은신
    Ransomware,     // 타워 감염
    ZeroDay,        // 분석 전 무적
    MemoryLeakWorm, // RAM 용량 감소
    APTBoss         // 타워 해킹 보스
}

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Hacking Tower Defense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName;          // 적 이름
    public EnemyType enemyType;       // 적 유형
    public GameObject enemyPrefab;    // 적 프리팹

    [Header("능력치")]
    public float maxHealth = 100f;    // 체력
    public float moveSpeed = 2.0f;    // 이동 속도
    public int cpuReward = 0;        // 처치 시 획득 CPU 자원
    public int attackDamage = 0;      // 공격력

    [Header("특수 능력 설정")]
    public bool isStealth = false;    // 은신 여부 (Trojan)
    public bool isInvulnerable = false; // 분석 전 무적 여부 (ZeroDay)
    public float ramReduction = 0f;   // RAM 감소량 (Memory Leak Worm)
    public bool hacking = false; // 타워 해킹 여부 (APTBoss)
    public bool infecting = false; // 타워 감염 여부 (Ransomware)

    [Header("스프라이트")]
    public Sprite[] enemySprite = new Sprite[4];
}