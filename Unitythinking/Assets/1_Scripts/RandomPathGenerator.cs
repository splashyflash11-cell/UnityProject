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
    private readonly int[] edgeBuffer = { 0, 1, 2, 3 };
    private readonly Vector2Int[] neighborBuffer = new Vector2Int[4];
    private readonly HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
    private Vector3Int[] tilePositionBuffer = new Vector3Int[0];
    private TileBase[] tileBuffer = new TileBase[0];
    private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public event System.Action MainTowerPlaced;

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
        MainTowerPlaced?.Invoke();
    }

    private List<Vector2Int> GetRandomEdgeSpawns()
    {
        int minX = -mapWidth / 2;
        int maxX = minX + mapWidth - 1;
        int minY = -mapHeight / 2;
        int maxY = minY + mapHeight - 1;

        for (int i = 0; i < edgeBuffer.Length; i++)
        {
            int temp = edgeBuffer[i];
            int randIndex = Random.Range(i, edgeBuffer.Length);
            edgeBuffer[i] = edgeBuffer[randIndex];
            edgeBuffer[randIndex] = temp;
        }

        int spawnCount = Random.Range(2, 4);
        List<Vector2Int> spawnPoints = new List<Vector2Int>();

        for (int i = 0; i < spawnCount; i++)
        {
            int edgeType = edgeBuffer[i];
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
        List<Vector2Int> singlePath = new List<Vector2Int>(mapWidth + mapHeight);
        Vector2Int current = start;

        singlePath.Add(current);
        pathCells.Add(current);
        visitedCells.Clear();
        visitedCells.Add(current);

        while (current != end)
        {
            int neighborCount = GetNeighbors(current, neighborBuffer);
            Vector2Int bestNext = current;
            float minCost = float.MaxValue;
            int currentDistance = Mathf.Abs(current.x - end.x) + Mathf.Abs(current.y - end.y);

            for (int i = 0; i < neighborCount; i++)
            {
                Vector2Int next = neighborBuffer[i];
                if (visitedCells.Contains(next)) continue;
                float distance = Mathf.Abs(next.x - end.x) + Mathf.Abs(next.y - end.y);
                if (distance >= currentDistance) continue;
                float randomWeight = Random.Range(0f, noiseFactor);
                float totalCost = distance + randomWeight;
                if (totalCost < minCost)
                {
                    minCost = totalCost;
                    bestNext = next;
                }
            }

            if (bestNext == current) break;
            current = bestNext;
            visitedCells.Add(current);
            singlePath.Add(current);
            pathCells.Add(current);
        }

        return singlePath;
    }

    public bool CanPlaceNormalTower(Vector2Int cellPos, bool isWall, HashSet<Vector2Int> towerPositions)
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

    private int GetNeighbors(Vector2Int cell, Vector2Int[] neighbors)
    {
        int count = 0;
        int minX = -mapWidth / 2;
        int maxX = minX + mapWidth - 1;
        int minY = -mapHeight / 2;
        int maxY = minY + mapHeight - 1;

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int check = cell + Directions[i];
            if (check.x >= minX && check.x <= maxX && check.y >= minY && check.y <= maxY)
                neighbors[count++] = check;
        }
        return count;
    }

    private void DrawPathOnTilemap()
    {
        if (pathTilemap == null || pathTile == null) return;

        if (tilePositionBuffer.Length != pathCells.Count)
        {
            tilePositionBuffer = new Vector3Int[pathCells.Count];
            tileBuffer = new TileBase[pathCells.Count];
        }
        int index = 0;
        foreach (Vector2Int cell in pathCells)
        {
            tilePositionBuffer[index] = (Vector3Int)cell;
            tileBuffer[index] = pathTile;
            index++;
        }
        pathTilemap.SetTiles(tilePositionBuffer, tileBuffer);
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