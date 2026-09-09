using System;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.WeaponVisualHolder.Lib.Feedback
{
    public abstract class AbstractFeedback : MonoBehaviour
    {
        public abstract void CreateFeedback();

        public abstract void FinishFeedback();

        private void OnDisable()
        {
            FinishFeedback();
        }
    }
}