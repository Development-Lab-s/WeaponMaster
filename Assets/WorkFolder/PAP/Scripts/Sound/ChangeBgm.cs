using UnityEngine;

namespace WorkFolder.PAP.Scripts.Sound
{
    public class ChangeBgm : MonoBehaviour
    {
        [SerializeField] private BGMSoundEnum targetEnum;
        public void ChangePlayingBgm()
        {
            BK_SoundManger.instance.UpdateBGMSoundState(targetEnum);
        }
    }
}