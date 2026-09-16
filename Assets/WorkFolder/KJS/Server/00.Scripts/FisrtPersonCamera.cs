using System;
using UnityEngine;
using UnityEngine.UI;

namespace _00.Scripts
{
    public class FisrtPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float mouseSensivity = 10f;

        private float verticalRotation;
        private float horizontalRotation;

        private void LateUpdate()
        {
            if (target == null) return;
            
            transform.position = target.position;
            
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            verticalRotation -= mouseY * mouseSensivity;
            verticalRotation = Mathf.Clamp(verticalRotation, -70f, 70f);

            horizontalRotation += mouseX * mouseSensivity;
            
            transform.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);

        }
    }
}
