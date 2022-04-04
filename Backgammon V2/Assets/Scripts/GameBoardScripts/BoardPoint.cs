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

    private int index = -5;
    public int GetIndex()
    {
        if (index != -5)
            return index;

        return transform.GetSiblingIndex() + transform.parent.GetSiblingIndex() * 6;
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
        GetPip(PipCount() - 1).GetComponent<InvisiblePip>().visualPip.SetHighlight(highlight);
    }

    public void SetCorrectNumberText()
    {
        for (int i = 0; i < pipHolder.childCount; i++)
        {
            if (i <= 4 || i != pipHolder.childCount - 1)
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAbsolute("");
            else
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAbsolute((i + 1).ToString());
        }
    }

    public void SetCorrectNumberTextAtStartOfMovement()
    {
        for (int i = 0; i < pipHolder.childCount; i++)
        {
            if (i <= 4 || i != pipHolder.childCount - 1)
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAtStartOfMovement("");
            else
                pipHolder.GetChild(i).GetComponent<InvisiblePip>().SetTextAtStartOfMovement((i + 1).ToString());
        }
    }
}
