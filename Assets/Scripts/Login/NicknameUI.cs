using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 닉네임 입력/검증/저장을 담당하는 UI 컨트롤러
/// - 처음 로그인한 유저만 보여주고, 기존 유저는 바로 Main으로 보냄
/// - 닉네임 중복은 nicknames/{lowerNickname} 문서 존재로 판별
/// - 성공 시 profiles/{uid} 생성 후 Main 씬으로 이동
/// </summary>

public class NicknameUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;         // 닉네임 패널(표시/숨김)
    public TMP_InputField nicknameInput; // 닉네임 입력 필드
    public Button confirmButton;     // 확정 버튼
    public TMP_Text messageText;         // 상태/오류 메시지

    [Header("Config")]
    public string mainSceneName = "MainScene"; // 로그인/설정 완료 후 이동할 씬 이름

    private FirebaseUser _user;
    private FirebaseFirestore _db;

    /// <summary>
    /// 외부에서 로그인 완료 후 호출 (AuthFlowController에서 연결)
    /// 현재 유저 정보로 프로필 존재 여부 확인 → 없으면 패널 표시
    /// </summary>
    public async void InitializeForUser(FirebaseUser user)
    {
        _user = user;
        _db = FirebaseFirestore.DefaultInstance;

        // 이미 프로필이 있는지 확인
        var profileSnap = await _db.Collection("profiles").Document(_user.UserId).GetSnapshotAsync();
        if (profileSnap.Exists)
        {
            // 이미 닉네임 설정 완료 → 메인으로
            LoadMainScene();
        }
        else
        {
            // 처음 로그인 → 닉네임 패널 표시
            panel.SetActive(true);
            messageText.text = "닉네임을 입력하세요. (2~12자, 영문/숫자/한글/언더스코어)";
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickConfirm);
        }
    }

    /// <summary>
    /// "확정" 버튼 콜백: 닉네임 유효성 → 중복검사 → Batch로 nicknames/profiles 생성
    /// </summary>
    private async void OnClickConfirm()
    {
        confirmButton.interactable = false;
        messageText.text = "확인 중...";

        var raw = nicknameInput.text?.Trim();
        if (!IsNicknameValid(raw))
        {
            messageText.text = "닉네임 형식을 확인하세요. (2~12자, 영문/숫자/한글/언더스코어)";
            confirmButton.interactable = true;
            return;
        }

        string lower = raw.ToLowerInvariant();

        // 닉네임 점유 문서 확인
        var nickRef = _db.Collection("nicknames").Document(lower);
        var nickSnap = await nickRef.GetSnapshotAsync();
        if (nickSnap.Exists)
        {
            messageText.text = "이미 사용 중인 닉네임입니다. 다른 닉네임을 입력하세요.";
            confirmButton.interactable = true;
            return;
        }

        // Batch로 닉네임 점유 + 프로필 생성
        WriteBatch batch = _db.StartBatch();

        // 1) 닉네임 점유표
        batch.Set(nickRef, new
        {
            uid = _user.UserId,
            createdAt = FieldValue.ServerTimestamp
        });

        // 2) 프로필
        var profileRef = _db.Collection("profiles").Document(_user.UserId);
        batch.Set(profileRef, new
        {
            uid = _user.UserId,
            email = _user.Email ?? "",
            nickname = raw,
            createdAt = FieldValue.ServerTimestamp
        });

        try
        {
            await batch.CommitAsync();
            messageText.text = "닉네임 설정 완료!";
            LoadMainScene();
        }
        catch (System.Exception e)
        {
            // (드물지만) 경합 시 에러 발생 가능 → 재시도 유도
            messageText.text = "설정 중 오류가 발생했습니다. 다시 시도하세요.\n" + e.Message;
            confirmButton.interactable = true;
        }
    }

    /// <summary>
    /// 닉네임 유효성 검사 (모바일 키보드 입력 고려, 한글/영문/숫자/언더스코어, 2~12자)
    /// </summary>
    private bool IsNicknameValid(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (s.Length < 2 || s.Length > 12) return false;
        foreach (var ch in s)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '_' || IsKorean(ch)))
                return false;
        }
        return true;

        bool IsKorean(char c) => (c >= 0xAC00 && c <= 0xD7A3); // 한글 완성형 범위
    }

    /// <summary>
    /// 메인 씬으로 로드
    /// </summary>
    private void LoadMainScene()
    {
        panel.SetActive(false);
        SceneManager.LoadScene(mainSceneName);
    }
}
