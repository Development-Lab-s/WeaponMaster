using Fusion;
using UnityEngine;

namespace WorkFolder.KJS._01.Scripts.Feedback
{
    public abstract class FeedbackAbstract : NetworkBehaviour
    {
        public abstract void PlayFeedBack();
        public abstract void StopFeedback();
    }
}
