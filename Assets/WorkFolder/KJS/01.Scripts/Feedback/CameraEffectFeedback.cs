using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using WorkFolder.KJS.SO;

namespace WorkFolder.KJS._01.Scripts.Feedback
{
    public class CameraEffectFeedback : FeedbackAbstract 
    {
        [SerializeField] private float duration;
        
        private Coroutine _delayCoroutine;
        public override void PlayFeedBack()
        {
            if (!HasStateAuthority) return;
            
            if(_delayCoroutine != null)
                StopCoroutine(_delayCoroutine);
            _delayCoroutine =  StartCoroutine(IDelayEnumerator());
        }

        public override void StopFeedback()
        {
            
        }

        public IEnumerator IDelayEnumerator()
        {
            EffectManager.Instance.vignette.SetActive(true);
            yield return new WaitForSeconds(duration);
            EffectManager.Instance.vignette.SetActive(false);
            _delayCoroutine = null;
        }
    }
}