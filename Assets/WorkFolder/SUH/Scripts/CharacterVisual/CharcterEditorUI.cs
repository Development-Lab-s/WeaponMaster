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
        
        private ushort HairStyleIndex = 0;
        private ushort BodyColor;
        private string PlayerName;
        
        public void ChangeBtnHair(ushort count)
        {
            HairStyleIndex += count;
            HairStyleUserInterfaceChange();
        }

        public void ChangeBtnBodyColor(ushort count)
        {
            BodyColor += count;
            BodyColorUserInterfaceChange();
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
            
        }
    }
}