using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


/// 공통 리스트 UI 베이스(모바일 빌드 호환)
/// - 버튼 생성/선택 이미지 전환(자식 Image 포함)
/// - SpriteSwap 덮어쓰기 방지
/// - Employment/ListUpManager 호환 유틸 포함

// Hero 리스트 추상 클래스
// 리스트 생성 클래스
public abstract class ListUIBase<TData> : MonoBehaviour
{
    [SerializeField] protected Button buttonPrefab;
    [SerializeField] protected Transform contentParent;
    [SerializeField] protected Sprite globalDefaultSprite;
    [SerializeField] protected Sprite globalSelectedSprite;
    [SerializeField] private string targetImagePath;

    [Header("Hero Info")]
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private TMP_Text heroHp;
    [SerializeField] private TMP_Text heroDef;
    [SerializeField] private TMP_Text heroRes;
    [SerializeField] private TMP_Text heroSpd;
    [SerializeField] private TMP_Text heroHit;

    protected Button currentSelect;
    protected List<Button> buttons = new();
    protected List<TData> dataList = new();

    UIClickResetHandler clickHandler;

    // 버튼별 표시 Image 캐싱(자식 구조가 바뀌어도 안정)
    readonly Dictionary<Button, Image> _btnImages = new();

    // ===== Lifecycle =====
    protected virtual void OnEnable()
    {
        clickHandler = FindObjectOfType<UIClickResetHandler>();
        if (clickHandler != null) clickHandler.RegisterResetCallback(ResetSelectedButton);
    }

    protected virtual void OnDisable()
    {
        if (clickHandler != null) clickHandler.UnregisterResetCallback(ResetSelectedButton);
    }

    // ===== Helpers for children (Employment/ListUpManager에서 사용) =====
    protected bool ValidateListBinding(string tag)
    {
        if (!buttonPrefab) { Debug.LogError($"[{tag}] buttonPrefab is NULL", this); return false; }
        if (!contentParent) { Debug.LogError($"[{tag}] contentParent is NULL", this); return false; }
        if (!globalDefaultSprite) { Debug.LogWarning($"[{tag}] globalDefaultSprite is NULL (스프라이트 미지정)", this); }
        if (!globalSelectedSprite) { Debug.LogWarning($"[{tag}] globalSelectedSprite is NULL (스프라이트 미지정)", this); }
        return true;
    }

    protected static Transform SafeFind(Transform root, string path, string tag)
    {
        if (!root) return null;
        var t = root.Find(path);
        if (!t) Debug.LogWarning($"[{tag}] child not found: {path}");
        return t;
    }

    /// 버튼을 만들고 라벨/이미지/클릭까지 연결(권장 생성기)
    protected Button SafeCreateButton(TData data, string callerTag)
    {
        if (!buttonPrefab)
        {
            Debug.LogError($"[{callerTag}] buttonPrefab == NULL → Instantiate 불가", this);
            return null;
        }
        if (!contentParent)
        {
            Debug.LogError($"[{callerTag}] contentParent == NULL → Instantiate 불가", this);
            return null;
        }
        if (!buttonPrefab.GetComponent<Button>())
        {
            Debug.LogError($"[{callerTag}] buttonPrefab 루트에 Button 컴포넌트가 없습니다. 프리팹: {buttonPrefab.name}", buttonPrefab);
            return null;
        }

        Debug.Log($"[{callerTag}] Instantiate 시도 → prefab={buttonPrefab.name}, parent={contentParent.name}", this);

        var button = Instantiate(buttonPrefab, contentParent);
        if (!button)
        {
            Debug.LogError($"[{callerTag}] Instantiate 결과가 NULL", this);
            return null;
        }

        Debug.Log($"[{callerTag}] Instantiate 성공 → {button.name} (parent={button.transform.parent?.name})", button);

        buttons.Add(button);
        dataList.Add(data);

        // 권장: ColorTint (SpriteSwap과 충돌 줄임)
        button.transition = Selectable.Transition.ColorTint;

        if (globalDefaultSprite)
            SetButtonSprite(button, globalDefaultSprite);

        SetLabel(button, data);

        var capturedBtn = button;
        var capturedData = data;

        button.onClick.AddListener(() =>
        {
            // ★★★ 전역 리셋 억제: 같은 프레임 리셋 차단
            if (clickHandler != null) clickHandler.SuppressOnce();

            if (currentSelect == capturedBtn) return;

            if (currentSelect && globalDefaultSprite)
                SetButtonSprite(currentSelect, globalDefaultSprite);

            currentSelect = capturedBtn;

            if (globalSelectedSprite)
                SetButtonSprite(capturedBtn, globalSelectedSprite);

            OnSelected(capturedData);
        });

        return button;
    }

