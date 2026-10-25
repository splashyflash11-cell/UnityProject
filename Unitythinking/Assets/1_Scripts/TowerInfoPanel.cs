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

    [Header("랜섬웨어 감염 UI (비워 두면 건너뜀)")]
    [SerializeField] private Image lockIconImage;             // 잠금 아이콘
    [SerializeField] private Button decryptButton;
    [SerializeField] private TextMeshProUGUI decryptButtonText;

    [Header("오른쪽 아래 여백")]
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);

    private static readonly Color LockColor = new Color(0.75f, 0.5f, 1f);

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
        if (decryptButton != null) decryptButton.onClick.AddListener(OnDecryptClicked);
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
        UpdateBorder(false, Color.clear);
        ClearLockUI();
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

        bool locked = tower.isLocked;

        string n = d != null ? d.towerName : tower.name;
        SetText(nameText, hacked ? $"{n} [Hacked]" : locked ? $"{n} [Infected]" : n);
        if (nameText != null) nameText.color = hacked ? Color.red : locked ? LockColor : Color.white;

        if (d == null)
        {
            SetText(infoText, "NoData");
        }
        else
        {
            // Tower.cs 의 해킹 페널티와 동일하게 표시
            int dmg = hacked ? (int)(d.attackDamage * 0.1f) : d.attackDamage;
            float interval = hacked ? d.attackInterval * 2f : d.attackInterval;

            string text =
                $"AD  {dmg} | " +
                $"AS  {interval:0.##}s\n" +
                $"Range  {d.attackRange:0.##}\n" +
                $"Cost  {d.useCpu} | " +
                $"Usage  {d.useRam:0.##}GB";

            if (d.isDebuffTower)
                text += $"\nAbility  {DebuffLabel(d.debuffType)} {d.debuffValue:0.##} / {d.debuffDuration:0.##}s";
            else if (d.debuffType == DebuffType.RADAR)
                text += "\nAbility  Stealth Detection";

            if (locked) text += "\nStatus  Infected · Attack Disabled";

            int sec = (int)tower.AliveTime;
            text += $"\nKill : {tower.KillCount} | Dmg  {tower.TotalDamage:0} | Time : {sec / 60:00}:{sec % 60:00}";

            SetText(infoText, text);
        }

        if (hpSlider != null)
            hpSlider.value = tower.maxHp > 0 ? (float)tower.currentHp / tower.maxHp : 0f;
        SetText(hpText, $"{tower.currentHp} / {tower.maxHp}");

        // 안내 문구
        string hint = "";
        if (!pinned) hint = locked ? "Infected · Click to pin" : "Click to pin";
        else if (hacked && d != null)
            hint = $"Hacked · Attack to CPU +{d.useCpu} | RAM -{d.useRam:0.##}GB";
        else if (locked)
            hint = $"Infected · Decryption cost CPU {tower.DecryptCost}";
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

        // 복호화 버튼 (고정 상태 + 감염된 타워만, CPU가 모자라면 비활성화)
        bool showDecrypt = pinned && locked;
        if (decryptButton != null)
        {
            if (decryptButton.gameObject.activeSelf != showDecrypt) decryptButton.gameObject.SetActive(showDecrypt);
            if (showDecrypt)
            {
                bool affordable = GameManager.Instance != null && GameManager.Instance.cpu >= tower.DecryptCost;
                decryptButton.interactable = affordable;
                SetText(decryptButtonText, affordable
                    ? $"Dec CPU -{tower.DecryptCost}"
                    : $"Dec CPU -{tower.DecryptCost} (Insufficient)");
            }
        }

        // 잠금 아이콘
        if (lockIconImage != null && lockIconImage.gameObject.activeSelf != locked)
            lockIconImage.gameObject.SetActive(locked);

        UpdateBorder(hacked || locked, hacked ? Color.red : LockColor);
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
            UpdateBorder(false, Color.clear);
            ClearLockUI();
            return;
        }

        SetText(nameText, $"{d.enemyName} [{EnemyTypeLabel(d.enemyType)}]");
        if (nameText != null) nameText.color = new Color(1f, 0.6f, 0.2f);

        string text =
            $"Speed  {enemy.MoveSpeed:0.##}\n" +
            $"Reward +{d.cpuReward}\n" +
            $"Damage  {d.attackDamage}";

        if (d.isStealth) text += "\n\nStealth · Only Radar Towers can attack";
        if (d.isInvulnerable)
            text += enemy.IsInvulnerable ? "\n\nInvulnerable · Can be defeated with reduced defense attacks" : "\n\nInvulnerable · Unactivated";
        if (d.hacking) text += "\n\nHacking · Periodically hacks towers";
        if (d.ramReduction > 0f) text += $"\n\nRAM Reduction {d.ramReduction:0.##}GB";

        SetText(infoText, text);

        float max = d.maxHealth;
        if (hpSlider != null) hpSlider.value = max > 0f ? enemy.CurrentHealth / max : 0f;
        SetText(hpText, $"{Mathf.CeilToInt(enemy.CurrentHealth)} / {Mathf.CeilToInt(max)}");

        SetText(hintText, pinned ? "" : "Click to pin");

        if (sellButton != null && sellButton.gameObject.activeSelf) sellButton.gameObject.SetActive(false);
        UpdateBorder(false, Color.clear);
        ClearLockUI();
    }

    // ───────────── 공통 ─────────────
    private void OnDecryptClicked()
    {
        if (tower != null && pinned && tower.isLocked)
            tower.TryDecrypt();
    }

    // 타워가 아닌 대상을 보여줄 때 / 닫을 때 감염 UI 숨김
    private void ClearLockUI()
    {
        if (lockIconImage != null && lockIconImage.gameObject.activeSelf) lockIconImage.gameObject.SetActive(false);
        if (decryptButton != null && decryptButton.gameObject.activeSelf) decryptButton.gameObject.SetActive(false);
    }

    private void OnSellClicked()
    {
        if (TowerSelectionManager.Instance != null)
            TowerSelectionManager.Instance.SellSelected();
    }

    // 해킹 상태면 테두리(없으면 배경색)를 붉게 깜빡인다
    private void UpdateBorder(bool alert, Color color)
    {
        float blink = Mathf.PingPong(Time.unscaledTime * 3f, 1f);

        if (borderOutline != null)
        {
            borderOutline.enabled = alert;
            if (alert)
                borderOutline.effectColor = Color.Lerp(new Color(color.r, color.g, color.b, 0.2f), new Color(color.r, color.g, color.b, 1f), blink);
        }
        else if (background != null)
        {
            Color alertColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, backgroundBase.a);
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