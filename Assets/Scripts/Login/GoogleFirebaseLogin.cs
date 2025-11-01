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
    [Header("UI")]
    public Text infoText; // [역할] 진행/에러 로그를 화면에 출력

    [Header("Google OAuth Web Client ID (.apps.googleusercontent.com)")]
    public string webClientId = "162380263807-iub5h5fkbkco9cn216avoa9ur0qf8esn.apps.googleusercontent.com"; // [역할] 구글 ID 토큰 발급 시 '반드시' Web 클라이언트 ID 사용

    private FirebaseAuth auth;
    private GoogleSignInConfiguration configuration;

    // [디버깅] 씬에 동일 컴포넌트 중복 배치 탐지용
    private static int _instanceCount;
    private static bool s_googleConfigured = false;

    // [역할] 외부에서 Firebase 로그인 성공을 구독할 수 있도록 제공
    public event Action<FirebaseUser> OnFirebaseSignInSuccess;

    public bool IsFirebaseReady { get; private set; }

    // ===================== 공통 유틸/디버그 =====================

    // [역할] 화면(Text)과 콘솔(Logcat)에 모두 로그 출력. infoText 없더라도 콘솔은 반드시 남김.
    private void AddToInformation(string msg)
    {
        try
        {
            if (infoText)
            {
                infoText.text += "\n" + msg;
                // [디버깅] UI 즉시 반영 (모바일에서 텍스트 갱신 지연 방지)
                Canvas.ForceUpdateCanvases();
            }
            else
            {
                Debug.LogWarning("[Auth] infoText is NULL — UI에 표시 불가");
            }
        }
        catch (Exception uiEx)
        {
            Debug.LogWarning($"[Auth] UI log failed: {uiEx.Message}");
        }

        Debug.Log("[Auth] " + msg);
    }

    // [역할] 시작 시 인스턴스/배선/환경 점검 로그 (중복 배치, infoText 연결, EventSystem 유무 등)
    private void SelfDiagnostics(string from)
    {
        _instanceCount = FindObjectsOfType<GoogleFirebaseLogin>(true).Length;
        Debug.Log($"[Auth][{from}] InstanceCount={_instanceCount} (중복이면 버튼이 A, 로그는 B로 찍히는 현상 발생)");

        Debug.Log($"[Auth][{from}] infoText is {(infoText ? "SET" : "NULL")}");
        Debug.Log($"[Auth][{from}] gameObject.activeInHierarchy={gameObject.activeInHierarchy} | enabled={enabled}");

        var es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        Debug.Log($"[Auth][{from}] EventSystem={(es ? "FOUND" : "MISSING")}");
    }

    // [역할] 버튼 배선이 살아있는지 확인용 PING (버튼 OnClick에 연결해서 눌렀을 때 찍히는지 확인)
    public void OnClick_SignInPing()
    {
        if (!infoText) Debug.LogWarning("[Auth] infoText is NULL (Ping) — 인스펙터에 Text 연결 필요");
        if (infoText) infoText.text += "\n[UI] SignIn button PING ✔";
        Debug.Log("[Auth] [UI] SignIn button PING ✔");
    }

    private void ConfigureGoogleOnce()
{
    if (s_googleConfigured) return;

    // 인스펙터에 넣어둔 Web Client ID 사용 (…apps.googleusercontent.com)
    if (string.IsNullOrWhiteSpace(webClientId))
    {
        AddToInformation("[Google] Web Client ID is EMPTY. Set it in the inspector.");
        // 비워두면 일부 기기/버전에서 SignIn 호출 시 크래시 유발 가능 → 진행 중단 권장
        return;
    }

    GoogleSignIn.Configuration = new GoogleSignInConfiguration
    {
        WebClientId   = webClientId,
        RequestEmail  = true,
        RequestIdToken= true,
        // UseGameSignIn = false (기본값)
    };

    s_googleConfigured = true;
    AddToInformation("[Google] GoogleSignIn configured (once).");
}

    // ===================== 라이프사이클 =====================

    private void Awake()
    {
        ConfigureGoogleOnce();
        
        if (AuthBootstrap.Instance)
        {
            if (AuthBootstrap.Instance.IsFirebaseReady)
            {
                IsFirebaseReady = true;
                auth = FirebaseAuth.DefaultInstance; // ←★ 준비 완료 시점에 즉시 Auth 캐싱
            }

            AuthBootstrap.Instance.OnFirebaseReady += () =>
            {
                IsFirebaseReady = true;
                auth = FirebaseAuth.DefaultInstance; // ←★ 항상 캐싱
                AddToInformation("Firebase ready.");
            };

            AuthBootstrap.Instance.OnAuthError += (msg) => AddToInformation("[Auth] " + msg);
        }
    }

    private void Start()
    {
        SelfDiagnostics("Start");
    }

    // ===================== Firebase 초기화 =====================

    // [역할] 파이어베이스 의존성 확인 및 초기화
    private void CheckFirebaseDependencies()
    {
        AddToInformation("ENTER CheckFirebaseDependencies");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                IsFirebaseReady = true;
                AddToInformation("Firebase ready.");
            }
            else if (task.IsCompleted)
            {
                AddToInformation("Could not resolve Firebase dependencies: " + task.Result);
            }
            else
            {
                AddToInformation("Dependency check not completed. Error: " + task.Exception?.Message);
            }
        });
    }

    // ===================== 구글 → Firebase 로그인 플로우 =====================

    // [역할] 사용자 액션으로 구글 로그인(계정 선택 팝업) → Firebase 연동까지 수행
    public async void SignInWithGoogle()
    {
        AddToInformation("ENTER SignInWithGoogle");
        // ✅ 준비 안 되었으면 '진행 안 함'
        if (!IsFirebaseReady) { AddToInformation("Firebase not ready. Please wait."); return; }

        try
        {
            // ✅ 설정은 이미 AuthBootstrap에서 '1회' 적용됨. 여기선 SignIn만!
            AddToInformation("Calling Google Sign-In...");
            var gUser = await GoogleSignIn.DefaultInstance.SignIn();
            AddToInformation("Google token received.");
            SignInWithGoogleOnFirebase(gUser.IdToken);
        }
        catch (GoogleSignIn.SignInException ex)
        {
            // [진단] 구글 단계 실패 상세 (DEVELOPER_ERROR/12500/10 등)
            AddToInformation($"[Google] sign-in failed\n- Status: {ex.Status}\n- Code: {(int)ex.Status}\n- Msg: {ex.Message}");
        }
        catch (Exception ex)
        {
            AddToInformation("[Google] unexpected error: " + ex.Message);
        }
    }

    // [역할] 구글 ID 토큰 → Firebase 자격증명 교환 및 로그인
    public void SignInWithGoogleOnFirebase(string idToken)
    {
        AddToInformation("ENTER SignInWithGoogleOnFirebase");

        if (string.IsNullOrWhiteSpace(idToken))
        {
            AddToInformation("[Firebase] idToken is empty. Aborting.");
            return;
        }

        // ★ 방어코드: 릴리즈 빌드에서 NRE로 크래시되는 것 방지
        if (auth == null)
        {
            auth = FirebaseAuth.DefaultInstance;              // 한번 더 시도
            if (auth == null)                                 // 그래도 없으면 중단
            {
                AddToInformation("[Firebase] Auth not ready (auth==null). Aborting to prevent crash.");
                return;
            }
        }

        var credential = GoogleAuthProvider.GetCredential(idToken, null);
        AddToInformation("[Firebase] Exchanging Google token...");

        auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                var flat = task.Exception?.Flatten();
                var first = flat?.InnerExceptions.FirstOrDefault();
                var fe = first as FirebaseException;
                var code = fe != null ? fe.ErrorCode.ToString() : "no-code";
                AddToInformation($"[Firebase] sign-in failed\n- Code: {code}\n- Detail: {first?.Message}");
            }
            else if (task.IsCanceled)
            {
                AddToInformation("[Firebase] sign-in canceled.");
            }
            else
            {
                AddToInformation("[Firebase] Sign In Successful.");
                OnFirebaseSignInSuccess?.Invoke(auth.CurrentUser);
            }
        });
    }

    // ===================== Silent Sign-In =====================

    // [역할] 앱 시작 시 캐시 세션으로 조용히 로그인 시도
    public void TrySilentSignIn()
    {
        AddToInformation("ENTER TrySilentSignIn");
        OnSignInSilently();
    }

    // [역할] Silent Sign-In 수행(첫 실행 실패는 정상)
    public async void OnSignInSilently()
    {
        AddToInformation("ENTER OnSignInSilently");
        if (!IsFirebaseReady) { AddToInformation("Firebase not ready. Skipping."); return; }

        try
        {
            AddToInformation("Calling SignIn Silently");
            var gUser = await GoogleSignIn.DefaultInstance.SignInSilently();
            if (gUser == null) { AddToInformation("No cached session."); return; }
            AddToInformation("Silent token received.");
            SignInWithGoogleOnFirebase(gUser.IdToken);
        }
        catch (GoogleSignIn.SignInException ex)
        {
            AddToInformation($"[Google Silent] failed\n- Status: {ex.Status}\n- Code: {(int)ex.Status}\n- Msg: {ex.Message}");
            AddToInformation("Silent sign-in failed. Please tap the Sign-In button.");
        }
        catch (Exception ex)
        {
            AddToInformation("[Google Silent] unexpected error: " + ex.Message);
            AddToInformation("Silent sign-in failed. Please tap the Sign-In button.");
        }
    }

    // ===================== 기타(선택) =====================

    // [역할] (구버전 경로) ContinueWith 기반 로그인 — 유지하되 사용 비권장
    private void OnSignIn()
    {
        AddToInformation("ENTER OnSignIn (legacy)");
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;
        AddToInformation("Calling SignIn");
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
    }

    // [역할] (구버전 경로) 구글 로그인 완료 콜백
    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        AddToInformation("ENTER OnAuthenticationFinished (legacy)");
        if (task.IsFaulted)
        {
            using (IEnumerator<Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var error = enumerator.Current as GoogleSignIn.SignInException;
                    if (error != null)
                    {
                        AddToInformation($"[Google] error\n- Status: {error.Status}\n- Code: {(int)error.Status}\n- Msg: {error.Message}");
                    }
                    else
                    {
                        AddToInformation("[Google] unexpected inner exception: " + enumerator.Current.Message);
                    }
                }
                else
                {
                    AddToInformation("[Google] unexpected aggregate exception: " + task.Exception);
                }
            }
        }
        else if (task.IsCanceled)
        {
            AddToInformation("[Google] canceled by user.");
        }
        else
        {
            AddToInformation("Welcome: " + task.Result.DisplayName + "!");
            AddToInformation("Email = " + task.Result.Email);
            AddToInformation("Google ID Token = " + task.Result.IdToken);
            SignInWithGoogleOnFirebase(task.Result.IdToken);
        }
    }

    // [역할] 명시적 로그아웃(구글/파베 모두 세션 클리어)
    public void SignOutFromGoogle() { OnSignOut(); }

    private void OnSignOut()
    {
        AddToInformation("Calling SignOut");
        GoogleSignIn.DefaultInstance.SignOut();
        auth?.SignOut();
    }

    // [역할] 구글 연결 해제
    public void OnDisconnect()
    {
        AddToInformation("Calling Disconnect");
        GoogleSignIn.DefaultInstance.Disconnect();
    }

    // [역할] (선택) Google Play Games 경로 — 현재 프로젝트에선 미사용 권장
    public void OnGamesSignIn()
    {
        AddToInformation("ENTER OnGamesSignIn (not recommended for this project)");
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = true;
        GoogleSignIn.Configuration.RequestIdToken = false;

        AddToInformation("Calling Games SignIn");
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
    }
}
