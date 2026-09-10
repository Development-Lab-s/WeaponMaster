using UnityEngine;
using UnityEngine.InputSystem;

public class DetectDevKey : MonoBehaviour
{
    [SerializeField] private GameObject devtab;

    private string _input = "";

    private void OnEnable()
    {
        Keyboard.current.onTextInput += OnTextInput;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnTextInput(char character)
    {
        _input += character;

        if (_input.Length > 6)
            _input = _input[^6..];

        if (_input == "devlab")
        {
            OpenTheDevTab();
            _input = "";
        }
    }

    private void OpenTheDevTab()
    {
        devtab.SetActive(true);
    }
}