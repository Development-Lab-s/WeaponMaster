using System;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


namespace _00.Scripts
{
    public class PlayerColor : NetworkBehaviour
    {
        public MeshRenderer MeshRenderer;
        
        [Networked , OnChangedRender(nameof(ColorChanged))]
        public Color NetWorkedColor{ get; set;}


        public override void Spawned()
        {
            ColorChanged();
        }

        private void Update()
        {
            if (HasStateAuthority && Input.GetKeyDown(KeyCode.E))
            {

                NetWorkedColor = new Color(Random.Range(0f,1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            }
        }

        void ColorChanged()
        {
            MeshRenderer.material.color = NetWorkedColor;
        }
    }
}