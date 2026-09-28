using _00.Scripts.Lobby;
using TMPro;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.MapSystem
{
    // 방 만들기 UI의 TMP 드롭다운에 붙인다. 카탈로그의 맵 이름으로 옵션을 채우고,
    // 고른 번호를 LobbyManager에 넘겨 방을 만들 때 세션 속성으로 들어가게 한다.
    [RequireComponent(typeof(TMP_Dropdown))]
    public sealed class MapSelectDropdown : MonoBehaviour
    {
        [Tooltip("비워 두면 Resources/MapCatalog를 쓴다.")]
        [SerializeField] private MapCatalogSO catalog;

        private TMP_Dropdown _dropdown;

        private void Awake()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
            if (catalog == null)
                catalog = MapCatalogSO.LoadDefault();

            _dropdown.ClearOptions();
            if (catalog == null || catalog.Count == 0)
            {
                Debug.LogWarning("[MapSelectDropdown] 맵 카탈로그가 비어 있습니다.", this);
                return;
            }

            var names = new System.Collections.Generic.List<string>(catalog.Count);
            for (int i = 0; i < catalog.Count; i++)
                names.Add(catalog.GetDisplayName(i));
            _dropdown.AddOptions(names);
            _dropdown.onValueChanged.AddListener(OnMapSelected);
        }

        // LobbyManager는 다른 오브젝트라 Awake 순서를 보장할 수 없어 Start에서 맞춘다.
        private void Start()
        {
            if (LobbyManager.instance == null || catalog == null || catalog.Count == 0)
                return;

            int selected = Mathf.Clamp(LobbyManager.instance.SelectedMapIndex, 0, catalog.Count - 1);
            _dropdown.SetValueWithoutNotify(selected);
            LobbyManager.instance.SelectedMapIndex = selected;
        }

        private void OnDestroy()
        {
            if (_dropdown != null)
                _dropdown.onValueChanged.RemoveListener(OnMapSelected);
        }

        private void OnMapSelected(int index)
        {
            if (LobbyManager.instance != null)
                LobbyManager.instance.SelectedMapIndex = index;
        }
    }
}
