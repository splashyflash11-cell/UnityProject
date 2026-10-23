using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 타워/적 정보 패널 (오른쪽 아래 고정)
public class TowerInfoPanel : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI infoText;   // 여러 줄 스탯
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider hpSlider;

    [Header("추가 UI (비워 두면 건너뜀)")]
    [SerializeField] private TextMeshProUGUI hintText;       // "클릭하여 고정" 등 안내
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;
    [SerializeField] private Outline borderOutline;          // 해킹 시 붉게 깜빡일 테두리

    [Header("오른쪽 아래 여백")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);

    private Tower tower;
    private Enemy enemy;
    private bool pinned;

    private CanvasGroup canvasGroup;
    private Image background;
    private Color backgroundBase = Color.white;

    private void Awake()
    {
        // 오른쪽 아래 고정 (앵커/피벗 모두 우하단)
        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-margin.x, margin.y);

        // 호버(미리보기) 상태에서는 마우스를 가리지 않도록 CanvasGroup 사용
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        background = GetComponent<Image>();
        if (background != null) backgroundBase = background.color;

        if (borderOutline != null) borderOutline.enabled = false;
        if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);
    }

    public void ShowTower(Tower t, bool isPinned)
    {
        tower = t;
        enemy = null;
        pinned = isPinned;
        Open();
    }

    public void ShowEnemy(Enemy e, bool isPinned)
    {
        enemy = e;
        tower = null;
        pinned = isPinned;
        Open();
    }

    public void Hide()
    {
        tower = null;
        enemy = null;
        UpdateBorder(false);
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void Open()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Refresh();
    }

    private void Update()
    {
        // 대상이 파괴되면 자동으로 닫기
        if (tower == null && enemy == null)
        {
            Hide();
            return;
        }
        Refresh();
    }

    private void Refresh()
    {
        if (tower != null) RefreshTower();
        else if (enemy != null) RefreshEnemy();

        if (canvasGroup != null) canvasGroup.blocksRaycasts = pinned;
    }

    // ───────────── 타워 ─────────────
    private void RefreshTower()
    {
        TowerData d = tower.data;
        bool hacked = tower.isHacked;

        SetIcon(tower.GetComponent<SpriteRenderer>());

        string n = d != null ? d.towerName : tower.name;
        SetText(nameText, hacked ? $"{n} [Hacked]" : n);
        if (nameText != null) nameText.color = hacked ? Color.red : Color.white;

        if (d == null)
        {
            SetText(infoText, "No Data");
        }
        else
        {
            // Tower.cs 의 해킹 페널티와 동일하게 표시
            int dmg = hacked ? (int)(d.attackDamage * 0.1f) : d.attackDamage;
            float interval = hacked ? d.attackInterval * 2f : d.attackInterval;

            string text =
                $"AD : {dmg} | " +
                $"AS : {interval:0.##}s\n" +
                $"Range : {d.attackRange:0.##}\n" +
                $"Cost : {d.useCpu} | " +
                $"RAM : {d.useRam:0.##}GB";

            if (d.isDebuffTower)
                text += $"\nAbility  {DebuffLabel(d.debuffType)} {d.debuffValue:0.##} / {d.debuffDuration:0.##}s";
            else if (d.debuffType == DebuffType.RADAR)
                text += "\nAbility  Stealth Detection";

            int sec = (int)tower.AliveTime;
            text += $"\nKill  {tower.KillCount} | Dmg  {tower.TotalDamage:0} | Time  {sec / 60:00}:{sec % 60:00}";

            SetText(infoText, text);
        }

        if (hpSlider != null)
            hpSlider.value = tower.maxHp > 0 ? (float)tower.currentHp / tower.maxHp : 0f;
        SetText(hpText, $"{tower.currentHp} / {tower.maxHp}");

        // 안내 문구
        string hint = "";
        if (!pinned) hint = "Click to pin";
        else if (hacked && d != null)
            hint = $"Hacked · Destroy to  CPU +{d.useCpu}, RAM -{d.useRam:0.##}GB";
        SetText(hintText, hint);

        // 판매 버튼 (고정 상태 + 해킹되지 않은 일반 타워만)
        bool canSell = pinned && tower.CanSell;
        if (sellButton != null)
        {
            if (sellButton.gameObject.activeSelf != canSell) sellButton.gameObject.SetActive(canSell);
            if (canSell && d != null)
            {
                float ratio = TowerSelectionManager.Instance != null ? TowerSelectionManager.Instance.SellRefundRatio : 0.5f;
                SetText(sellButtonText, $"CPU +{tower.GetSellValue(ratio)} | RAM -{d.useRam:0.##}GB");
            }
        }

        UpdateBorder(hacked);
    }

    // ───────────── 적 도감 ─────────────
    private void RefreshEnemy()
    {
        EnemyData d = enemy.data;

        SetIcon(enemy.GetComponent<SpriteRenderer>());

        if (d == null)
        {
            SetText(nameText, enemy.name);
            SetText(infoText, "NoData");
            SetText(hpText, "");
            if (hpSlider != null) hpSlider.value = 0f;
            SetText(hintText, "");
            if (sellButton != null && sellButton.gameObject.activeSelf) sellButton.gameObject.SetActive(false);
            UpdateBorder(false);
            return;
        }

        SetText(nameText, $"{d.enemyName} [{EnemyTypeLabel(d.enemyType)}]");
        if (nameText != null) nameText.color = new Color(1f, 0.6f, 0.2f);

        string text =
            $"Speed : {enemy.MoveSpeed:0.##}\n" +
            $"Reward : +{d.cpuReward}\n" +
            $"Damage  {d.attackDamage}";

        if (d.isStealth) text += "\n\nStealth · Only Radar Towers can attack";
        if (d.isInvulnerable)
            text += enemy.IsInvulnerable ? "\n\nInvulnerable · Reduced by Armor Reduction attacks" : "\n\nInvulnerable removed";
        if (d.hacking) text += "\n\nHacking · Periodically hacks towers";
        if (d.ramReduction > 0f) text += $"\n\nRAM Reduction {d.ramReduction:0.##}GB";

        SetText(infoText, text);

        float max = d.maxHealth;
        if (hpSlider != null) hpSlider.value = max > 0f ? enemy.CurrentHealth / max : 0f;
        SetText(hpText, $"{Mathf.CeilToInt(enemy.CurrentHealth)} / {Mathf.CeilToInt(max)}");

        SetText(hintText, pinned ? "" : "Click to pin");

        if (sellButton != null && sellButton.gameObject.activeSelf) sellButton.gameObject.SetActive(false);
        UpdateBorder(false);
    }

    // ───────────── 공통 ─────────────
    private void OnSellClicked()
    {
        if (TowerSelectionManager.Instance != null)
            TowerSelectionManager.Instance.SellSelected();
    }

    // 해킹 상태면 테두리(없으면 배경색)를 붉게 깜빡인다
    private void UpdateBorder(bool alert)
    {
        float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);

        if (borderOutline != null)
        {
            borderOutline.enabled = alert;
            if (alert)
                borderOutline.effectColor = Color.Lerp(new Color(1f, 0f, 0f, 0.2f), new Color(1f, 0.1f, 0.1f, 1f), blink);
        }
        else if (background != null)
        {
            Color alertColor = new Color(0.8f, 0.05f, 0.05f, backgroundBase.a);
            background.color = alert ? Color.Lerp(backgroundBase, alertColor, blink) : backgroundBase;
        }
    }

    private void SetIcon(SpriteRenderer sr)
    {
        if (iconImage == null || sr == null) return;
        iconImage.sprite = sr.sprite;
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;
    }

    // 매 프레임 갱신하므로 값이 바뀔 때만 대입 (문자열 갱신 비용 절약)
    private static void SetText(TMP_Text label, string value)
    {
        if (label != null && label.text != value) label.text = value;
    }

    private string DebuffLabel(DebuffType type)
    {
        switch (type)
        {
            case DebuffType.Slow: return "Slow";
            case DebuffType.DamageOverTime: return "DOT";
            case DebuffType.ArmorReduce: return "Sensor";
            case DebuffType.RADAR: return "RADAR";
            default: return "None";
        }
    }

    private string EnemyTypeLabel(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Normal: return "Normal Packet";
            case EnemyType.DDoS: return "DDoS";
            case EnemyType.Trojan: return "Trojan";
            case EnemyType.Ransomware: return "Ransomware";
            case EnemyType.ZeroDay: return "Zero Day";
            case EnemyType.MemoryLeakWorm: return "Memory Leak Worm";
            case EnemyType.APTBoss: return "APT";
            default: return type.ToString();
        }
    }
}