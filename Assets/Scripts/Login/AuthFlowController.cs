using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로그인 씬에서 전반적인 인증 관리
/// - 앱 시작 시 Silent Sign-In 시도
/// - Google 로그인 성공 시 닉네임 UI 초기화
/// - 실패/취소 시에는 수동 로그인 버튼으로 유도
/// </summary>

public class AuthFlowController : MonoBehaviour
{
    public GoogleFirebaseLogin googleLogin; // 로그인 컴포넌트(필수)
    public NicknameUI nicknameUI;           // 닉네임 UI
    public Text infoText;                   // 상태 출력(선택)

    private void Awake()
    {
        // ✅ Firebase 로그인 성공 시 콜백 연결
        googleLogin.OnFirebaseSignInSuccess += OnSignedIn;
    }

    private void Start()
    {
        // ✅ 앱 시작 시 자동 로그인 시도 (이미 로그인한 적 있는 사용자 경험 최적화)
        googleLogin.TrySilentSignIn();
        Log("Silent Sign-In 시도 중...");
    }

    /// <summary>
    /// Google/Firebase 인증이 모두 끝났을 때 호출됨
    /// → 닉네임 유무 확인 및 처리
    /// </summary>
    private void OnSignedIn(FirebaseUser user)
    {
        Log($"로그인 성공: {user.Email}");
        nicknameUI.InitializeForUser(user);
    }

    private void Log(string msg)
    {
        if (infoText) infoText.text += "\n" + msg;
        Debug.Log(msg);
    }
}
