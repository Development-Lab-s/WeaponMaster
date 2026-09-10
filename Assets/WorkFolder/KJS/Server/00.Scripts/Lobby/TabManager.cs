using UnityEngine;

public class TabManager : MonoBehaviour
{
    [SerializeField] private GameObject lobbyPanel;
    public void OpenTheTab()
    {
        lobbyPanel.SetActive(true);
    }
    
    public void CloseTheTab()
    {
        lobbyPanel.SetActive(false);
    }
}
