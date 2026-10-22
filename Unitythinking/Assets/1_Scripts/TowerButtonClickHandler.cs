using UnityEngine;

public sealed class TowerButtonClickHandler : MonoBehaviour
{
    private int buttonIndex;
    private bool isMainTower;

    public void Configure(int index, bool mainTower)
    {
        buttonIndex = index;
        isMainTower = mainTower;
    }

    public void HandleClick()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnClickTowerPlacingButton(buttonIndex, isMainTower);
    }
}
