using System;
using UnityEngine;
using WorkFolder.SUH.Scripts.OnTargetingLib.Class;

namespace WorkFolder.SUH.Scripts.OnTargetingLib.ScriptableObject
{
    [CreateAssetMenu(fileName = "targeting data", menuName = "SO/Suxghui/OnTargetLib/TargetSetting SO", order = 0)]
    public class TargetingSettingSO : UnityEngine.ScriptableObject
    {
        [Header("Candidate Settings")] 
        public float searchRadius;
        public float maximumTargetAngle;
        public float candidateRefeshInvertal;
        public int maximumCandidates;
        public LayerMask targetLayerMask;
        public LayerMask lineOfSightMask;
        public bool includeNeutralTarget;

        [Header("Target Unlock")] 
        public float maximumTargetDistance;
        public float occlusionGraceTime;
        public float viewportPadding;
        public float targetSwitchCooldown;
        public float maximumLookDuration;

        [Header("Score Setting")] 
        public float screenCenterWeight;
        public float distanceWeight;
        public float facingWeight;
        public float visibilityWeight;
        public float recenterWeight;
        public float weaponDirectionWeight;
        public float priorityBiasWeight;
        public float currentTargetBonus;
        public float occlusionPenalty;
        public float outsideScreenPenalty;

        private void OnValidate()
        {
            
        }

        public void ResetToDefault()
        {
            
        }

        public void GetTotalScoreWeight()
        {
            
        }
    }
}