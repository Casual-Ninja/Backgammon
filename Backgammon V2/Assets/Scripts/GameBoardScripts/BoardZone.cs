using UnityEngine;

public class BoardZone : MonoBehaviour
{
    private BoardPoint[] points;

    private void Awake()
    {
        points = new BoardPoint[6];
        for (int i = 0; i < 6; i++)
            points[i] = transform.GetChild(i).GetComponent<BoardPoint>();
    }

    public BoardPoint GetPoint(int index)
    {
        return points[index];
    }

    public void DestroyAllPips()
    {
        foreach (BoardPoint point in points)
            point.DestroyAllPips();
    }

    public float GetZoneHeight()
    {
        return GetComponent<RectTransform>().rect.height;
    }

    public float GetYPosition()
    {
        return transform.localPosition.y;
    }
}
