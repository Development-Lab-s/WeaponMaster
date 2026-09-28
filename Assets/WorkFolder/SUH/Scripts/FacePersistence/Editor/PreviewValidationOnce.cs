using UnityEditor;
[InitializeOnLoad]
internal static class PreviewValidationOnce
{
    static PreviewValidationOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) FacePersistenceValidation.ValidateLobbyPanel();
        };
    }
}
