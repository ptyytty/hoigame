using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// PartyManager
/// Party 이동 제어, 전투 UI 제어, 보상 UI 제어, 퀘스트 관리
/// </summary>

// === 던전 퀘스트 런타임 ===
[Serializable]
public class DungeonQuestRuntime
{
    public string questId;
    public string displayName;
    public bool isCompleted;
    public int targetCount;   // 필요 시(예: 전투 N회, 방 N칸) 목표치
    public int current;       // 진행도

    // 진행 1단계 증가 (전투 1회 승리 등)
    public void Tick(int amount = 1)
    {
        current = Mathf.Max(0, current + amount);
        if (targetCount > 0) isCompleted = current >= targetCount;
    }
}

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager instance { get; private set; }

    [Header("Inventory")]
    [SerializeField] private DungeonInventory dungeonInventory;
    [SerializeField] private DungeonInventoryBinder inventoryBinder;
    public DungeonInventory DungeonInventory => dungeonInventory;

    [Header("Dungeon UI")]
    public GameObject moveLeft;
    public GameObject moveRight;

    [SerializeField] private DungeonPartySpawner partyVisual; // 파티 애니메이션 제어 창구
    [SerializeField] private bool animateAllParty = true;           // true면 모든 슬롯에 동일 적용

    [Header("Battle UI")]
    public GameObject battleUI;

    [Header("Quest HUD")]
    [SerializeField] private TMP_Text questText;     // 좌상단 퀘스트 텍스트
    [SerializeField] private Toggle questToggle;     // 완료 시 체크
    [SerializeField] private Image questCheckmark;   // 체크마크 이미지
    [SerializeField] private Color questIncompleteColor = Color.white;  // 미완료 색
    [SerializeField] private Color questCompleteColor = Color.green;  // 완료 색
    [SerializeField] private string currentDungeonId = "dungeon_Oratio"; // 씬/던전 식별자

    private int _totalBattlesInThisDungeon;     // 던전 전투 수
    private int _battlesCleared;                // 전투 완료 수

    private DungeonQuestRuntime _quest;  // 현재 던전 퀘스트 상태
    private bool _questRewardGiven = false; // 퀘스트 완료 보상 지급 여부

    [Header("Reward UI")]
    [SerializeField] private GameObject rewardToastPanel;  // 보상 패널
    [SerializeField] private CanvasGroup rewardToastGroup; // 알파 페이드 대상
    [SerializeField] private Transform rewardToastScaleRoot; // 스캐일 애니메이션
    [SerializeField] private RewardPanel rewardPanelBinder; // 텍스트 적용

    [Header("Reward Settings (per battle)")]
    [SerializeField] private float toastShowSeconds = 1.2f; // 화면 유지 시간
    [SerializeField] private float tweenIn = 0.25f;         // 등장 시간
    [SerializeField] private float tweenOut = 0.25f;        // 퇴장 시간
    [SerializeField] private Ease easeIn = Ease.OutBack;    // 가속 곡선
    [SerializeField] private Ease easeOut = Ease.InSine;

    [Header("Dungeon Clear UI")]
    [SerializeField] private GameObject dungeonClearPanel;
    [SerializeField] private CanvasGroup dungeonClearGroup;    // 페이드용
    [SerializeField] private Transform dungeonClearScaleRoot;
    [SerializeField] private float clearFadeIn = 0.25f;
    [SerializeField] private float clearTweenIn = 0.25f;
    [SerializeField] private float clearStartScale = 0.9f;
    [SerializeField] private Ease clearEaseIn = Ease.OutBack;

    [SerializeField] private DungeonResultBinder resultBinder; // ✅ 결과 패널 바인더(아래 2번)
    private readonly Dictionary<string, HeroEntrySnapshot> _entry = new(); // key = instanceId

    [Serializable]
    private class HeroEntrySnapshot     // 파티 정보
    {
        public string instanceId;
        public int startLevel;
        public int startExp;
        public int startHp;
        public int startMaxHp;
    }
    [SerializeField] private Button rewardButton;
    [SerializeField] private ResultRewardGrid resultRewardGrid;     // 보상

    private bool _dungeonClearShown = false;                        // 중복 방지

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void Start()
    {
        // 자동 찾기
        if (!dungeonInventory) dungeonInventory = FindObjectOfType<DungeonInventory>(true);
        if (!partyVisual) partyVisual = FindObjectOfType<DungeonPartySpawner>(true);

        // PartyBridge → DungeonInventory 스냅샷 1회 적용 (인벤토리)
        var snap = PartyBridge.Instance?.dungeonLoadoutSnapshot;
        if (dungeonInventory && snap != null && snap.Count > 0)
        {
            dungeonInventory.ApplySnapshot(snap);              // 6칸 재초기화 후 채움 :contentReference[oaicite:10]{index=10}
            PartyBridge.Instance.dungeonLoadoutSnapshot = null;
        }

        StartCoroutine(CoLateCountSpawners());  // 스포너 배치
        _battlesCleared = 0;                    // 완료 전투 횟수 초기화

        InitQuestOnEnter();                     // 던전 입장 시 퀘스트 HUD 세팅
        _questRewardGiven = false;              // 퀘스트 상태 초기화

        // 2) 바인더 연결(이벤트 구독 + 즉시 그리기)
        if (inventoryBinder && dungeonInventory)
            inventoryBinder.Bind(dungeonInventory);

        CaptureDungeonEntrySnapshot();      // 입장 시 현재 파티 정보 저장
    }

    private IEnumerator CoLateCountSpawners()
    {
        // 스포너 배치가 끝난 다음 프레임에 전투 수를 계산
        yield return null; // 한 프레임 대기
        _totalBattlesInThisDungeon = FindObjectsOfType<EnemySpawner>(true).Length;

        if (_quest != null && _quest.questId == "all_combat_completed")
        {
            _quest.targetCount = Mathf.Max(1, _totalBattlesInThisDungeon); // 실제 전투 수로 갱신
            _quest.current = Mathf.Min(_quest.current, _quest.targetCount); // 진행도 상한 보정
        }
        // 필요시 퀘스트 HUD 리프레시
        UpdateQuestHud();
    }

    // =========== 던전 입장 직후 파티 정보 저장 =============
    private void CaptureDungeonEntrySnapshot()
    {
        _entry.Clear();

        var party = PartyBridge.Instance?.ActiveParty;
        if (party == null || party.Count == 0) return;

        foreach (var h in party)        // 파티 영웅 정보 저장
        {
            if (h == null) continue;
            var key = string.IsNullOrEmpty(h.instanceId) ? h.id_job.ToString() : h.instanceId;  // null = id_job    !null = instanceId

            _entry[key] = new HeroEntrySnapshot
            {
                instanceId = key,
                startLevel = h.level,
                startExp = h.exp,
                startHp = h.hp,
                startMaxHp = Mathf.Max(1, h.maxHp),
            };
        }
    }

    void OnEnable() { EnemySpawner.OnBattleStart += HandleBattleStart; }
    void OnDisable() { EnemySpawner.OnBattleStart -= HandleBattleStart; }

    public Transform partyTransform;
    public float moveSpeed = 50f;  // 이동 속도
    private bool isMoving = false;
    private bool isInFrontRow = true; // 앞열인지 뒷열인지 구분하는 변수

    // 이동 애니메이션
    private void SetRunAnim(bool run)
    {
        // [역할] 파티 애니메이션 Speed(0/1) 제어
        if (!partyVisual) return;

        // 슬롯 개수만큼 안전하게 반복
        for (int i = 0; i < partyVisual.slots.Count; i++)
        {
            if (!animateAllParty && i != 0) break; // 필요 시 0번만 제어

            var binder = partyVisual.slots[i].binder;
            if (binder) binder.SetMoveSpeed01(run ? 1f : 0f);   // Speed 파라미터 세팅(Idle/Run 전환)
        }
    }

    public MoveDirection currentDir { get; private set; }

    // ==================== 던전 이동 =========================
    void Update()
    {
        if (isMoving)
        {
            Vector3 dir = GetMoveVector(currentDir);
            partyTransform.Translate(dir * moveSpeed * Time.deltaTime);
        }
    }

    // 버튼 연결
    public void StartMove(int dir)
    {
        currentDir = (MoveDirection)dir;  //정수 -> 열거형 캐스팅
        isMoving = true;

        SetRunAnim(true);

        if (currentDir == MoveDirection.Left && !isInFrontRow)
        {
            transform.Rotate(0, -180, 0);
            isInFrontRow = true;
        }
        else if (currentDir == MoveDirection.Right && isInFrontRow)
        {
            transform.Rotate(0, 180, 0);
            isInFrontRow = false;
        }
    }

    public void StopMove()      // EnentTrigger 연결
    {
        isMoving = false; 
        SetRunAnim(false);      // 버튼에서 손 떼면 Idle로 복귀
    }    
    public void StopMoveHard() 
    {
        isMoving = false; 
        SetRunAnim(false); // 강제 정지 시에도 Idle
    }

    public void ResumeMove()
    {
        isMoving = true;
        SetRunAnim(true);  // 필요 시 재개 즉시 Run
    }
    
    public void ResumeMoveIfNeeded() { /* 조건부 재개가 필요하면 여기에 로직 */ }

    Vector3 GetMoveVector(MoveDirection dir)
    {
        if (isInFrontRow)
            return dir == MoveDirection.Left ? Vector3.back : Vector3.forward;
        else
            return dir == MoveDirection.Left ? Vector3.forward : Vector3.back;
    }

    // ─────────────────────────────────────
    // 전투 시작 => 이동 정지, UI 전환
    void HandleBattleStart(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        StopMoveHard();        // 파티 즉시 정지

        // UI 전환
        moveLeft.SetActive(false);
        moveRight.SetActive(false);
        InteractableManager.instance?.EnterBattleMode();        // 전투 시작 -> 상호작용 중지   
        battleUI.SetActive(true);

        if (rewardButton) rewardButton.interactable = false;    // 전체 보상 창 상호작용 불가
    }

    // 전투 종료 => UI 전환
    public void ShowDungeonUIAfterBattle()
    {
        if (battleUI) battleUI.SetActive(false);

        if (moveLeft) moveLeft.SetActive(true);
        if (moveRight) moveRight.SetActive(true);
        InteractableManager.instance?.ExitBattleMode();         // 전투 종료 -> 상호작용 복구

        if (rewardButton) rewardButton.interactable = true;

        SetRunAnim(false);      // Idle 복귀
    }
    // ─────────────────────────────────────

    // 보상 토스트만 띄우는 공개 메서드 (BattleManager, InteractableManager에서 호출)
    public void ShowBattleRewardToast(SoulType soulType, int soulAmount, int coins)
    {
        // 내용 바인딩
        rewardPanelBinder.Bind(soulType, soulAmount, coins);

        if (!rewardToastScaleRoot) rewardToastScaleRoot = rewardToastPanel.transform;
        rewardToastPanel.SetActive(true);
        if (rewardToastGroup) rewardToastGroup.alpha = 0f;
        rewardToastScaleRoot.localScale = Vector3.one * 0.9f;

        var seq = DOTween.Sequence();
        if (rewardToastGroup) seq.Join(rewardToastGroup.DOFade(1f, tweenIn));
        seq.Join(rewardToastScaleRoot.DOScale(1f, tweenIn).SetEase(easeIn));
        seq.AppendInterval(toastShowSeconds);
        if (rewardToastGroup) seq.Append(rewardToastGroup.DOFade(0f, tweenOut));
        seq.Join(rewardToastScaleRoot.DOScale(0.9f, tweenOut).SetEase(easeOut));
        seq.OnComplete(() => { if (rewardToastPanel) rewardToastPanel.SetActive(false); });
    }

    // =============== 퀘스트 진행 ==================
    // 던전 입장 시 1회 호출: 퀘스트 선택 및 HUD 표기
    private void InitQuestOnEnter()
    {
        // DB에서 현재 던전에 유효한 퀘스트 목록 조회
        var candidates = QuestDB.questLists.FindAll(q => q.dungeonId != null && q.dungeonId.Contains(currentDungeonId));
        // 없으면 전체에서 아무거나
        if (candidates == null || candidates.Count == 0) candidates = QuestDB.questLists;

        // 무작위 1개 선택
        var pick = candidates[UnityEngine.Random.Range(0, Mathf.Max(1, candidates.Count))];

        // 런타임 상태 구성
        _quest = new DungeonQuestRuntime
        {
            questId = pick.questId,
            displayName = pick.questname,
            isCompleted = false,
            targetCount = 0, // 퀘스트 클리어 수치
            current = 0
        };

        // HUD 반영
        UpdateQuestHud();
    }

    // HUD에 현재 퀘스트 텍스트/체크 상태 반영
    private void UpdateQuestHud()
    {
        // 텍스트
        if (questText)
        {
            questText.text = $"클리어 조건: {_quest?.displayName ?? "-"}";
            questText.color = (_quest != null && _quest.isCompleted) ? questCompleteColor : questIncompleteColor;
        }

        // 클리어 토글
        if (questToggle)
        {
            questToggle.isOn = _quest != null && _quest.isCompleted;
            questToggle.interactable = false; // 유저 클릭 방지(표시용)
        }

        // 클리어 체크마크
        if (questCheckmark)
            questCheckmark.enabled = _quest != null && _quest.isCompleted;
    }

    // 전투에서 승리했을 때 BattleManager가 호출(퀘스트 진행 갱신용)
    public void NotifyBattleWon()
    {
        if (_quest == null) return;

        bool wasCompleted = _quest.isCompleted;

        GrantExpToActiveParty(1);   // 전투 승리 보상(+1)

        // 던전 전투 클리어 카운트
        _battlesCleared = Mathf.Clamp(_battlesCleared + 1, 0, Mathf.Max(1, _totalBattlesInThisDungeon));

        // '모든 전투 완료' 타입이라면 진행도 갱신
        if (_quest.questId == "all_combat_completed" && _quest.targetCount > 0)
        {
            _quest.current = _battlesCleared;
            if (_quest.current >= _quest.targetCount)
                _quest.isCompleted = true;
        }

        UpdateQuestHud();

        // ✅ 이 자리에서 완료되었다면 완료 보상(+3)까지 즉시 처리
        if (!wasCompleted && _quest.isCompleted)
            CompleteQuestIfNeeded();
    }

    // 퀘스트 완료 상태면 완료 보상 지급
    private void CompleteQuestIfNeeded()
    {
        if (_quest == null) return;
        if (!_quest.isCompleted) return;

        // 중복 지급 방지
        if (!_questRewardGiven)
        {
            GrantExpToActiveParty(3);   // ✅ 퀘스트 완료 보상 즉시 지급
            _questRewardGiven = true;
        }

        UpdateQuestHud();
        ShowDungeonClearUIOnce();       // 결과창/클리어 처리
    }

    // 던전 목표 달성 시 외부(보스 처치/맵 90% 탐험 등)에서 호출
    public void SetQuestComplete()
    {
        if (_quest == null) return;

        bool wasCompleted = _quest.isCompleted;
        _quest.isCompleted = true;

        // ✅ 완료 보상(+3)과 결과창을 한 곳에서 처리(중복 지급 방지 포함)
        CompleteQuestIfNeeded();
    }

    //=============== 던전 클리어 UI =============
    private void ShowDungeonClearUIOnce()
    {
        if (_dungeonClearShown) return;
        _dungeonClearShown = true;

        // 입력/이동/전투 UI 정리
        StopMoveHard();
        if (battleUI) battleUI.SetActive(false);
        if (moveLeft) moveLeft.SetActive(false);
        if (moveRight) moveRight.SetActive(false);

        BindResultPanel();

        PlayDungeonClearAppear();
    }

    // 보상창 버튼 토글 기능
    public void ToggleDungeonClearUI()
    {
        if (dungeonClearPanel.activeSelf)
        {
            HideDungeonClearUI();
        }
        else
        {
            ShowDungeonClearUI();
        }
    }

    // 파티의 현재 상태와 입장 시 상태 비교 후 전달
    private void BindResultPanel()
    {
        if (!resultBinder) return;

        var party = PartyBridge.Instance?.ActiveParty;
        if (party == null || party.Count == 0) { resultBinder.ClearAll(); return; }

        // 슬롯 순서를 파티 순서와 동일하게 전달
        var results = new List<DungeonResultBinder.HeroResult>(4);

        foreach (var h in party)
        {
            if (h == null)
            {
                results.Add(DungeonResultBinder.HeroResult.Empty());
                continue;
            }

            var key = string.IsNullOrEmpty(h.instanceId) ? h.id_job.ToString() : h.instanceId;
            _entry.TryGetValue(key, out var snap);

            int curHp = h.hp;
            int maxHp = Mathf.Max(1, h.maxHp);
            int curLv = h.level;
            int curExp = h.exp;

            int gotExp = (snap != null) ? Mathf.Max(0, curExp - snap.startExp) : 0;
            int hpDelta = (snap != null) ? (curHp - snap.startHp) : 0;


            results.Add(new DungeonResultBinder.HeroResult
            {
                portrait = h.portrait,
                editName = string.IsNullOrEmpty(h.displayName) ? h.name_job : h.displayName, // 표시명 우선
                jobName = h.name_job,
                levelNow = Mathf.Max(1, h.level),
                hpNow = h.hp,
                hpMax = Mathf.Max(1, h.maxHp),
                expProgress = SafeGetExpProgress(h), // 아래 보조 함수 참조
                leveledUp = (snap != null && h.level > snap.startLevel)
            });
        }

        resultBinder.Bind(results);
        resultRewardGrid?.RebindFromCurrentRun();
    }

    // ============ 경험치 ===========
    // 경험치 부여 및 UI 갱신
    public void GrantExpToActiveParty(int amount)
    {
        var party = PartyBridge.Instance?.ActiveParty;
        if (party == null || party.Count == 0) return;

        foreach (var hero in party)
        {
            if (hero == null) continue;
            hero.exp += Mathf.Max(0, amount);

            // 자동 레벨업
            if (hero.exp >= GameBalance.GetRequiredExpForLevel(hero.level))
            {
                hero.exp = 0;
                hero.level = Mathf.Min(hero.level + 1, 5);
            }
        }

        // ✅ UI 즉시 반영
        // 1) 정보창, 2) 결과창 둘 다 자동 갱신 가능
        if (resultBinder && dungeonClearPanel && dungeonClearPanel.activeSelf)
            BindResultPanel(); // 결과창이 열려 있으면 즉시 갱신
    }



    // 현재 레벨 기준 경험치 진행도 호출
    private float SafeGetExpProgress(Job h)
    {
        try
        {
            return h.GetExpProgress();
        }
        catch
        {
            return 0f;
        }
    }


    // UI 애니메이션
    private void PlayDungeonClearAppear()
    {
        if (!dungeonClearPanel)
        {
            Debug.LogWarning("[DungeonManager] dungeonClearPanel 미할당");
            return;
        }

        if (moveLeft) moveLeft.SetActive(false);
        if (moveRight) moveRight.SetActive(false);

        if (!dungeonClearScaleRoot) dungeonClearScaleRoot = dungeonClearPanel.transform;

        // 패널 활성화 & 초기 상태 세팅
        dungeonClearPanel.SetActive(true);

        // 기존 트윈 정리(겹침 방지). target을 명확히 지정하기 위해 각 컴포넌트로 Kill
        if (dungeonClearGroup) DOTween.Kill(dungeonClearGroup);
        if (dungeonClearScaleRoot) DOTween.Kill(dungeonClearScaleRoot);

        if (dungeonClearGroup) dungeonClearGroup.alpha = 0f;
        dungeonClearScaleRoot.localScale = Vector3.one * clearStartScale;

        // 등장 트윈(알파/스케일만, 유지)
        var seq = DOTween.Sequence();
        if (dungeonClearGroup) seq.Join(dungeonClearGroup.DOFade(1f, clearTweenIn));
        seq.Join(dungeonClearScaleRoot
            .DOScale(1f, clearTweenIn)
            .SetEase(clearEaseIn));
    }

    // 외부에서 패널 open
    public void ShowDungeonClearUI()
    {
        BindResultPanel();
        PlayDungeonClearAppear();
    }

    // 외부에서 패널 close
    public void HideDungeonClearUI()
    {
        if (!dungeonClearPanel) return;
        // 닫힐 때도 깔끔하게 트윈 정리
        if (dungeonClearGroup) DOTween.Kill(dungeonClearGroup);
        if (dungeonClearScaleRoot) DOTween.Kill(dungeonClearScaleRoot);
        dungeonClearPanel.SetActive(false);

        if (moveLeft) moveLeft.SetActive(true);
        if (moveRight) moveRight.SetActive(true);

        bool isBattleActive = battleUI && battleUI.activeSelf;
        if (!isBattleActive)
        {
            if (moveLeft) moveLeft.SetActive(true);
            if (moveRight) moveRight.SetActive(true);
        }

    }

}


// 이동 방향 열
public enum MoveDirection
{
    Left = 0,
    Right = 1
}
