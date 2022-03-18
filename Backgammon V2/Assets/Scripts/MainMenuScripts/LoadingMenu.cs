using UnityEngine;
using TMPro;

public delegate Menu ShouldStopLoading();

public class LoadingMenu : Menu
{
    [SerializeField] private TextMeshProUGUI text;
    
    private ShouldStopLoading handler;

    private void Update()
    {
        Menu nextMenu = handler();
        if (nextMenu != null)
            MenuManager.instance.OpenMenu(nextMenu);
    }

    public void StartLoading(string text, ShouldStopLoading handler)
    {
        this.text.text = text;
        this.handler = handler;
        MenuManager.instance.OpenMenu(this);
    }
}