    // (구) CreateButton을 쓰는 기존 코드 호환
    protected void CreateButton(TData data) => SafeCreateButton(data, nameof(ListUIBase<TData>));

    // ===== Image switching core =====
    static Image GetButtonImage(Button btn)
    {
        if (!btn) return null;
        if (btn.targetGraphic is Image tg) return tg;         // 1) TargetGraphic
        var self = btn.GetComponent<Image>(); if (self) return self; // 2) 자기 Image
        return btn.GetComponentInChildren<Image>(true);       // 3) 자식 Image
    }

    static void ForceApplySprite(Button btn, Sprite sprite)
    {
        if (!btn || !sprite) return;

        if (btn.targetGraphic is Image tg) tg.sprite = sprite;

        // SpriteSwap을 쓰는 프리팹과의 충돌 방지(상태 스프라이트 동기화)
        if (btn.transition == Selectable.Transition.SpriteSwap)
        {
            var st = btn.spriteState;
            st.highlightedSprite = sprite;
            st.pressedSprite = sprite;
            st.selectedSprite = sprite;
            btn.spriteState = st;
        }
    }

    static Image FindDisplayImage(Button btn)
    {
        if (!btn) return null;
        if (btn.targetGraphic is Image tg) return tg;
        var self = btn.GetComponent<Image>(); if (self) return self;
        // 자식 중 "Icon", "Image" 같은 Image를 우선 탐색
        foreach (var img in btn.GetComponentsInChildren<Image>(true))
        {
            if (!img) continue;
            // 필요하면 이름 기준 필터 강화 가능
            return img;
        }
        return null;
    }

    void SetButtonSprite(Button btn, Sprite sprite)
    {
        if (!btn || !sprite) return;
        if (!_btnImages.TryGetValue(btn, out var img) || img == null)
        {
            img = FindDisplayImage(btn);
            _btnImages[btn] = img;
        }
        if (img) img.sprite = sprite;

        // SpriteSwap 사용 시 상태 스프라이트도 동기화(덮어쓰기 방지)
        if (btn.transition == Selectable.Transition.SpriteSwap)
        {
            var st = btn.spriteState;
            st.highlightedSprite = sprite;
            st.pressedSprite = sprite;
            st.selectedSprite = sprite;
            btn.spriteState = st;
        }
    }

    // ===== List management =====
    protected void ClearList()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);
        buttons.Clear();
        dataList.Clear();
        currentSelect = null;
    }

    protected void ResetSelectedButton()
    {
        if (!currentSelect) return;
        if (globalDefaultSprite)
            SetButtonSprite(currentSelect, globalDefaultSprite);
        currentSelect = null;
    }

    protected void SetAllButtonsInteractable(bool state)
    {
        foreach (var b in buttons) if (b) b.interactable = state;
    }

    // ===== Info panel =====
    public void ShowHeroInfo(Job hero)
    {
        if (heroName) heroName.text = $"{hero.name_job}";
        if (heroHp) heroHp.text = $"{hero.hp}";
        if (heroDef) heroDef.text = $"{hero.def}";
        if (heroRes) heroRes.text = $"{hero.res}";
        if (heroSpd) heroSpd.text = $"{hero.spd}";
        if (heroHit) heroHit.text = $"{hero.hit}";
    }

    // ===== Hooks =====
    protected abstract void SetLabel(Button button, TData data);
    protected abstract void OnSelected(TData data);
    protected abstract void LoadList();
}
