using UnityEngine;

// 대상 스프라이트 뒤에 8방향으로 살짝 어긋난 복제 스프라이트를 깔아 윤곽선처럼 보이게 한다.
// (피벗 위치와 상관없이 동작한다)
public class SelectionOutline : MonoBehaviour
{
    private static readonly Vector2[] Dirs =
    {
        new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f)
    };

    private SpriteRenderer[] copies;
    private SpriteRenderer target;
    private Color color = Color.white;
    private float thickness = 0.04f;

    public static SelectionOutline Create(float thickness, Material material)
    {
        GameObject go = new GameObject("SelectionOutline");
        SelectionOutline outline = go.AddComponent<SelectionOutline>();
        outline.Build(thickness, material);
        go.SetActive(false);
        return outline;
    }

    private void Build(float outlineThickness, Material material)
    {
        thickness = outlineThickness;
        copies = new SpriteRenderer[Dirs.Length];
        for (int i = 0; i < Dirs.Length; i++)
        {
            GameObject child = new GameObject("Outline_" + i);
            child.transform.SetParent(transform, false);
            copies[i] = child.AddComponent<SpriteRenderer>();
            if (material != null) copies[i].sharedMaterial = material;
        }
    }

    public void Follow(SpriteRenderer sr, Color outlineColor)
    {
        if (sr == null)
        {
            Clear();
            return;
        }

        target = sr;
        color = outlineColor;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Sync();
    }

    public void Clear()
    {
        target = null;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Clear();
            return;
        }
        Sync();
    }

    private void Sync()
    {
        Transform t = target.transform;
        transform.position = t.position;
        transform.rotation = t.rotation;
        transform.localScale = t.lossyScale;

        for (int i = 0; i < copies.Length; i++)
        {
            SpriteRenderer c = copies[i];
            c.sprite = target.sprite;
            c.flipX = target.flipX;
            c.flipY = target.flipY;
            c.color = color;
            c.sortingLayerID = target.sortingLayerID;
            c.sortingOrder = target.sortingOrder - 1;
            c.transform.localPosition = new Vector3(Dirs[i].x * thickness, Dirs[i].y * thickness, 0f);
        }
    }
}