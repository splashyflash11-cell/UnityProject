using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// IPointerEnterHandler, IPointerExitHandler 인터페이스를 구현하여 마우스 오버/이탈을 감지합니다.
public class ButtonTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    [SerializeField] public TextMeshProUGUI costText;

    public void Start()
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(false); // 초기에는 툴팁을 비활성화
        }
    }

    // 마우스 커서가 버튼 영역 안으로 들어왔을 때 호출
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(true);
        }
    }

    // 마우스 커서가 버튼 영역 밖으로 나갔을 때 호출
    public void OnPointerExit(PointerEventData eventData)
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(false);
        }
    }

    // 버튼이 비활성화될 때 툴팁이 화면에 남아있는 것을 방지
    private void OnDisable()
    {
        if (costText != null)
        {
            costText.gameObject.SetActive(false);
        }
    }
}