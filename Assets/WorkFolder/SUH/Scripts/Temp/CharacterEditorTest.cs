using DevLib.ServiceLocator;
using Photon.Client.StructWrapping;
using UnityEngine;
using WorkFolder.PAP.Scripts.GameSystem.KillSystem;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.Temp
{
    public class CharacterEditorTest : MonoBehaviour
    {
        [ContextMenu("HairStyleIndexAdd")]
        public void HairStyleIndexAdd()
        {
            IServerToPlayerSender service = ServiceLocator.Get<IServerToPlayerSender>();

            service?.PlayerHairStyleReceived(1);
        }
        
        
    }
}