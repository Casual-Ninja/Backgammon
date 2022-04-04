using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VisualPip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] RectTransform rectTransform;
    [SerializeField] private HighlighShader highlightManager;
    [SerializeField] private Image colorImage;
    [SerializeField] private float speed;

    public InvisiblePip invisiblePip { get; private set; }
    private Vector3 startingPosition;
    private float precentageMoved = 0;
    private string wantedText = "";
    private bool changedText = true;

    private float ModifiedSigmoidFunction(float xValue)
    {
        float yValue = 1f / (1 + Mathf.Exp(-20f * (xValue - 0.29f)));

        if (yValue >= 0.9995f) // so there won't be a visible jump
            return 1;

        return yValue;
    }

    public void SetColor(Color32 color)
    {
        this.colorImage.color = color;
        numberText.color = new Color(1 - color.r, 1 - color.g, 1 - color.b, color.a);
    }

    public void SetSize(Vector2 size)
    {
        rectTransform.sizeDelta = size;
    }

    public void SetInvisiblePip(Transform invisiblePip)
    {
        this.invisiblePip = invisiblePip.GetComponent<InvisiblePip>();
        this.invisiblePip.ConnectedVisualPip(this);
    }

    private bool VectorsEqual(Vector3 v1, Vector3 v2)
    {
        if (Vector3.SqrMagnitude(v1 - v2) <= 0.0005f)
            return true;
        return false;
    }

    private Vector3 GetWantedPosition()
    {
        return invisiblePip.GetWantedPosition();
    }

    public void SetTextAtStartOfMovement(string text)
    {
        if (numberText.text != text)
        {
            wantedText = text;
            changedText = false;
        }
    }

    public void SetTextAbsolute(string text)
    {
        numberText.text = text;
        changedText = true;
    }

    public void GoToInvisiblePip()
    {
        transform.position = GetWantedPosition();
        precentageMoved = 0f;
        BoardViewManager.instance.ChangePipMoveValue(this, false);
    }

    public void MoveTowardsDesignatedPip()
    {
        Vector3 wantedPosition = GetWantedPosition();

        if (VectorsEqual(wantedPosition,transform.position))
        {
            if (changedText == false)
            {
                // i don't need to move, but i also need to change text
                // meaning either someone moved from me so now i show the text,
                // or someone moved to me, so i should stop showing text when he reaches me
                // can only do first option here so...
                if (wantedText != "")
                {
                    numberText.text = wantedText;
                    changedText = true;
                }
            }
            return;
        }

        if (precentageMoved == 0)
        {
            if (changedText == false)
            {
                // i need to move, and also need to change text
                numberText.text = "";
                if (wantedText == "")
                    changedText = true;
            }

            startingPosition = transform.position;
            transform.SetAsLastSibling(); // so when he moves will be on top of others in drawing
            BoardViewManager.instance.ChangePipMoveValue(this, true);
        }

        precentageMoved += Time.deltaTime * speed;

        float sigmoidValue = ModifiedSigmoidFunction(precentageMoved);
        transform.position = Vector3.Lerp(startingPosition, wantedPosition, sigmoidValue);

        if (sigmoidValue == 1 || VectorsEqual(transform.position, wantedPosition)) // did i finish moving?
        {   
            precentageMoved = 0;
            if (changedText == false)
            {
                // i finished moving, and now need to change number, meaning im on top right now
                numberText.text = wantedText;
                changedText = true;
            }
            
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
        if (invisiblePip != null)
            MoveTowardsDesignatedPip();
    }
}
