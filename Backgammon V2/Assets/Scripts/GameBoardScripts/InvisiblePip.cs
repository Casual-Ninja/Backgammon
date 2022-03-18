using UnityEngine;

public class InvisiblePip : MonoBehaviour
{
    public VisualPip visualPip { get; private set; }
    
    public void ConnectedVisualPip(VisualPip thePip)
    {
        this.visualPip = thePip;
    }
}
