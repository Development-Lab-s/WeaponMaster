    using System;
    using System.Threading;
    using DevLib.ServiceLocator;
    using DG.Tweening;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;

    namespace WorkFolder.SUH.Scripts.CharacterVisual
    {
        public class CharacterEditorUI : MonoBehaviour
        {
            [SerializeField] private Transform bodyColorSelectButton;
            [SerializeField] private TextMeshProUGUI hairStyleTextLabel;
            [SerializeField] private TMP_Text warningText;
            [SerializeField] private RectTransform warningRect;

            [SerializeField] private NicknameInputUI NicknameInputUI;

            [SerializeField] private ParticleSystem[] _particleSystem;
            [SerializeField] private SkinnedMeshRenderer _meshRenderer;
            [SerializeField] private Material[] _bodyMaterials;
            
            
            private uint HairStyleIndex = 0;
            private uint BodyColor;
            private string PlayerName;
    private Vector2 originalPos;
    public UnityEvent OnFinishSave;
            

            public void ChangeBtnHair(string count)
            {
                if (HairStyleIndex <= 0 && int.Parse(count) < 0)
                    return;

                if (int.Parse(count) >= 0)
                    HairStyleIndex++;
                else
                    HairStyleIndex--;
                
                HairStyleUserInterfaceChange();
            }

            public void ChangePlayerName(string playerName)
            {
                PlayerName = playerName;
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
            
            public async void SaveButtonData()
            {
                if (!NicknameInputUI._currentSaveNickname)
                {
                    ShowWarning("닉네임을 저장하지 않으셨습니다.");
                    return; 
                }
                try
                {
                    ServiceLocator.Get<IBodyColorInterfaceChange>()?.BodyColorInterfaceChange(BodyColor);
                    ServiceLocator.Get<IHairStyleInterfaceChange>()?.HairStyleInterfaceChange(HairStyleIndex);
                    var names = ServiceLocator.Get<IPlayerNameInterfaceChange>();
                    if (names != null && !string.IsNullOrWhiteSpace(PlayerName))
                    {
                        if (!await names.PlayerNameChange(PlayerName)) Debug.LogWarning("이미 있는 닉네임입니다.");
                    }
                    OnFinishSave?.Invoke();
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    Debug.Log("닉네임에 변경에 오류뜸 텍스트 표시");
                    ShowWarning("저장에 실패했습니다. 다시 시도해주세요.");
                }
            }
            
            private void BodyColorUserInterfaceChange()
            {
                foreach (Transform obj in bodyColorSelectButton)
                {
                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(obj.DOScale(1f, 1f));
                    sequence.Kill();
                }
            }

            private void HairStyleUserInterfaceChange()
            {
                //hairStyleTextLabel.SetText(HairStyleIndex.ToString());
            }

            public void BodyColorIndexChange(string bodyColorIndex)
            {
                BodyColor = uint.Parse(bodyColorIndex);
                _particleSystem[Convert.ToInt32(bodyColorIndex)].Play();
                _meshRenderer.material = _bodyMaterials[Convert.ToInt32(bodyColorIndex)];
            }
        }
    }
