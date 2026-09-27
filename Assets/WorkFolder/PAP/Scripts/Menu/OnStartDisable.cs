using System;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.Menu
{
    public class OnStartDisable : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(false);
        }
    }
}