using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public Slider healthSlider;
    private GameManager subscribedGameManager;

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
        subscribedGameManager.ResourcesChanged -= RefreshHealth;
        subscribedGameManager = null;
    }

    private void SubscribeToGameManager()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || subscribedGameManager == gameManager) return;
        if (subscribedGameManager != null)
            subscribedGameManager.ResourcesChanged -= RefreshHealth;
        subscribedGameManager = gameManager;
        subscribedGameManager.ResourcesChanged += RefreshHealth;
        RefreshHealth();
    }

    private void RefreshHealth()
    {
        if (healthSlider == null || subscribedGameManager == null) return;
        int maxHealth = subscribedGameManager.defaultServerHP;
        healthSlider.value = maxHealth > 0
            ? Mathf.Clamp01((float)subscribedGameManager.currentServerHP / maxHealth)
            : 0f;
    }
}
