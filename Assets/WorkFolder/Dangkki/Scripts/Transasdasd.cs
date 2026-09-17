using UnityEngine;
using UnityEngine.InputSystem;


public class Transasdasd : MonoBehaviour
{
    [SerializeField] private Trans trans;
    private void Update()
    {
        Trans target = trans;
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            target.Play();
    }
}
