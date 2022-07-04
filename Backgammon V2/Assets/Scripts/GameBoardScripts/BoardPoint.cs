using UnityEngine;
using UnityEngine.UI;

public class BoardPoint : MonoBehaviour
{
    [SerializeField] private Image pointImage;
    [SerializeField] private HighlighShader highlightManager;
    [SerializeField] private Button button;
    [SerializeField] private Transform pipHolder;

    public void ChangeColor(Color32 newColor)
    {
        pointImage.color = newColor;
    }

    public Transform GetPipHolder() { return pipHolder; }

    public Transform GetPip(int index) { return pipHolder.GetChild(index); }

    public int PipCount() { return pipHolder.childCount; }

    public Button GetButton() { return button; }

    public void DestroyAllPips()
    {
        for (int i = pipHolder.childCount - 1; i >= 0; i--)
            Destroy(pipHolder.GetChild(i).gameObject);
    }

    public void SetPointHighlight(bool highlight)
    {
        if (highlight)
            highlightManager.UseHighlighShader();
        else
            highlightManager.UseDefaultShader();
    }

    public void SetPipHighlight(bool highlight)
    {
        for (int i = 0; i < PipCount(); i++)
        {
            if (i > 3 || i == PipCount() - 1)
                GetPip(i).GetComponent<InvisiblePip>().visualPip.SetHighlight(highlight);
            else
                GetPip(i).GetComponent<InvisiblePip>().visualPip.SetHighlight(false);
        }
    }

    public void SetCorrectNumberText()
    {
        for (int i = 0; i < PipCount(); i++)
        {
            if (i <= 3 || (PipCount() == 5))
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAbsolute("");
            else
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAbsolute(PipCount().ToString());
        }
    }

    public void SetCorrectNumberTextAtStartOfMovement()
    {
        for (int i = 0; i < PipCount(); i++)
        {
            if (i <= 3 || (PipCount() == 5))
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAtStartOfMovement("");
            else
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAtStartOfMovement(PipCount().ToString());
        }
    }
}
