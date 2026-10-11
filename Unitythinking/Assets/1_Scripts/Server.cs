using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public Slider healthSlider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        healthSlider.value = 1.0f * GameManeger.Instance.currentServerHP / GameManeger.Instance.defaultServerHP;
    }
}
