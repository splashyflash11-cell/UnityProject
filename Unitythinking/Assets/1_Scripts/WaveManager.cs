using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    [Header("웨이브 데이터 (ScriptableObject)")]
    public WaveDataS0 waveData;

    [Header("웨이브 상태")]
    public int currentWaveIndex = 0;
    public bool isWaveRunning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        StartNextWave();
    }


    // 웨이브 시작
    public void StartNextWave()
    {
        if (isWaveRunning || waveData == null) return;

        // [수정된 부분] RandomPathGenerator의 isMainTowerPlaced 참조
        if (RandomPathGenerator.Instance == null || !RandomPathGenerator.Instance.isMainTowerPlaced)
        {
            Debug.LogWarning("메인 타워가 먼저 배치되어야 웨이브를 시작할 수 있습니다!");
            return;
        }

        if (currentWaveIndex < waveData.waves.Count)
        {
            StartCoroutine(SpawnWaveRoutine(waveData.waves[currentWaveIndex]));
        }
        else
        {
            Debug.Log("모든 웨이브가 종료되었습니다!");
        }
    }

    private IEnumerator SpawnWaveRoutine(Wave currentWave)
    {
        isWaveRunning = true;
        Debug.Log($"[{currentWave.waveName}] 시작!");

        List<Vector2Int> spawnCells = GetSpawnPositions();

        foreach (EnemySpawnInfo enemyInfo in currentWave.enemySpawnList)
        {
            // 1. 프리팹이 Null(빈칸)인 경우 생성을 건너뛰도록 처리
            if (enemyInfo.enemyPrefab == null)
            {
                Debug.LogError($"[{enemyInfo.enemyName}] 적 프리팹이 WaveDataSO에 등록되어 있지 않습니다!");
                continue;
            }

            for (int i = 0; i < enemyInfo.spawnCount; i++)
            {
                Vector2Int randomSpawnCell = spawnCells[Random.Range(0, spawnCells.Count)];
                Vector3 spawnWorldPos = GetWorldPosFromCell(randomSpawnCell);

                // 2. 명확하게 웨이브 데이터에 할당된 프리팹만 스폰
                GameObject enemyObj = Instantiate(enemyInfo.enemyPrefab, spawnWorldPos, Quaternion.identity);

                Enemy enemyMovement = enemyObj.GetComponent<Enemy>();

                List<List<Vector2Int>> paths = RandomPathGenerator.Instance.generatedPaths;
                List<Vector2Int> selectedPath = paths[Random.Range(0, paths.Count)];

                if (enemyMovement != null)
                {
                    enemyMovement.InitData();
                    enemyMovement.SetupPath(selectedPath);
                }


                yield return new WaitForSeconds(enemyInfo.spawnInterval);
            }
        }

        currentWaveIndex++;
        isWaveRunning = false;
    }

    // 맵의 4개 모서리 좌표 가져오기 (RandomPathGenerator 변수 활용)
    private List<Vector2Int> GetSpawnPositions()
    {
        int mapWidth = RandomPathGenerator.Instance.mapWidth;
        int mapHeight = RandomPathGenerator.Instance.mapHeight;

        int minX = -mapWidth / 2;
        int maxX = minX + mapWidth - 1;
        int minY = -mapHeight / 2;
        int maxY = minY + mapHeight - 1;

        return new List<Vector2Int>
        {
            new Vector2Int(minX, minY),
            new Vector2Int(minX, maxY),
            new Vector2Int(maxX, minY),
            new Vector2Int(maxX, maxY)
        };
    }

    // 2:1 아이소메트릭 좌표 변환 (RandomPathGenerator의 tileWidth 사용)
    private Vector3 GetWorldPosFromCell(Vector2Int cell)
    {
        float W = RandomPathGenerator.Instance.tileWidth; 
        float H = W * 0.5f;

        float snapX = (cell.x - cell.y) * (W * 0.5f);
        float snapY = (cell.x + cell.y + 1f) * (H * 0.5f);

        return new Vector3(snapX, snapY, 0f);
    }
}