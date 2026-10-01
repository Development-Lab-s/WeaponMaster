using TMPro;
using UnityEngine;
using WorkFolder.SUH.Scripts.ServerCache.Lib.Interface;

namespace WorkFolder.SUH.Scripts.ServerCache
{
    public class ServerDelay : MonoBehaviour, IServerDelayCall
    {
        [SerializeField] private GameObject _notifyObject;
        [SerializeField] private TextMeshProUGUI _textLabel;


        public void IServerDelayStart()
        {
            _notifyObject.SetActive(true);
            
        }
    }
}