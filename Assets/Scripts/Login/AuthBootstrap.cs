using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System;
using System.Threading.Tasks;

public class AuthBootstrap : MonoBehaviour
{
    public static AuthBootstrap Instance { get; private set; }

    // [역할] Firebase 준비/로그인 상태 플래그
    public bool IsFirebaseReady { get; private set; }
    public bool IsSignedIn => FirebaseAuth.DefaultInstance != null && FirebaseAuth.DefaultInstance.CurrentUser != null;

    // [역할] Google Sign-In 설정을 딱 1번만 적용하기 위한 가드
    private static bool s_googleConfigured = false;

    // [역할] 초기화 완료/로그인 성공 이벤트
    public event Action OnFirebaseReady;
    public event Action<FirebaseUser> OnSignInSuccess;
    public event Action<string> OnAuthError;

    [Header("Google Sign-In (필요 시)")]
    [Tooltip("Web Client ID (OAuth 2.0): GoogleSignIn에 한 번만 설정")]
    public string webClientId;

    private void Awake()
    {
        // [역할] 단일 인스턴스 유지 및 중복 파괴
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // [역할] GoogleSignIn Configuration은 'DefaultInstance 생성 전에' 1회만
        ConfigureGoogleOnce();
    }

    private void Start()
    {
        // [역할] Firebase 종속성 체크 및 앱 초기화
        _ = InitFirebaseAsync();
    }

    /// <summary>
    /// [역할] GoogleSignIn Configuration을 앱 시작 시 1회만 적용하여
    ///        "DefaultInstance already created" 예외를 방지
    /// </summary>
    private void ConfigureGoogleOnce()
    {
#if USE_GOOGLE_SIGNIN // 플러그인 사용 시 자체 심볼로 감싸면 좋아요
        if (s_googleConfigured) return;

        if (!string.IsNullOrEmpty(webClientId))
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestEmail = true,
                RequestIdToken = true
            };
            Debug.Log("[Auth] GoogleSignIn configured (once).");
        }
        s_googleConfigured = true;
#endif
    }

    /// <summary>
    /// [역할] Firebase 종속성 점검 및 초기화. 성공 시 IsFirebaseReady를 true로 세팅.
    /// </summary>
    private async Task InitFirebaseAsync()
    {
        try
        {
            var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dep == DependencyStatus.Available)
            {
                IsFirebaseReady = true;
                Debug.Log("[Auth] Firebase ready.");
                OnFirebaseReady?.Invoke();
            }
            else
            {
                OnAuthError?.Invoke($"Firebase dependency not available: {dep}");
            }
        }
        catch (Exception e)
        {
            OnAuthError?.Invoke($"InitFirebaseAsync exception: {e}");
        }
    }

    /// <summary>
    /// [역할] Google 로그인 → Firebase Auth 연동 (중복 설정 없이)
    /// </summary>
    public async void SignInWithGoogle()
    {
        if (!IsFirebaseReady) { OnAuthError?.Invoke("Firebase not ready."); return; }

        try
        {
#if USE_GOOGLE_SIGNIN
            var gsUser = await GoogleSignIn.DefaultInstance.SignIn();
            var credential = GoogleAuthProvider.GetCredential(gsUser.IdToken, null);
            var result = await FirebaseAuth.DefaultInstance.SignInWithCredentialAsync(credential);
            Debug.Log($"[Auth] Sign-In OK: {result.User.Email}");
            OnSignInSuccess?.Invoke(result.User);
#else
            OnAuthError?.Invoke("Google Sign-In plugin not enabled.");
#endif
        }
        catch (Exception e)
        {
            // ⚠ 여기서 흔히 보던 "DefaultInstance already created..."가 난다면
            // → Configuration을 여러 번 바꾸려 한 것. ConfigureGoogleOnce()가 반드시 1회만 실행되도록 유지하세요.
            OnAuthError?.Invoke($"SignIn exception: {e.Message}");
        }
    }

    /// <summary>
    /// [역할] 로그아웃. (주의: 로그아웃해도 GoogleSignIn.Configuration을 다시 바꾸면 안 됨)
    /// </summary>
    public void SignOut()
    {
        try
        {
            FirebaseAuth.DefaultInstance?.SignOut();
#if USE_GOOGLE_SIGNIN
            GoogleSignIn.DefaultInstance?.SignOut();
#endif
            Debug.Log("[Auth] Signed out.");
        }
        catch (Exception e)
        {
            OnAuthError?.Invoke($"SignOut exception: {e.Message}");
        }
    }
}
