using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class TowerButtonGenerator : MonoBehaviour
{
    public GameObject towerButtonPrefab;
    public List<Sprite> buttonImages;

    void Start()
    {
        
        for (int i = 0; i < buttonImages.Count; i++)
        {
            GameObject button = Instantiate(towerButtonPrefab, transform);
            int bbb = i;
            bool ccc = (i == buttonImages.Count-1 ? true : false);
            button.GetComponent<Button>().onClick.AddListener(() => { GameManeger.Instance.OnClickTowerPlacingButton(bbb, ccc);});
            button.transform.GetChild(0).GetComponent<Image>().sprite = buttonImages[i];
            int cost = GameManeger.Instance.normalTowerPrefabs[i].GetComponent<Tower>().data.useCpu;
            button.GetComponent<ButtonTooltipTrigger>().costText.text = $"{cost}";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
