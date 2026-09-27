using System;
using DevLib.ServiceLocator;
using DG.Tweening;
using TMPro;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using Random = UnityEngine.Random;

public class NicknameInputUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform inputRect;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private RectTransform infoRect;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private RectTransform warningRect;
    private Vector2 originalPos;
    [SerializeField] private string defaultNickname = "건전한 닉네임";
    
    [Header("Size")]
    [SerializeField] private float horizontalPadding = 20f;
    
    [Header("Length")]
    [SerializeField] private int minLength = 1;
    [SerializeField] private int maxLength = 10;
    
    [Header("Banned")]
    [SerializeField] private string[] bannedWords;

    public bool _currentSaveNickname;
    
    private async void Start()
    {
        inputField.onValueChanged.AddListener(SetUI);
        inputField.onEndEdit.AddListener(NameChange);
        originalPos = warningRect.anchoredPosition;
        int random = Random.Range(0, 10) * 1000 +
                     Random.Range(0, 10) * 100 +
                     Random.Range(0, 10) * 10 +
                     Random.Range(0, 10);
        string value = await ServiceLocator.Get<IDataStoreService>().GetAsync<string>("PlayerName");
        if (value == null)
            inputField.text = $"{defaultNickname} {random}";
        else 
            inputField.text = $"{value}";
        NameChange(inputField.text);
        _currentSaveNickname = true;
    }
    private void LateUpdate()
    {
        if (!inputField.isFocused) return;

        int caret = inputField.caretPosition;

        inputField.selectionAnchorPosition = caret;
        inputField.selectionFocusPosition = caret;
    }
    private void SetUI(string text)
    {
        float width = inputField.textComponent.GetPreferredValues(text).x;
        inputRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + horizontalPadding);
    }

    public async void NameChange(string text)
    {
        text = text.Trim();
        if (!LengthCheck(text)) return;
        if (!BannedCheck(text)) return;

        try
        {
            var service = ServiceLocator.Get<IPlayerNameInterfaceChange>();
            if (service == null)
            {
                ShowWarning("닉네임 서비스를 준비하지 못했습니다.");
                return;
            }
            bool result = await service.PlayerNameChange(text);
            if (this == null) return;
            if (result)
            {
                warningText.gameObject.SetActive(false);
                _currentSaveNickname = true;
                ShowInformation("닉네임이 성공적으로 저장되었습니다");
            }
            else
                ShowWarning("이미 있는 닉네임 입니다.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (this != null) ShowWarning("닉네임 저장에 실패했습니다. 다시 시도해주세요.");
        }
    }

    private void ShowInformation(string info)
    {
        infoText.text = info;
        infoText.gameObject.SetActive(true);

        Color color = infoText.color;
        color.a = 1f;
        infoText.color = color;
        
        infoRect.anchoredPosition = originalPos + Vector2.down * 20f;

        infoRect.DOKill();
        Sequence seq = DOTween.Sequence();
        
        seq.Append(infoRect.DOAnchorPos(originalPos, 0.25f));
        seq.AppendInterval(0.8f);
        seq.Append(infoText.DOFade(0f, 0.5f));

        seq.OnComplete(() => { infoText.gameObject.SetActive(false); });
    }

    private bool LengthCheck(string nickname)
    {
        if (nickname.Length >= minLength && nickname.Length <= maxLength) return true;
        ShowWarning($"닉네임은 {minLength}~{maxLength}자로 입력해주세요.");        return false;
    }
    private bool BannedCheck(string nickname)
    {   
        foreach (string bannedWord in bannedWords)
        {
            if (string.IsNullOrEmpty(bannedWord))
                continue;
            if (nickname.Contains(bannedWord, StringComparison.OrdinalIgnoreCase))
            {
                ShowWarning("사용할 수 없는 단어가 포함되어 있습니다.");
                return false;
            }
        }
        return true;
    }

    void ShowWarning(string text)
    {
        warningText.text = text;
        warningText.gameObject.SetActive(true);

        Color color = warningText.color;
        color.a = 1f;
        warningText.color = color;
        
        warningRect.anchoredPosition = originalPos + Vector2.down * 20f;

        warningRect.DOKill();
        Sequence seq = DOTween.Sequence();
        
        seq.Append(warningRect.DOAnchorPos(originalPos, 0.25f));
        seq.AppendInterval(0.8f);
        seq.Append(warningText.DOFade(0f, 0.5f));

        seq.OnComplete(() => { warningText.gameObject.SetActive(false); });
    }
    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(SetUI);
        inputField.onEndEdit.RemoveListener(NameChange);
    }
}