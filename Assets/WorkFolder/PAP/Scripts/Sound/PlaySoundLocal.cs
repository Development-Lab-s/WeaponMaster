using UnityEngine;

namespace WorkFolder.PAP.Scripts.Sound
{
    public class PlaySoundLocal : MonoBehaviour
    {
        [SerializeField] private SfxSoundEnum targetEnum;
        public void PlaySound()
        {
            BK_SoundManger.instance.PlaySfxLocal(targetEnum);
        }
    }
}