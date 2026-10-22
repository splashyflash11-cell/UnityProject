using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerButtonGenerator : MonoBehaviour
{
    public GameObject towerButtonPrefab;
    public List<Sprite> buttonImages;

    private void Start()
    {
        if (towerButtonPrefab == null || buttonImages == null || GameManager.Instance == null) return;
        for (int i = 0; i < buttonImages.Count; i++)
        {
            GameObject button = Instantiate(towerButtonPrefab, transform);
            Button uiButton = button.GetComponent<Button>();
            if (uiButton != null)
            {
                TowerButtonClickHandler handler = button.GetComponent<TowerButtonClickHandler>();
                if (handler == null) handler = button.AddComponent<TowerButtonClickHandler>();
                handler.Configure(i, i == buttonImages.Count - 1);
                uiButton.onClick.AddListener(handler.HandleClick);
            }

            if (button.transform.childCount > 0)
            {
                Image image = button.transform.GetChild(0).GetComponent<Image>();
                if (image != null) image.sprite = buttonImages[i];
            }

            if (GameManager.Instance.normalTowerPrefabs != null && i < GameManager.Instance.normalTowerPrefabs.Length)
            {
                Tower tower = GameManager.Instance.normalTowerPrefabs[i] != null
                    ? GameManager.Instance.normalTowerPrefabs[i].GetComponent<Tower>()
                    : null;
                ButtonTooltipTrigger tooltip = button.GetComponent<ButtonTooltipTrigger>();
                if (tower != null && tower.data != null && tooltip != null && tooltip.costText != null)
                    tooltip.costText.text = tower.data.useCpu.ToString();
            }
        }
    }
}
