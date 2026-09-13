using UnityEngine;

namespace WorkFolder.PAP.Scripts.MainSystems
{
    public class SelectStartUI : MonoBehaviour
    {
        public static SelectStartUI Instance;
        [SerializeField] private GameObject selectUI;

        private void Start()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void OpenSelectUI()
        {
            selectUI.SetActive(true);
        }
    }
}