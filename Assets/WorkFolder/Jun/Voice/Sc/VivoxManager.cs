using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

public class VivoxManager : MonoBehaviour
{
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        await VivoxService.Instance.InitializeAsync();
        await VivoxService.Instance.LoginAsync();

        Debug.Log("비복스 로그인 성공");
        
        Channel3DProperties properties = new Channel3DProperties(
            15,   // AudibleDistance
            2,    // ConversationalDistance
            1.0f, // Fade 강도
            AudioFadeModel.InverseByDistance
        );

        await VivoxService.Instance.JoinPositionalChannelAsync(
            "PositionTest",
            ChatCapability.AudioOnly,
            properties
        );

        Debug.Log("Positional Channel Join Success");
    }
}