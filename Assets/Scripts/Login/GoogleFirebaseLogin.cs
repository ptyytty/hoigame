using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Google;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Extensions;

public class GoogleFirebaseLogin : MonoBehaviour
{
    public Text infoText;
    public string webClientId = "<your client id here>";

    private FirebaseAuth auth;
    private GoogleSignInConfiguration configuration;

    // 외부에서 로그인 성공을 구독할 수 있게 제공
    public event Action<FirebaseUser> OnFirebaseSignInSuccess; // ✅ 파이어베이스 로그인 완료 이벤트

    public bool IsFirebaseReady { get; private set; }

    private void Awake()
    {
        configuration = new GoogleSignInConfiguration { WebClientId = webClientId, RequestEmail = true, RequestIdToken = true };
        CheckFirebaseDependencies();
    }

    private void CheckFirebaseDependencies()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                IsFirebaseReady = true;                 // ✅ 준비 완료
                AddToInformation("Firebase ready.");
            }
            else if (task.IsCompleted)
            {
                AddToInformation("Could not resolve all Firebase dependencies: " + task.Result);
            }
            else
            {
                AddToInformation("Dependency check was not completed. Error: " + task.Exception?.Message);
            }
        });
    }


    public async void SignInWithGoogle()
    {
        if (!IsFirebaseReady)
        {
            AddToInformation("Firebase not ready yet. Please wait...");
            return;
        }

        // Google Sign-In 설정 (webClientId는 인스펙터에서 넣은 값 사용)
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestEmail = true,
            RequestIdToken = true,        // ✅ Firebase 교환용 ID 토큰 필요
            UseGameSignIn = false
        };
        GoogleSignIn.Configuration = configuration;

        try
        {
            AddToInformation("Calling Google Sign-In...");
            var gUser = await GoogleSignIn.DefaultInstance.SignIn();   // ← 계정 선택 팝업
            AddToInformation("Google token received.");                // 여기까지 오면 구글 단계 성공

            // 다음 단계: 구글 ID 토큰을 Firebase Auth로 교환
            SignInWithGoogleOnFirebase(gUser.IdToken);
        }
        catch (GoogleSignIn.SignInException ex)
        {
            // 구글 단계에서 실패(가장 흔한 원인: webClientId 불일치/설정 문제)
            AddToInformation($"Google sign-in failed: {ex.Status} / {ex.Message}");
        }
        catch (Exception ex)
        {
            AddToInformation("Google sign-in unexpected error: " + ex.Message);
        }
    }
    public void SignOutFromGoogle() { OnSignOut(); }

    private void OnSignIn()
    {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;
        AddToInformation("Calling SignIn");

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
    }

    private void OnSignOut()
    {
        AddToInformation("Calling SignOut");
        GoogleSignIn.DefaultInstance.SignOut();
    }

    public void OnDisconnect()
    {
        AddToInformation("Calling Disconnect");
        GoogleSignIn.DefaultInstance.Disconnect();
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            using (IEnumerator<Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                    AddToInformation("Got Error: " + error.Status + " " + error.Message);
                }
                else
                {
                    AddToInformation("Got Unexpected Exception?!?" + task.Exception);
                }
            }
        }
        else if (task.IsCanceled)
        {
            AddToInformation("Canceled");
        }
        else
        {
            AddToInformation("Welcome: " + task.Result.DisplayName + "!");
            AddToInformation("Email = " + task.Result.Email);
            AddToInformation("Google ID Token = " + task.Result.IdToken);
            AddToInformation("Email = " + task.Result.Email);
            SignInWithGoogleOnFirebase(task.Result.IdToken);
        }
    }

    public void SignInWithGoogleOnFirebase(string idToken)
    {
        var credential = GoogleAuthProvider.GetCredential(idToken, null);

        auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                var e = task.Exception?.Flatten().InnerExceptions.FirstOrDefault();
                AddToInformation("Firebase sign-in failed: " + e?.Message);
            }
            else
            {
                AddToInformation("Sign In Successful.");     // ✅ 여기까지 오면 Auth 완료
                OnFirebaseSignInSuccess?.Invoke(auth.CurrentUser); // 닉네임 패널/씬 전환으로 진행
            }
        });
    }

    // 자동 로그인 노출 (Start에서 호출할 용도)
    public void TrySilentSignIn()
    {
        OnSignInSilently();
    }

    public async void OnSignInSilently()
    {
        if (!IsFirebaseReady)
        {
            AddToInformation("Firebase not ready yet. Skipping silent sign-in.");
            return;
        }

        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestEmail = true,
            RequestIdToken = true,
            UseGameSignIn = false
        };
        GoogleSignIn.Configuration = configuration;

        try
        {
            AddToInformation("Calling SignIn Silently");
            var gUser = await GoogleSignIn.DefaultInstance.SignInSilently();
            if (gUser == null)
            {
                AddToInformation("No cached session. Please tap the Sign-In button.");
                return;
            }

            AddToInformation("Silent token received.");
            SignInWithGoogleOnFirebase(gUser.IdToken);
        }
        catch (Exception)
        {
            // Silent 실패는 정상적인 시나리오(첫 실행). 버튼로 유도만 하면 됨.
            AddToInformation("Silent sign-in failed. Please tap the Sign-In button.");
        }
    }

    public void OnGamesSignIn()
    {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = true;
        GoogleSignIn.Configuration.RequestIdToken = false;

        AddToInformation("Calling Games SignIn");

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
    }

    private void AddToInformation(string str) { infoText.text += "\n" + str; }
}
