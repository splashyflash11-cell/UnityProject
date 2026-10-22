using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerInfoPanel : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI infoText;   // 여러 줄 스탯
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider hpSlider;

    [Header("오른쪽 아래 여백")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);

    private Tower current;

    private void Awake()
    {
        // 오른쪽 아래 고정 (앵커/피벗 모두 우하단)
        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-margin.x, margin.y);
    }

    public void Show(Tower tower)
    {
        current = tower;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        current = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // 선택된 타워가 파괴되면 자동으로 닫기
        if (current == null)
        {
            Hide();
            return;
        }
        Refresh();   // 해킹 여부 / 체력 실시간 반영
    }

    private void Refresh()
    {
        TowerData d = current.data;
        bool hacked = current.isHacked;

        // 아이콘: 타워 스프라이트 재사용
        SpriteRenderer sr = current.GetComponent<SpriteRenderer>();
        if (iconImage != null && sr != null)
        {
            iconImage.sprite = sr.sprite;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        }

        if (nameText != null)
        {
            string n = d != null ? d.towerName : current.name;
            nameText.text = hacked ? $"{n} [HACKED]" : n;
            nameText.color = hacked ? Color.red : Color.white;
        }

        if (infoText != null)
        {
            if (d == null)
            {
                infoText.text = "NO DATA";
            }
            else
            {
                // Tower.cs의 해킹 페널티와 동일하게 표시
                int dmg = hacked ? (int)(d.attackDamage * 0.1f) : d.attackDamage;
                float interval = hacked ? d.attackInterval * 2f : d.attackInterval;

                string text =
                    $"AD : {dmg} | " +
                    $"AS : {interval:0.##}s\n" +
                    $"Range : {d.attackRange:0.##}\n" +
                    $"Cost : {d.useCpu}  | " +
                    $"Usage : {d.useRam:0.##}GB";

                if (d.isDebuffTower)
                    text += $"\nAbility  {DebuffLabel(d.debuffType)} {d.debuffValue:0.##} / {d.debuffDuration:0.##}s";
                else if (d.debuffType == DebuffType.RADAR)
                    text += "\nAbility  은신 탐지";

                infoText.text = text;
            }
        }

        if (hpSlider != null)
            hpSlider.value = current.maxHp > 0 ? (float)current.currentHp / current.maxHp : 0f;
        if (hpText != null)
            hpText.text = $"{current.currentHp} / {current.maxHp}";
    }

    private string DebuffLabel(DebuffType type)
    {
        switch (type)
        {
            case DebuffType.Slow: return "Slow";
            case DebuffType.DamageOverTime: return "DOT";
            case DebuffType.ArmorReduce: return "Scan";
            case DebuffType.RADAR: return "RADAR";
            default: return "None";
        }
    }
}