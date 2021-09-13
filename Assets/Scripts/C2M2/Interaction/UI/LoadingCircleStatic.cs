using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingCircleStatic : MonoBehaviour
{
    // Start is called before the first frame update
    private Image rectComponent;
    private float rotationSpeed = 0.5f;
    void Start()
    {
        rectComponent = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        rectComponent.fillAmount = (rectComponent.fillAmount + Time.deltaTime * rotationSpeed) % 1;
        //GetComponentInParent<Transform>().position = Camera.main.transform.position;
    }
}
