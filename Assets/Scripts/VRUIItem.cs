using UnityEngine;



[RequireComponent(typeof(RectTransform))]

public class VRUIItem : MonoBehaviour
{

    private BoxCollider boxCollider;

    private RectTransform rectTransform;

    public string value;

    private void OnEnable()
    {

        ValidateCollider();

    }



    private void OnValidate()
    {

        ValidateCollider();

    }



    private void ValidateCollider()
    {

        rectTransform = GetComponent<RectTransform>();



        boxCollider = GetComponent<BoxCollider>();

        if (boxCollider == null)
        {

            boxCollider = gameObject.AddComponent<BoxCollider>();

        }

        boxCollider.size = rectTransform.sizeDelta;

    }

    void Update()
    {
        boxCollider.size = rectTransform.sizeDelta;
    }
}