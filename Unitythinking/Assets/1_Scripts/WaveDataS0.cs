using System;
using System.Collections.Generic;
using UnityEngine;

// 단일 적 스폰 정보
[Serializable]
public struct EnemySpawnInfo
{
    public string enemyName;        // 적 이름 (DDoS, Trojan 등)
    public GameObject enemyPrefab;  // 스폰할 적 프리팹
    public int spawnCount;          // 스폰할 총 마리 수
    public float spawnInterval;     // 적 스폰 간격 (초 단위)
}

// 1개 웨이브 세트 데이터
[Serializable]
public struct Wave
{
    public string waveName;                      // 예: "Wave 1 - DDoS 폭주"
    public List<EnemySpawnInfo> enemySpawnList;  // 이 웨이브에 생성할 적 목록
}

// 스크립터블 오브젝트 정의
[CreateAssetMenu(fileName = "NewWaveData", menuName = "Hacking Tower Defense/Wave Data")]
public class WaveDataS0 : ScriptableObject
{
    [Header("스테이지 전체 웨이브 목록")]
    public List<Wave> waves;
}