using UnityEngine;
using UnityEngine.Events;

namespace WorkFolder.KJS.Server._00.Scripts.Lobby
{
    public class LobbyTab: MonoBehaviour
    {
        [SerializeField] private int step;
        [SerializeField] private GameObject tab;
        public UnityEvent OnOpened;
    
        public void ClickTheFirst()
        {
            LobbyTabManager.Instance.CloseTheTabs(step);
            tab.SetActive(true);
            OnOpened?.Invoke();
        }
    
    
    }
}
