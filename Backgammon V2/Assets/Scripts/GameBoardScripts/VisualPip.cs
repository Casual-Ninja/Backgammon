using UnityEngine;
using UnityEngine.UI;

public class VisualPip : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] private HighlighShader highlightManager;
    [SerializeField] private Image colorImage;
    [SerializeField] private float speed;

    public Transform invisiblePip { get; private set; }
    private Vector3 startingPosition;
    private float precentageMoved = 0;

    private float ModifiedSigmoidFunction(float xValue)
    {
        float yValue = 1f / (1 + Mathf.Exp(-20f * (xValue - 0.29f)));

        if (yValue >= 0.9995f) // so there won't be a visible jump
            return 1;

        return yValue;
    }

    public void ChangeColor(Color32 color)
    {
        this.colorImage.color = color;
    }

    public void SetSize(Vector2 size)
    {
        rectTransform.sizeDelta = size;
    }

    public void SetInvisiblePip(Transform invisiblePip)
    {
        this.invisiblePip = invisiblePip;
        invisiblePip.GetComponent<InvisiblePip>().ConnectedVisualPip(this);
    }

    private Vector3 GetWantedPosition()
    {
        Vector3 wantedPosition = invisiblePip.parent.parent.parent.localPosition - transform.parent.localPosition; // the zone position
        wantedPosition += invisiblePip.parent.parent.localPosition + invisiblePip.parent.localPosition; // the point position
        wantedPosition += invisiblePip.localPosition; // the overall wanted position
        
        return wantedPosition;
    }

    public void GoToInvisiblePip()
    {
        rectTransform.localPosition = GetWantedPosition();
    }

    public void MoveTowardsDesignatedPip(float deltaTime)
    {
        Vector3 wantedPosition = GetWantedPosition();

        if (wantedPosition == transform.localPosition)
            return;

        if (precentageMoved == 0)
        {
            startingPosition = transform.localPosition;
            transform.SetAsLastSibling(); // so when he moves will be on top of others in drawing
            BoardViewManager.instance.ChangePipMoveValue(this, true);
        }

        precentageMoved += deltaTime * speed;

        float sigmoidValue = ModifiedSigmoidFunction(precentageMoved);
        transform.localPosition = Vector3.Lerp(startingPosition, wantedPosition, sigmoidValue);

        if (sigmoidValue == 1 || transform.localPosition == wantedPosition) // did i finish moving?
        {
            precentageMoved = 0;
            transform.localPosition = wantedPosition;
            BoardViewManager.instance.ChangePipMoveValue(this, false);
        }
    }

    public void SetHighlight(bool highlight)
    {
        if (highlight)
            highlightManager.UseHighlighShader();
        else
            highlightManager.UseDefaultShader();
    }

    private void Update()
    {
        MoveTowardsDesignatedPip(Time.deltaTime);
    }
}
