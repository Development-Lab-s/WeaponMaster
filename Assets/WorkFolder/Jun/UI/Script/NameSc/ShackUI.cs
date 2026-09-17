using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShackUI : MonoBehaviour
{
    [SerializeField] private Transform MyTrm;

    [SerializeField] private float xGam;
    [SerializeField] private float YGam;

    private void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        GeSan(mousePos);
    }

    private void GeSan(Vector2 mousePos)
    {
        float distance = Vector2.Distance(mousePos, MyTrm.position);
        
        
    }
}
