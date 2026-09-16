using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorkFolder.KJS._01.Scripts.Feedback
{

    public interface IFeedable
    {
        public void PlayAllFeedBacks();
        public void StopAllFeedbacks();
    }
    public sealed class FeedPlayer : MonoBehaviour , IFeedable
    {
        private List<FeedbackAbstract> _feedbacks;
        private void Awake()
        {
            _feedbacks = GetComponents<FeedbackAbstract>().ToList();
        }

        public void PlayAllFeedBacks()
        {
            _feedbacks.ForEach(x => x.PlayFeedBack());
        }

        public void StopAllFeedbacks()
        {
            _feedbacks.ForEach(x => x.StopFeedback());
        }
    }
}
