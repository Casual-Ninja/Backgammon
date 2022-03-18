using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VisualDie : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dieText;
    [SerializeField] private Image dieImage;
    [SerializeField] private float usedAlphaValue;

    private float lastAlpha = 1;

    public void SetUsed(bool used)
    {
        if (used)
        {
            dieImage.color = new Color(dieImage.color.r, dieImage.color.g, dieImage.color.b, usedAlphaValue);
            dieText.color = new Color(dieText.color.r, dieText.color.g, dieText.color.b, usedAlphaValue);
        }
        else
        {
            dieImage.color = new Color(dieImage.color.r, dieImage.color.g, dieImage.color.b, 1);
            dieText.color = new Color(dieText.color.r, dieText.color.g, dieText.color.b, 1);
        }
    }

    public void SetDieValue(byte dieValue)
    {
        this.dieText.text = dieValue.ToString();
    }

    public int GetDieValue()
    {
        return int.Parse(dieText.text);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            dieImage.color = new Color(dieImage.color.r, dieImage.color.g, dieImage.color.b, lastAlpha);
            dieText.color = new Color(dieText.color.r, dieText.color.g, dieText.color.b, lastAlpha);
        }
        else
        {
            lastAlpha = dieImage.color.a;
            dieImage.color = new Color(dieImage.color.r, dieImage.color.g, dieImage.color.b, 0);
            dieText.color = new Color(dieText.color.r, dieText.color.g, dieText.color.b, 0);
        }
    }

    public bool IsEnabled()
    {
        return dieImage.color.a != 0;
    }

    public bool IsUsed()
    {
        return dieImage.color.a == usedAlphaValue;
    }
}
