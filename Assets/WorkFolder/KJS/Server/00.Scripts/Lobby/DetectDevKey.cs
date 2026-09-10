using UnityEngine;

public class DetectDevKey : MonoBehaviour
{
    [SerializeField] private GameObject devtab;

    private string _input = "";

    private void Update()
    {
        foreach (char c in Input.inputString)
        {
            _input += c;

            if (_input.Length > 6)
                _input = _input[^6..];

            if (_input == "devlab")
            {
                OpenTheDevTab();
                _input = "";
            }
        }
    }

    private void OpenTheDevTab()
    {
        devtab.SetActive(true);
    }
}