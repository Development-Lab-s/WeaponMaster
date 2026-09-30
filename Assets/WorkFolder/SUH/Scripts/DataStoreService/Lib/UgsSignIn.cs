using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.DataStoreService.Lib
{
    // 여러 스크립트(데이터 저장소, Vivox 등)가 시작 시 각자 익명 로그인을 호출하면
    // 두 번째 호출이 "The player is already signing in." 예외로 실패한다.
    // 모든 로그인을 이 한 곳으로 모아 진행 중인 작업 하나를 함께 기다리게 한다.
    public static class UgsSignIn
    {
        private static Task _signInTask;

        private static bool HasValidAccessToken =>
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsAuthorized &&
            !string.IsNullOrEmpty(AuthenticationService.Instance.AccessToken);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _signInTask = null;
        }

        public static Task EnsureSignedInAsync()
        {
            if (HasValidAccessToken)
                return Task.CompletedTask;

            // 실패한 작업을 계속 돌려주면 한 번의 실패가 영구 실패가 되므로 다시 시도한다.
            if (_signInTask != null && _signInTask.IsCompleted)
                _signInTask = null;

            return _signInTask ??= SignInCoreAsync();
        }

        private static async Task SignInCoreAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (HasValidAccessToken)
                return;

            string previousPlayerId = AuthenticationService.Instance.PlayerId;

            try
            {
                // Expired still counts as IsSignedIn in UGS. Resume the cached session;
                // do not silently replace an existing account when its session token is missing.
                await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions
                {
                    CreateAccount = string.IsNullOrEmpty(previousPlayerId)
                });
            }
            catch (AuthenticationException exception)
                when (exception.ErrorCode == AuthenticationErrorCodes.ClientInvalidUserState)
            {
                // 이 클래스를 거치지 않은 다른 코드가 이미 로그인 중이다. 그 결과를 기다린다.
                await WaitForSignInResultAsync();
            }
            if (!HasValidAccessToken)
                throw new InvalidOperationException("로그인 후 유효한 인증 토큰을 받지 못했습니다. 다시 시도해주세요.");
            if (!string.IsNullOrEmpty(previousPlayerId) && previousPlayerId != AuthenticationService.Instance.PlayerId)
                throw new InvalidOperationException("인증 중 계정이 변경되었습니다. 다시 로그인해주세요.");
        }

        private static async Task WaitForSignInResultAsync()
        {
            IAuthenticationService authentication = AuthenticationService.Instance;
            if (HasValidAccessToken)
                return;

            var completion = new TaskCompletionSource<bool>();

            void OnSignedIn()
            {
                Unsubscribe();
                if (HasValidAccessToken) completion.TrySetResult(true);
                else completion.TrySetException(new InvalidOperationException("로그인 토큰을 받지 못했습니다."));
            }

            void OnSignInFailed(RequestFailedException exception)
            {
                Unsubscribe();
                completion.TrySetException(exception);
            }

            void Unsubscribe()
            {
                authentication.SignedIn -= OnSignedIn;
                authentication.SignInFailed -= OnSignInFailed;
            }

            authentication.SignedIn += OnSignedIn;
            authentication.SignInFailed += OnSignInFailed;

            // 구독하는 사이에 로그인이 끝났을 수 있다.
            if (HasValidAccessToken)
                OnSignedIn();

            try
            {
                if (await Task.WhenAny(completion.Task, Task.Delay(30000)) != completion.Task)
                    throw new TimeoutException("로그인 대기 시간이 초과되었습니다. 다시 시도해주세요.");
                await completion.Task;
            }
            finally { Unsubscribe(); }
        }
    }
}
