using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomPathGenerator : MonoBehaviour
{
    public static RandomPathGenerator Instance;

    [Header("맵 및 타일 설정")]
    public float tileWidth = 1.0f;
    public int mapWidth = 14;
    public int mapHeight = 14;
    public float noiseFactor = 2.5f;

    [Header("길 시각화 (선택)")]
    public Tilemap pathTilemap;
    public TileBase pathTile;

    public Vector2Int mainTowerCell;
    public bool isMainTowerPlaced = false;
    public HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();
    

    // [추가] 개별 경로(시작점 -> 본진)들의 목록 관리
    public List<List<Vector2Int>> generatedPaths = new List<List<Vector2Int>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnPlaceMainTower(Vector2Int towerCell)
    {
        mainTowerCell = towerCell;
        isMainTowerPlaced = true;

        pathCells.Clear();
        generatedPaths.Clear();
        if (pathTilemap != null) pathTilemap.ClearAllTiles();

        List<Vector2Int> spawnPoints = GetRandomEdgeSpawns();

        foreach (Vector2Int spawn in spawnPoints)
        {
            List<Vector2Int> path = GenerateRandomPath(spawn, mainTowerCell);
            if (path != null && path.Count > 0)
            {
                generatedPaths.Add(path);
            }
        }

        DrawPathOnTilemap();

        // [추가] 경로 생성이 완료되면 적 스폰 시작
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.StartSpawning();
        }
    }

    private List<Vector2Int> GetRandomEdgeSpawns()
    {
        int minX = -mapWidth / 2;
        int maxX = minX + mapWidth - 1;
        int minY = -mapHeight / 2;
        int maxY = minY + mapHeight - 1;

        List<int> edges = new List<int> { 0, 1, 2, 3 };

        for (int i = 0; i < edges.Count; i++)
        {
            int temp = edges[i];
            int randIndex = Random.Range(i, edges.Count);
            edges[i] = edges[randIndex];
            edges[randIndex] = temp;
        }

        int spawnCount = Random.Range(2, 4);
        List<Vector2Int> spawnPoints = new List<Vector2Int>();

        for (int i = 0; i < spawnCount; i++)
        {
            int edgeType = edges[i];
            Vector2Int spawnPos = Vector2Int.zero;

            switch (edgeType)
            {
                case 0: spawnPos = new Vector2Int(minX, Random.Range(minY, maxY + 1)); break;
                case 1: spawnPos = new Vector2Int(maxX, Random.Range(minY, maxY + 1)); break;
                case 2: spawnPos = new Vector2Int(Random.Range(minX, maxX + 1), minY); break;
                case 3: spawnPos = new Vector2Int(Random.Range(minX, maxX + 1), maxY); break;
            }
            spawnPoints.Add(spawnPos);
        }

        return spawnPoints;
    }

    private List<Vector2Int> GenerateRandomPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> singlePath = new List<Vector2Int>();
        Vector2Int current = start;

        singlePath.Add(current);
        pathCells.Add(current);

        int maxStep = mapWidth * mapHeight;
        int step = 0;

        while (current != end && step < maxStep)
        {
            step++;
            List<Vector2Int> neighbors = GetNeighbors(current);
            Vector2Int bestNext = current;
            float minCost = float.MaxValue;

            foreach (Vector2Int next in neighbors)
            {
                if (singlePath.Contains(next) && next != end) continue;

                float distance = Mathf.Abs(next.x - end.x) + Mathf.Abs(next.y - end.y);
                float randomWeight = Random.Range(0f, noiseFactor);
                float totalCost = distance + randomWeight;

                if (totalCost < minCost)
                {
                    minCost = totalCost;
                    bestNext = next;
                }
            }

            if (bestNext == current)
            {
                float minDistance = float.MaxValue;
                foreach (Vector2Int next in neighbors)
                {
                    float distance = Mathf.Abs(next.x - end.x) + Mathf.Abs(next.y - end.y);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestNext = next;
                    }
                }
            }

            current = bestNext;
            singlePath.Add(current);
            pathCells.Add(current);

            if (current == end) break;
        }

        return singlePath;
    }

    public bool CanPlaceNormalTower(Vector2Int cellPos, bool isWall = false, List<Vector2> towerPositions = null)
    {
        if (!isMainTowerPlaced) return false;
        if (cellPos == mainTowerCell) return false;
        if (towerPositions != null && towerPositions.Contains(cellPos)) return false;

        bool inPath = pathCells.Contains(cellPos);
        if (inPath && isWall) return true;
        if (!inPath && isWall) return false;
        if (inPath) return false;

        int minX = -mapWidth / 2;
        int maxX = minX + mapWidth - 1;
        int minY = -mapHeight / 2;
        int maxY = minY + mapHeight - 1;

        return cellPos.x >= minX && cellPos.x <= maxX && cellPos.y >= minY && cellPos.y <= maxY;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        int minX = -mapWidth / 2;
        int maxX = minX + mapWidth - 1;
        int minY = -mapHeight / 2;
        int maxY = minY + mapHeight - 1;

        foreach (Vector2Int d in dirs)
        {
            Vector2Int check = cell + d;
            if (check.x >= minX && check.x <= maxX && check.y >= minY && check.y <= maxY)
            {
                neighbors.Add(check);
            }
        }
        return neighbors;
    }

    private void DrawPathOnTilemap()
    {
        if (pathTilemap == null || pathTile == null) return;

        foreach (Vector2Int cell in pathCells)
        {
            pathTilemap.SetTile((Vector3Int)cell, pathTile);
        }
    }

    // [추가] 셀 인덱스를 월드 좌표로 변환 (TowerPlacementManager 방식 동일 사용)[cite: 2]
    public Vector3 GetWorldPosFromCell(Vector2Int cell)
    {
        float W = tileWidth;
        float H = tileWidth * 0.5f;

        float snapX = (cell.x - cell.y) * (W * 0.5f);
        float snapY = (cell.x + cell.y + 1f) * (H * 0.5f);

        return new Vector3(snapX, snapY, 0f);
    }
}