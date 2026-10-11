using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("적 데이터 에셋 목록")]
    public GameObject[] enemyDataList; // Project 창에서 생성한 EnemyData 에셋들을 등록
    public float spawnInterval = 2.0f;

    private bool isSpawning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (RandomPathGenerator.Instance != null && RandomPathGenerator.Instance.generatedPaths.Count > 0 && enemyDataList.Length > 0)
            {
                // 1. 길 무작위 선택
                List<List<Vector2Int>> paths = RandomPathGenerator.Instance.generatedPaths;
                List<Vector2Int> selectedPath = paths[Random.Range(0, paths.Count)];

                // 2. 적 데이터 무작위 선택
                GameObject selectedEnemy = enemyDataList[Random.Range(0, enemyDataList.Length)];

                // 3. 적 생성 및 설정
                GameObject enemyObj = Instantiate(selectedEnemy);
                Enemy enemyMovement = enemyObj.GetComponent<Enemy>();

                if (enemyMovement != null)
                {
                    enemyMovement.InitData();
                    enemyMovement.SetupPath(selectedPath);
                }
            }
        }
    }
}