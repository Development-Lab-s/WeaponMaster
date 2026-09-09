using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.WeaponVisualHolder.Lib.Feedback
{
    public class FeedbackPlayer : MonoBehaviour
    {
        private List<AbstractFeedback> _feedbackToPlay;

        private void Awake()
        {
            _feedbackToPlay = new List<AbstractFeedback>();
        }

        public void PlayAllFeedback()
        {
           _feedbackToPlay.ForEach(feedback => feedback.CreateFeedback());
        }

        public void StopAllFeedback()
        {
            _feedbackToPlay.ForEach(feedback => feedback.FinishFeedback());
        }
    }
}