using UnityEngine;

namespace WorkFolder.PAP.Scripts.MainSystems
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        [SerializeField] private GameObject selectUI;
        [SerializeField] private DropText dropUI;

        private void Start()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void DropDeathUI(string deathMent, string targetBy, int streak) => dropUI.Dead(deathMent, targetBy, streak);

        public void OpenSelectUI()
        {
            selectUI.SetActive(true);
        }
    }
}