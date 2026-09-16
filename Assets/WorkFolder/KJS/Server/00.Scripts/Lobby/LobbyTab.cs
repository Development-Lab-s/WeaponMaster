using UnityEngine;

namespace WorkFolder.KJS.Server._00.Scripts.Lobby
{
    public class LobbyTab: MonoBehaviour
    {
        [SerializeField] private int step;
        [SerializeField] private GameObject tab;
    
        public void ClickTheFirst()
        {
            LobbyTabManager.Instance.CloseTheTabs(step);
            tab.SetActive(true);
        }
    
    
    }
}
