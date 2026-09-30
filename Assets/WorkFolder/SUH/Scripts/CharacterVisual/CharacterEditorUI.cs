    using System;
    using System.Threading;
    using DevLib.ServiceLocator;
    using DG.Tweening;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using Unity.Services.Authentication;
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
            private GameObject accessoryOne;
            private GameObject accessoryTwo;
            [SerializeField] private GameObject faceDrawPanel;
            private bool saving;
            private bool ready;
            private CharacterFacePreview facePreview;
            private SavedFace previewFace;

            private void Awake() => ApplyAccessoryPreview();

            private void ApplyPreview()
            {
                facePreview ??= new CharacterFacePreview(_meshRenderer);
                facePreview.Apply(_bodyMaterials[BodyColor], previewFace);
                ApplyAccessoryPreview();
            }

            private async System.Threading.Tasks.Task RefreshSavedFace()
            {
                var saved = await FaceStore.Load(AuthenticationService.Instance.PlayerId);
                if (this == null) return;
                previewFace = saved;
                ApplyPreview();
            }

            private void OnDestroy() => facePreview?.Dispose();
            
            
            private uint AccessoryStyleIndex = 0;
            private uint BodyColor;
            private string PlayerName;
    private Vector2 originalPos;
    public UnityEvent OnFinishSave;
    public UnityEvent OnReturnFromFaceDraw;
            

            public void ChangeBtnHair(string count)
            {
                // Keep this UnityEvent entry point for existing scene button bindings.
                ChangeBtnAccessory(count);
            }

            public void ChangeBtnAccessory(string count)
            {
                if (!ready || saving || !int.TryParse(count, out int direction) || direction == 0) return;
                AccessoryStyleIndex = (uint)Mathf.Clamp((int)AccessoryStyleIndex + Math.Sign(direction), 0, 2);
                ApplyAccessoryPreview();
            }

            private async void Start()
            {
                try
                {
                    var service = ServiceLocator.Get<IDataStoreService>();
                    BodyColor = await service.GetAsync<uint>("BodyColor");
                    AccessoryStyleIndex = await service.GetAsync<uint>("AccessoryStyle");
                    if (this == null) return;
                    ApplyPreview();
                    ready = true;
                    await RefreshSavedFace();
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
                    await service.SetAsync<uint>("AccessoryStyle", AccessoryStyleIndex);
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
                    if (!ready)
                    {
                        var service = ServiceLocator.Get<IDataStoreService>();
                        BodyColor = await service.GetAsync<uint>("BodyColor");
                        AccessoryStyleIndex = await service.GetAsync<uint>("AccessoryStyle");
                        if (this == null) return;
                        ready = true;
                    }
                    var faceController = faceDrawPanel.GetComponentInChildren<FaceSaveController>(true);
                    faceController.SetPreviewSkin(BodyColor, _bodyMaterials[BodyColor]);
                    await LobbyFaceDrawFlow.OpenAsync(gameObject.scene, faceDrawPanel, async () =>
                    {
                        if (this == null) return;
                        OnReturnFromFaceDraw?.Invoke();
                        await RefreshSavedFace();
                    });
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

            private void ApplyAccessoryPreview()
            {
                if (accessoryOne == null || accessoryTwo == null)
                {
                    // Search only the preview model's ancestor hierarchy, including inactive meshes.
                    for (Transform root = _meshRenderer != null ? _meshRenderer.transform : null;
                         root != null; root = root.parent)
                    {
                        accessoryOne = AccessoryVisuals.Find(root, "Torus.002");
                        accessoryTwo = AccessoryVisuals.Find(root, "Torus.003");
                        if (accessoryOne != null && accessoryTwo != null) break;
                    }
                }
                AccessoryVisuals.Show(accessoryOne, AccessoryStyleIndex == 1);
                AccessoryVisuals.Show(accessoryTwo, AccessoryStyleIndex == 2);
                if (hairStyleTextLabel != null)
                    hairStyleTextLabel.SetText(AccessoryStyleIndex == 0 ? "악세사리 없음" : $"악세사리 {AccessoryStyleIndex}");
            }

            public void BodyColorIndexChange(string bodyColorIndex)
            {
                if (!ready || saving) return;
                BodyColor = uint.Parse(bodyColorIndex);
                _particleSystem[Convert.ToInt32(bodyColorIndex)].Play();
                ApplyPreview();
            }
        }
    }
