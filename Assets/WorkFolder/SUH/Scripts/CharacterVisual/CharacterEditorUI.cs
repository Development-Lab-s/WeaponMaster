    using System;
    using System.Threading;
    using DevLib.ServiceLocator;
    using DG.Tweening;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
    using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
    using WorkFolder.SUH.Scripts.FacePersistence;

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
            [SerializeField] private GameObject faceDrawPanel;
            private bool saving;
            private bool ready;
            
            
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

            private async void Start()
            {
                try
                {
                    var service = ServiceLocator.Get<IDataStoreService>();
                    BodyColor = await service.GetAsync<uint>("BodyColor");
                    HairStyleIndex = await service.GetAsync<uint>("HairStyle");
                    if (this == null) return;
                    _meshRenderer.material = _bodyMaterials[BodyColor];
                    ready = true;
                }
                catch (Exception ex) { Debug.LogException(ex, this); }
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
                if (saving || !ready) return;
                saving = true;
                try
                {
                    await NicknameInputUI.PendingSave;
                    if (this == null) return;
                    if (!NicknameInputUI._currentSaveNickname)
                    {
                        ShowWarning("닉네임을 저장하지 않으셨습니다.");
                        return;
                    }
                    var service = ServiceLocator.Get<IDataStoreService>();
                    if (service == null) throw new InvalidOperationException("IDataStoreService missing.");
                    await service.SetAsync<uint>("BodyColor", BodyColor);
                    await service.SetAsync<uint>("HairStyle", HairStyleIndex);
                    var names = ServiceLocator.Get<IPlayerNameInterfaceChange>();
                    if (names != null && !string.IsNullOrWhiteSpace(PlayerName))
                    {
                        if (!await names.PlayerNameChange(PlayerName))
                        {
                            ShowWarning("이미 있는 닉네임입니다.");
                            return;
                        }
                    }
                    await service.SaveAsync();
                    if (this == null) return;
                    OnFinishSave?.Invoke();
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    Debug.Log("닉네임에 변경에 오류뜸 텍스트 표시");
                    ShowWarning("저장에 실패했습니다. 다시 시도해주세요.");
                }
                finally { saving = false; }
            }

            public async void OpenFaceDraw()
            {
                if (saving || LobbyFaceDrawFlow.IsOpen) return;
                saving = true;
                try
                {
                    // Opening the brush does not implicitly save the character profile.
                    if (await LobbyFaceDrawFlow.OpenAsync(gameObject.scene, faceDrawPanel) && this != null)
                        OnFinishSave?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                    if (this != null) ShowWarning("얼굴 편집을 열지 못했습니다. 다시 시도해주세요.");
                }
                finally { saving = false; }
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
