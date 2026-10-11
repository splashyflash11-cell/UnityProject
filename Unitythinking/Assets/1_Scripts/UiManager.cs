using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cpuText;
    [SerializeField] private TextMeshProUGUI curRamText;
    [SerializeField] private TextMeshProUGUI mainTowerHealthText;
    [SerializeField] private TextMeshProUGUI maxRamText;

    private GameManeger subscribedGameManager;

    private void OnEnable()
    {
        SubscribeToGameManager();
    }

    private void Start()
    {
        SubscribeToGameManager();
    }

    private void OnDisable()
    {
        if (subscribedGameManager == null) return;

        subscribedGameManager.ResourcesChanged -= RefreshUI;
        subscribedGameManager = null;
    }

    private void SubscribeToGameManager()
    {
        GameManeger gameManager = GameManeger.Instance;
        if (gameManager == null || subscribedGameManager == gameManager) return;

        if (subscribedGameManager != null)
            subscribedGameManager.ResourcesChanged -= RefreshUI;

        subscribedGameManager = gameManager;
        subscribedGameManager.ResourcesChanged += RefreshUI;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (subscribedGameManager == null) return;

        if (cpuText != null)
            cpuText.text = GameManeger.FormatCpu(subscribedGameManager.cpu);
         
        if (curRamText != null)
            curRamText.text = $"{subscribedGameManager.ram:0.##}GB";

        if (maxRamText != null)
            maxRamText.text = $"{subscribedGameManager.maxRam:0.##}GB";

        if (mainTowerHealthText != null)
            mainTowerHealthText.text = $"{100.0f * subscribedGameManager.currentServerHP / subscribedGameManager.defaultServerHP:0.##}%";
    }
}