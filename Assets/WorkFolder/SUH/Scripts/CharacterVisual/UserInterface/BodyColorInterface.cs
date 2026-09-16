using System;
using DevLib.ModuleSystem;
using TMPro;
using UnityEngine;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class BodyColorInterface : MonoBehaviour, IAfterInitModule, IBodyColorInterfaceChange
    {
        private void Awake()
        {
            DevLib.ServiceLocator.ServiceLocator.Register<IBodyColorInterfaceChange>(this);
        }

        private void OnDestroy()
        {
            DevLib.ServiceLocator.ServiceLocator.UnRegister<IBodyColorInterfaceChange>(this);
        }

        [SerializeField] private TextMeshProUGUI textLabel_CurrentIndex;
        private IDataStoreService service;
        [SerializeField] private Color[] bodyColors = Array.Empty<Color>();

        public async void BodyColorInterfaceChange(ushort index)
        {
            AfterInit();
            if (service == null) return;
            if (index >= bodyColors.Length)
            {
                Debug.LogError("BodyColorInterface의 bodyColors 팔레트에 해당 인덱스가 없습니다.", this);
                return;
            }
            Color selectedColor = bodyColors[index];
            // Unity Color의 기본 alpha는 0이므로, 팔레트에 alpha를 지정하지 않아도
            // 캐릭터가 투명해지지 않도록 항상 불투명 색상으로 저장합니다.
            selectedColor.a = 1f;
            await service.SetAsync(nameof(PlayerDataStore.BodyColor), selectedColor);
            if (textLabel_CurrentIndex != null) textLabel_CurrentIndex.SetText(index.ToString());
        }

        public void AfterInit()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<IDataStoreService>();
            

        }

        public async void BodyColorTextChange()
        {
            AfterInit();
            
            if (service == null || textLabel_CurrentIndex == null) return;
            Color value = await service.GetAsync<Color>(nameof(PlayerDataStore.BodyColor));
            textLabel_CurrentIndex.SetText(ColorUtility.ToHtmlStringRGBA(value));
        }
    }
}
