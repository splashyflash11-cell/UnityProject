using UnityEngine;

public class HoneyPot : MonoBehaviour
{
    public int maxHp = 500;
    [HideInInspector] public Vector2Int cell;
    [HideInInspector] public bool registered;   // 프리뷰 오브젝트가 Unregister 하지 않도록
    int hp;

    void Start() { hp = maxHp; }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        if (hp <= 0) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (registered && RandomPathGenerator.Instance != null)
            RandomPathGenerator.Instance.UnregisterHoneyPot(cell);
    }
}