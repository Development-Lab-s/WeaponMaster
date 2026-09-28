using System;
using System.Threading.Tasks;
using DevLib.ServiceLocator;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    public sealed class FaceSaveController : MonoBehaviour
    {
        [SerializeField] private FaceDraw faceDraw;
        [SerializeField] private Material[] bodyMaterials;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button returnButton;
        private int loadVersion;
        private uint bodyColor;
        private uint? previewBodyColor;
        private Material previewSkin;
        public void SetPreviewSkin(uint index, Material skin)
        {
            previewBodyColor = index;
            previewSkin = skin;
        }
        private string status = "얼굴 불러오는 중...";
        private bool ready;
        private bool savedThisSession;
        public Action<bool> FlowFinished { get; set; }

        private void Awake()
        {
            faceDraw.InputLocked = true;
            faceDraw.onConfirmed.AddListener(OnConfirmed);
            if (returnButton != null) returnButton.onClick.AddListener(ReturnToCustomization);
            faceDraw.SaveAsync = async texture =>
            {
                await FaceStore.Save(bodyColor, texture, faceDraw.BackgroundColor);
                savedThisSession = true;
            };
        }

        private void OnConfirmed(Texture2D texture) => FlowFinished?.Invoke(true);

        private void OnDestroy()
        {
            if (faceDraw != null) faceDraw.onConfirmed.RemoveListener(OnConfirmed);
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToCustomization);
            FlowFinished?.Invoke(false);
        }

        public void ReturnToCustomization()
        {
            if (faceDraw.IsSaving) return;
            faceDraw.Cancel();
            FlowFinished?.Invoke(false);
        }

        private void OnDisable()
        {
            loadVersion++;
            if (faceDraw != null)
            {
                faceDraw.Cancel();
                faceDraw.InputLocked = true;
            }
            FlowFinished?.Invoke(false);
        }

        private async void OnEnable()
        {
            int version = ++loadVersion;
            ready = false;
            savedThisSession = false;
            status = "얼굴 불러오는 중...";
            faceDraw.InputLocked = true;
            try
            {
                // The inactive Lobby panel may initialize sibling FaceDraw components this frame.
                await Task.Yield();
                if (this == null || version != loadVersion) return;
                var service = ServiceLocator.Get<IDataStoreService>();
                if (service == null) throw new InvalidOperationException("IDataStoreService is missing.");
                bodyColor = previewBodyColor ?? await service.GetAsync<uint>(nameof(PlayerDataStore.BodyColor));
                if (this == null || version != loadVersion) return;
                if (bodyColor >= bodyMaterials.Length || bodyMaterials[bodyColor] == null)
                    throw new InvalidOperationException("BodyColor material index is out of range.");
                var saved = await FaceStore.Load(AuthenticationService.Instance.PlayerId);
                if (this == null || version != loadVersion) return;
                // Keep the drawing even when only BodyColor changed; replace its old background.
                faceDraw.InitializeCanvas(previewSkin != null ? previewSkin : bodyMaterials[bodyColor],
                    saved?.Decode(),
                    saved?.StoredBackground);
                faceDraw.InputLocked = false;
                ready = true;
                status = saved == null ? "저장된 얼굴이 없습니다. 새로 그려주세요." : "얼굴 불러오기 완료";
            }
            catch (Exception ex)
            {
                if (this == null || version != loadVersion) return;
                status = "얼굴 불러오기 실패. 돌아간 뒤 다시 열어주세요.";
                Debug.LogException(ex, this);
            }
        }

        private void Update()
        {
            string text = faceDraw.IsSaving ? "UGS에 저장 중..." : (ready ? faceDraw.SaveError : null) ?? status;
            if (ready && savedThisSession && !faceDraw.IsDrawing && faceDraw.SaveError == null) text = "얼굴 저장 완료";
            if (statusText != null && statusText.text != text) statusText.text = text;
            if (returnButton != null) returnButton.interactable = FlowFinished != null && !faceDraw.IsSaving;
        }
    }
}
