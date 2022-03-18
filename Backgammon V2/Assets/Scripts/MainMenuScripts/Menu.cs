using UnityEngine;

public class Menu : MonoBehaviour
{
    public void ChangeActiveState(bool state)
    {
        gameObject.SetActive(state);
    }
}
