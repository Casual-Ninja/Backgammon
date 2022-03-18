using UnityEngine;
using System.Collections;

public class RectResizer : MonoBehaviour
{
    [SerializeField] private bool heightEqualsWidth;
    [SerializeField] private bool widthEqualsHeight;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame(); // so that the canvas can be updated 
        RectTransform rect = GetComponent<RectTransform>();
        if (heightEqualsWidth)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.width);
        }
        else if (widthEqualsHeight)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.rect.height);
        }
    }
}
