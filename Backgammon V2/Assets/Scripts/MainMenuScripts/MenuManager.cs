using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [SerializeField] private Menu[] menus;
    [SerializeField] private Menu currentMenu;
    [SerializeField] private LoadingMenu loadingMenu;

    private void Awake()
    {
        instance = this;

        if (currentMenu != null)
            currentMenu.ChangeActiveState(true);
    }

    public void OpenMenu(Menu newMenu)
    {
        currentMenu.ChangeActiveState(false);
        newMenu.ChangeActiveState(true);
        currentMenu = newMenu;
    }

    public void OpenMenu(string newMenu)
    {
        currentMenu.ChangeActiveState(false);
        foreach (Menu m in menus)
        {
            if (m.name == newMenu)
            {
                m.ChangeActiveState(true);
                currentMenu = m;
                break;
            }
        }
    }

    public LoadingMenu GetLoadingMenu()
    {
        return loadingMenu;
    }
}