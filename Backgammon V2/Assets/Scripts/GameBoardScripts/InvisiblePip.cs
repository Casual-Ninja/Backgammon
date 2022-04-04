using UnityEngine;

public class InvisiblePip : MonoBehaviour
{
    private const string POINT_PIP_HOLDER_TAG = "PointPipHolder";
    public VisualPip visualPip { get; private set; }
    
    public Vector3 GetWantedPosition()
    {
        if (transform.parent.CompareTag(POINT_PIP_HOLDER_TAG))
        {
            if (transform.GetSiblingIndex() <= 4)
                return transform.position;
            else
                return transform.parent.GetChild(4).position;
        }
        else
            return transform.position;
    }

    public void SetTextAbsolute(string text)
    {
        visualPip.SetTextAbsolute(text);
    }

    public void SetTextAtStartOfMovement(string text)
    {
        visualPip.SetTextAtStartOfMovement(text);
    }

    public void ConnectedVisualPip(VisualPip thePip)
    {
        this.visualPip = thePip;
    }
}
