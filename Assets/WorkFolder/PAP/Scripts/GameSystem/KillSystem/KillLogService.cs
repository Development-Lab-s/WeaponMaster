using System;
using DevLib.ServiceLocator;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.GameSystem.KillSystem
{
    public class KillLogService : MonoBehaviour, IKillLogService
    {
        [SerializeField] private TextMeshProUGUI killLogText;
        private Vector3 newPos;
        private Vector3 originPos;
        
        private void Awake()
        {
            ServiceLocator.Register<IKillLogService>(this);
        }

        private void Start()
        {
            newPos = killLogText.transform.position + new Vector3(0f, 300f, 0f);
            originPos = killLogText.transform.position;
        }
        
        public void KillLog(PlayerData playerData, PlayerData targetData)
        {
            Color color = new Color(1f, 0.25f, 0.4f, 0f);
            killLogText.color = color;
            killLogText.transform.position = newPos;
            killLogText.SetText($"{playerData.name} has KILLED {targetData.name}.");
            killLogText.DOFade(1f, 0.8f);
            killLogText.transform.DOMove(originPos, 0.8f);
        }
    }
}