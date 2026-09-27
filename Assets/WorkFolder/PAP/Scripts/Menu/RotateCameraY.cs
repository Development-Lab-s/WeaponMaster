using System;
using DG.Tweening;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.Menu
{
    public class RotateCameraY : MonoBehaviour
    {
        private void Start()
        {
            transform.DOBlendableRotateBy(new Vector3(0, 360, 0), 30f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }
    }
}