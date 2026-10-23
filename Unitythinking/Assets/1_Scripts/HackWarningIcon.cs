using TMPro;
using UnityEngine;

// 해킹된 타워 머리 위에 뜨는 경고 아이콘.
// Assets/Resources/HackWarning (Sprite) 가 있으면 그 이미지를, 없으면 빨간 "!" 텍스트를 쓴다.
public class HackWarningIcon : MonoBehaviour
{
    private SpriteRenderer sr;
    private TextMeshPro label;
    private Vector3 baseLocalPos;

    public static HackWarningIcon Create(Transform parent, Vector3 worldPos)
    {
        GameObject go = new GameObject("HackWarning");
        go.transform.position = worldPos;
        go.transform.SetParent(parent, true);
        return go.AddComponent<HackWarningIcon>();
    }

    private void Awake()
    {
        baseLocalPos = transform.localPosition;

        Sprite icon = Resources.Load<Sprite>("HackWarning");
        if (icon != null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = icon;
            sr.sortingOrder = 6000;
        }
        else
        {
            label = gameObject.AddComponent<TextMeshPro>();
            label.text = "!";
            label.fontSize = 4f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.red;
            label.sortingOrder = 6000;
        }
    }

    private void Update()
    {
        float t = Time.time;
        transform.localPosition = baseLocalPos + Vector3.up * (Mathf.Sin(t * 6f) * 0.05f);

        float alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 5f));
        if (sr != null) sr.color = new Color(1f, 1f, 1f, alpha);
        if (label != null) label.color = new Color(1f, 0.15f, 0.15f, alpha);
    }
}