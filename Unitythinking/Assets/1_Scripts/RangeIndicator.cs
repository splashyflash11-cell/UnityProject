using UnityEngine;

// 2:1 아이소메트릭 비율(가로 = range, 세로 = range * 0.5)의 사거리 타원을 그린다.
// Tower.FindTarget 의 판정식(y * 2)과 같은 모양이다.
public class RangeIndicator : MonoBehaviour
{
    private const int Segments = 72;

    private LineRenderer line;
    private float shownRange = -1f;

    public static RangeIndicator Create(string objectName)
    {
        GameObject go = new GameObject(objectName);
        RangeIndicator indicator = go.AddComponent<RangeIndicator>();
        go.SetActive(false);
        return indicator;
    }

    private void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = Segments;
        line.widthMultiplier = 0.04f;
        line.sortingOrder = 5000;
        line.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void Show(Vector3 center, float range, Color color)
    {
        if (range <= 0f)
        {
            Hide();
            return;
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        transform.position = new Vector3(center.x, center.y, 0f);
        if (!Mathf.Approximately(shownRange, range)) Rebuild(range);

        line.startColor = color;
        line.endColor = color;
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void Rebuild(float range)
    {
        for (int i = 0; i < Segments; i++)
        {
            float a = (float)i / Segments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * range, Mathf.Sin(a) * range * 0.5f, 0f));
        }
        shownRange = range;
    }
}