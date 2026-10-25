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
    public event System.Action MainTowerPlaced;
    public HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();

    // 개별 경로(시작점 -> 본진)들의 목록 관리
    public List<List<Vector2Int>> generatedPaths = new List<List<Vector2Int>>();

    // 설치된 허니팟 목록 (셀 -> 허니팟)
    public Dictionary<Vector2Int, HoneyPot> honeyPots = new Dictionary<Vector2Int, HoneyPot>();

    static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

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
        honeyPots.Clear();
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

        // 경로 생성이 완료되면 적 스폰 시작
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.StartSpawning();
        }
        MainTowerPlaced?.Invoke();
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

    public bool CanPlaceNormalTower(Vector2Int cellPos, bool isWall = false, HashSet<Vector2Int> towerPositions = null)
    {
        if (!isMainTowerPlaced) return false;
        if (cellPos == mainTowerCell) return false;
        if (towerPositions != null && towerPositions.Contains(cellPos)) return false;

        bool inPath = pathCells.Contains(cellPos);
        if (inPath && isWall) return true;
        if (!inPath && isWall) return false;
        if (inPath) return false;

        return InBounds(cellPos);
    }

    private List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        foreach (Vector2Int d in Dirs)
        {
            Vector2Int check = cell + d;
            if (InBounds(check))
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

    // ───────────────────────── 허니팟 가지길 ─────────────────────────

    public void RegisterHoneyPot(Vector2Int cell, HoneyPot pot)
    {
        pot.cell = cell;
        pot.registered = true;
        honeyPots[cell] = pot;
        if (!BuildBranchToNearestRoad(cell))
            Debug.LogWarning("허니팟에서 도로로 이어지는 빈 칸 경로가 없습니다. 유인이 작동하지 않습니다.");
    }

    public void UnregisterHoneyPot(Vector2Int cell)
    {
        honeyPots.Remove(cell);
    }

    bool InBounds(Vector2Int c)
    {
        int minX = -mapWidth / 2, maxX = minX + mapWidth - 1;
        int minY = -mapHeight / 2, maxY = minY + mapHeight - 1;
        return c.x >= minX && c.x <= maxX && c.y >= minY && c.y <= maxY;
    }

    // 가지길이 지나갈 수 있는 칸: 맵 안 + 도로 아님 + 본진 아님 + 타워 없음
    bool IsFreeForBranch(Vector2Int c)
    {
        if (!InBounds(c) || pathCells.Contains(c) || c == mainTowerCell) return false;
        var tpm = TowerPlacementManager.Instance;
        return tpm == null || !tpm.IsCellOccupied(c);
    }

    bool BuildBranchToNearestRoad(Vector2Int honey)
    {
        if (pathCells.Count == 0) return false;

        List<Vector2Int> branch = FindStraightBranch(honey) ?? FindBfsBranch(honey);
        if (branch == null) return false;

        foreach (Vector2Int c in branch) pathCells.Add(c);   // 도로 끝점은 이미 pathCells에 있음
        DrawPathOnTilemap();
        return true;
    }

    // 1순위: 상하좌우 한 방향으로 쭉 가서 도로에 닿는 가장 짧은 직선
    List<Vector2Int> FindStraightBranch(Vector2Int honey)
    {
        List<Vector2Int> best = null;
        foreach (Vector2Int d in Dirs)
        {
            var cells = new List<Vector2Int>();
            Vector2Int cur = honey + d;
            while (InBounds(cur))
            {
                if (pathCells.Contains(cur))                       // 도로 도달
                {
                    if (best == null || cells.Count < best.Count) best = cells;
                    break;
                }
                if (!IsFreeForBranch(cur)) break;                  // 타워/본진에 막힘
                cells.Add(cur);
                cur += d;
            }
        }
        return best;
    }

    // 2순위: 빈 칸만 통과하는 BFS (꺾이더라도 가장 가까운 도로까지)
    List<Vector2Int> FindBfsBranch(Vector2Int honey)
    {
        var prev = new Dictionary<Vector2Int, Vector2Int> { [honey] = honey };
        var q = new Queue<Vector2Int>();
        q.Enqueue(honey);

        while (q.Count > 0)
        {
            Vector2Int c = q.Dequeue();
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = c + d;
                if (prev.ContainsKey(n) || !InBounds(n)) continue;

                if (pathCells.Contains(n))                          // 도로 발견 -> 역추적
                {
                    var cells = new List<Vector2Int>();
                    for (Vector2Int p = c; p != honey; p = prev[p]) cells.Add(p);
                    cells.Reverse();
                    return cells;
                }
                if (!IsFreeForBranch(n)) continue;
                prev[n] = c;
                q.Enqueue(n);
            }
        }
        return null;   // 막혀서 연결 불가
    }

    // 도로 그래프 위 BFS (goal이 허니팟 칸이면 pathCells 밖이어도 허용)
    public List<Vector2Int> FindRoute(Vector2Int start, Vector2Int goal)
    {
        var prev = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
        var q = new Queue<Vector2Int>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            Vector2Int c = q.Dequeue();
            if (c == goal) break;
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = c + d;
                if (prev.ContainsKey(n)) continue;
                if (!pathCells.Contains(n) && n != goal) continue;
                prev[n] = c;
                q.Enqueue(n);
            }
        }
        if (!prev.ContainsKey(goal)) return null;

        var route = new List<Vector2Int>();
        for (Vector2Int c = goal; ; c = prev[c]) { route.Add(c); if (c == start) break; }
        route.Reverse();
        return route;
    }

    // ───────────────────────── 좌표 변환 ─────────────────────────

    // 셀 인덱스를 월드 좌표로 변환 (TowerPlacementManager와 동일한 방식)
    public Vector3 GetWorldPosFromCell(Vector2Int cell)
    {
        float W = tileWidth;
        float H = tileWidth * 0.5f;

        float snapX = (cell.x - cell.y) * (W * 0.5f);
        float snapY = (cell.x + cell.y + 1f) * (H * 0.5f);

        return new Vector3(snapX, snapY, 0f);
    }

    // 월드 -> 셀 (GetWorldPosFromCell의 역변환)
    public Vector2Int GetCellFromWorldPos(Vector3 pos)
    {
        float W = tileWidth, H = W * 0.5f;
        float a = pos.x / (W * 0.5f);           // x - y
        float b = pos.y / (H * 0.5f) - 1f;      // x + y
        return new Vector2Int(Mathf.RoundToInt((a + b) * 0.5f), Mathf.RoundToInt((b - a) * 0.5f));
    }
}