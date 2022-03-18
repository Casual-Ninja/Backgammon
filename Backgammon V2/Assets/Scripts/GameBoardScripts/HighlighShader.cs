using UnityEngine;
using UnityEngine.UI;

public class HighlighShader : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Material defaultMat, highlightMat;

    private void Start()
    {
        Material newHighlightMat = new Material(this.highlightMat);
        newHighlightMat.mainTexture = image.mainTexture;
        this.highlightMat = newHighlightMat;
        
        
        //UseHighlighShader();
    }

    public void UseDefaultShader()
    {
        image.material = defaultMat;
    }

    public void UseHighlighShader()
    {
        this.highlightMat.SetColor("_Color", image.color);
        image.material = highlightMat;
    }
}
