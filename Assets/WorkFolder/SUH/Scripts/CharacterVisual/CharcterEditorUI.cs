    using System.Threading;
    using DevLib.ServiceLocator;
    using DG.Tweening;
    using TMPro;
    using UnityEngine;
    using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;

    namespace WorkFolder.SUH.Scripts.CharacterVisual
    {
        public class CharcterEditorUI : MonoBehaviour
        {
            [SerializeField] private Transform bodyColorSelectButton;
            [SerializeField] private TextMeshProUGUI hairStyleTextLabel;
            
            
            private uint HairStyleIndex = 0;
            private uint BodyColor;
            private string PlayerName;

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

            public async void SaveButtonData()
            {
                try
                {
                    ServiceLocator.Get<IBodyColorInterfaceChange>()?.BodyColorInterfaceChange(BodyColor);
                    ServiceLocator.Get<IHairStyleInterfaceChange>()?.HairStyleInterfaceChange(HairStyleIndex);
                    var names = ServiceLocator.Get<IPlayerNameInterfaceChange>();
                    if (names != null && !string.IsNullOrWhiteSpace(PlayerName))
                    {
                        if (!await names.PlayerNameChange(PlayerName)) Debug.LogWarning("이미 있는 닉네임입니다.");
                    }

                    Debug.Log("씬 변경");
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    Debug.Log("닉네임에 변경에 오류뜸 텍스트 표시");
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
                hairStyleTextLabel.SetText(HairStyleIndex.ToString());
            }

            public void BodyColorIndexChange(string bodyColorIndex)
            {
                BodyColor = uint.Parse(bodyColorIndex);
            }
        }
    }
