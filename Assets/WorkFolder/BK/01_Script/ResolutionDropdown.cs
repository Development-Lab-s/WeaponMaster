using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResolutionDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private GameObject checkBox;

    // 처음에는 창 모드로 테스트하는 것을 권장합니다.
    [SerializeField] private FullScreenMode screenMode =
        FullScreenMode.Windowed;

    private readonly List<Vector2Int> resolutions =
        new List<Vector2Int>();
    [SerializeField] private UnityEngine.UI.Toggle fullscreenToggle;
    private bool currentbool = true;

    private void Start()
    {
        if (dropdown == null)
        {
            Debug.LogError("해상도 Dropdown을 연결해주세요.", this);
            return;
        }

        foreach (Resolution resolution in Screen.resolutions)
        {
            AddResolution(resolution.width, resolution.height);
        }

        // 현재 창 크기가 모니터 목록에 없어도 표시합니다.
        AddResolution(Screen.width, Screen.height);

        resolutions.Sort((a, b) =>
        {
            int widthComparison = a.x.CompareTo(b.x);
            return widthComparison != 0
                ? widthComparison
                : a.y.CompareTo(b.y);
        });

        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Count; i++)
        {
            Vector2Int resolution = resolutions[i];
            options.Add($"{resolution.x} x {resolution.y}");

            if (resolution.x == Screen.width &&
                resolution.y == Screen.height)
            {
                currentIndex = i;
            }
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(currentIndex);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(ApplyResolution);
        
        screenMode = Screen.fullScreenMode;
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    private void AddResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        var size = new Vector2Int(width, height);

        if (!resolutions.Contains(size))
            resolutions.Add(size);
    }

    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count)
            return;

        Vector2Int size = resolutions[index];
        Screen.SetResolution(size.x, size.y, screenMode);
    }

    private void OnDestroy()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(ApplyResolution);
    }

    public void SetFullscreen(bool value)
    {
        currentbool = !currentbool;
        checkBox.SetActive(currentbool);
        screenMode = currentbool ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        ApplyResolution(dropdown.value);
        
    }
}
