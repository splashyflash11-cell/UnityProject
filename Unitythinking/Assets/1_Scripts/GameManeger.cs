using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManeger : MonoBehaviour
{
    public static GameManeger Instance { get; private set; }  // 싱글톤

    public static string FormatCpu(long value)
    {
        return FormatCpu((double)value);
    }

    public static string FormatCpu(double value)
    {
        double magnitude = Math.Abs(value);
        double divisor;
        string suffix;

        if (magnitude >= 1_000_000_000)
        {
            divisor = 1_000_000_000;
            suffix = "B";
        }
        else if (magnitude >= 1_000_000)
        {
            divisor = 1_000_000;
            suffix = "M";
        }
        else if (magnitude >= 1_000)
        {
            divisor = 1_000;
            suffix = "K";
        }
        else
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        return (value / divisor).ToString("0.##", CultureInfo.InvariantCulture) + suffix;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
 

    [Header("타워 목록")]
    public GameObject[] normalTowerPrefabs; // 번호에 매핑할 타워 프리팹 배열
    //public GameObject mainTowerPrefab; // 메인 타워 프리팹
    [FormerlySerializedAs("cpu"), SerializeField] private int cpuValue = 1000;
    [FormerlySerializedAs("ram"), SerializeField] private float ramValue = 0f;
    [FormerlySerializedAs("maxRam"), SerializeField] private float maxRamValue = 100f;
    [FormerlySerializedAs("mainTowerHealth"), SerializeField] private float mainTowerHealthValue = 100f;

    public int cpu => cpuValue;
    public float ram => ramValue;
    public float maxRam => maxRamValue;
    public float mainTowerHealth => mainTowerHealthValue;

    public event Action ResourcesChanged;

    public int defaultServerHP = 100;
    public int currentServerHP;

    void Start()
    {
        currentServerHP = defaultServerHP;
    }

    public void ChangeCpu(int amount)
    {
        SetCpu(cpuValue + amount);
    }

    public void SetCpu(int value)
    {
        if (cpuValue == value) return;

        cpuValue = value;
        NotifyResourcesChanged();
    }

    public void SetRam(float value)
    {
        if (Mathf.Approximately(ramValue, value)) return;

        ramValue = value;
        NotifyResourcesChanged();
    }

    public void SetMaxRam(float value)
    {
        if (Mathf.Approximately(maxRamValue, value)) return;

        maxRamValue = value;
        NotifyResourcesChanged();
    }

    public void SetMainTowerHealth(float value)
    {
        if (Mathf.Approximately(mainTowerHealthValue, value)) return;

        mainTowerHealthValue = value;
        NotifyResourcesChanged();
    }

    private void NotifyResourcesChanged()
    {
        ResourcesChanged?.Invoke();
    }

    
    
    
    
    
    
    public void OnClickTowerPlacingButton(int btnID, bool isMainTower)
    {
        print(btnID);
        int index = btnID;
        if (index >= 0 && index < normalTowerPrefabs.Length)
        {
            TowerPlacementManager.Instance.StartPlacement(normalTowerPrefabs[index], isMainTower);
        }
    }

    public void TakeDamage(int damage)
    {
        currentServerHP -= damage;
        if (currentServerHP <= 0)
        {
            currentServerHP = 0;
            print("게임오버");
            //GameOver();
        }

        NotifyResourcesChanged();
    }
}
