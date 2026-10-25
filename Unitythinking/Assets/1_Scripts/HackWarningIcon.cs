using TMPro;
using UnityEngine;

// 타워 머리 위에 뜨는 상태 아이콘 (해킹 경고 / 랜섬웨어 잠금 공용).
// Assets/Resources/<spriteResource> (Sprite) 가 있으면 그 이미지를, 없으면 색깔 있는 글자를 쓴다.
public class HackWarningIcon : MonoBehaviour
{
    private string spriteResource = "HackWarning";
    private string fallbackText = "!";
    private Color fallbackColor = Color.red;
    private float fallbackSize = 4f;
    private bool blink = true;

    private SpriteRenderer sr;
    private TextMeshPro label;
    private Vector3 baseLocalPos;

    public static HackWarningIcon Create(
        Transform parent, Vector3 worldPos,
        string spriteResource = "HackWarning",
        string fallbackText = "!",
        Color? fallbackColor = null,
        float fallbackSize = 4f,
        bool blink = true)
    {
        GameObject go = new GameObject(spriteResource);
        go.SetActive(false);                       // 값 설정 후 활성화해서 Awake 가 최종 값을 쓰도록
        go.transform.position = worldPos;
        go.transform.SetParent(parent, true);

        HackWarningIcon icon = go.AddComponent<HackWarningIcon>();
        icon.spriteResource = spriteResource;
        icon.fallbackText = fallbackText;
        icon.fallbackColor = fallbackColor ?? Color.red;
        icon.fallbackSize = fallbackSize;
        icon.blink = blink;

        go.SetActive(true);
        return icon;
    }

    private void Awake()
    {
        baseLocalPos = transform.localPosition;

        Sprite icon = Resources.Load<Sprite>(spriteResource);
        if (icon != null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = icon;
            sr.sortingOrder = 6000;
        }
        else
        {
            label = gameObject.AddComponent<TextMeshPro>();
            label.text = fallbackText;
            label.fontSize = fallbackSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = fallbackColor;
            label.sortingOrder = 6000;
        }
    }

    private void Update()
    {
        float t = Time.time;
        transform.localPosition = baseLocalPos + Vector3.up * (Mathf.Sin(t * 6f) * 0.05f);

        float alpha = blink ? 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 5f)) : 1f;
        if (sr != null) sr.color = new Color(1f, 1f, 1f, alpha);
        if (label != null) label.color = new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, alpha);
    }
}