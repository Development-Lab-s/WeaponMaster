using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class NicknameInputUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform inputRect;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private RectTransform warningRect;
    private Vector2 originalPos;

    [Header("Size")]
    [SerializeField] private float horizontalPadding = 20f;
    
    [Header("Length")]
    [SerializeField] private int minLength = 1;
    [SerializeField] private int maxLength = 10;
    
    [Header("Banned")]
    [SerializeField] private string[] bannedWords;
    private void Awake()
    {
        inputField.onValueChanged.AddListener(SetUI);
        inputField.onEndEdit.AddListener(NameChange);
        originalPos = warningRect.anchoredPosition;
    }

    private void SetUI(string text)
    {
        float width = inputField.textComponent.GetPreferredValues(text).x;
        inputRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width + horizontalPadding);
    }

    private void NameChange(string text)
    {
        text = text.Trim();
        if (!LengthCheck(text)) return;
        if (!BannedCheck(text)) return;
        
        
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
        seq.Join(warningText.DOFade(0f, 0.8f));

        seq.OnComplete(() => { warningText.gameObject.SetActive(false); });
    }
    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(SetUI);
        inputField.onEndEdit.RemoveListener(NameChange);
    }
}