using System;
using DevLib.ServiceLocator;
using Unity.Services.Authentication;
using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    public sealed class FaceSaveController : MonoBehaviour
    {
        [SerializeField] private FaceDraw faceDraw;
        [SerializeField] private Material[] bodyMaterials;
        private uint bodyColor;
        private string status = "얼굴 불러오는 중...";
        private bool ready;
        private bool savedThisSession;

        private void Awake()
        {
            faceDraw.InputLocked = true;
            faceDraw.SaveAsync = async texture =>
            {
                await FaceStore.Save(bodyColor, texture, faceDraw.BackgroundColor);
                savedThisSession = true;
            };
        }

        private async void Start()
        {
            try
            {
                var service = ServiceLocator.Get<IDataStoreService>();
                if (service == null) throw new InvalidOperationException("IDataStoreService is missing.");
                bodyColor = await service.GetAsync<uint>(nameof(PlayerDataStore.BodyColor));
                if (bodyColor >= bodyMaterials.Length || bodyMaterials[bodyColor] == null)
                    throw new InvalidOperationException("BodyColor material index is out of range.");
                var saved = await FaceStore.Load(AuthenticationService.Instance.PlayerId);
                if (this == null) return;
                // Keep the drawing even when only BodyColor changed; replace its old background.
                faceDraw.InitializeCanvas(bodyMaterials[bodyColor],
                    saved?.Decode(),
                    saved?.StoredBackground);
                faceDraw.InputLocked = false;
                ready = true;
                status = "얼굴 준비 완료";
            }
            catch (Exception ex)
            {
                status = "얼굴 불러오기 실패. 씬을 다시 열어주세요.";
                Debug.LogException(ex, this);
            }
        }

        private void OnGUI()
        {
            string text = faceDraw.IsSaving ? "UGS에 저장 중..." : faceDraw.SaveError ?? status;
            if (ready && savedThisSession && !faceDraw.IsDrawing && faceDraw.SaveError == null) text = "얼굴 저장 완료";
            GUI.Box(new Rect(20, 20, 390, 35), text);
        }
    }
}
