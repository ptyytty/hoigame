using UnityEngine;
using TMPro;
using System.Text;

public class EquipInfoBox : MonoBehaviour
{
    public static EquipInfoBox Instance { get; private set; } // 역할: 화면 내 단일 접근 포인트

    [Header("Assign In Inspector")]
    [SerializeField] private GameObject panelRoot;   // 역할: Hero Info 패널의 루트(여기를 켜고/끈다)
    [SerializeField] private TMP_Text effectsText;   // 역할: 장비 효과 출력 대상 TMP_Text

    private void Awake()
    {
        // 역할: 싱글턴 초기화
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        // 역할: 시작 시 패널은 꺼둔다
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        // 역할: 선택/장비 변경 이벤트 구독(패널 토글·텍스트 갱신 트리거)
        SelectionEvents.OnHeroSelected     += OnHeroSelected;
        SelectionEvents.OnHeroEquipChanged += OnHeroEquipChanged;
    }

    private void OnDisable()
    {
        // 역할: 이벤트 구독 해제(메모리 누수 방지)
        SelectionEvents.OnHeroSelected     -= OnHeroSelected;
        SelectionEvents.OnHeroEquipChanged -= OnHeroEquipChanged;
    }

    /// <summary>
    /// 역할: 이벤트 핸들러—영웅이 선택되면 패널을 켜고 내용 렌더,
    ///       선택 해제(null)면 패널을 끔.
    /// </summary>
    private void OnHeroSelected(Job hero)
    {
        if (hero == null)
        {
            Close(); // 외부 클릭 등으로 선택 해제 → 패널 OFF
            return;
        }

        Open();     // 패널 ON
        Show(hero); // 내용 렌더
    }

    /// <summary>
    /// 역할: 이벤트 핸들러—장비가 바뀌면(선택 유지 중) 즉시 내용만 갱신
    /// </summary>
    private void OnHeroEquipChanged(Job hero)
    {
        if (hero == null) return;
        if (panelRoot != null && panelRoot.activeSelf == false) return; // 패널이 꺼져있으면 무시
        Show(hero);
    }

    /// <summary>
    /// 역할: 패널 ON (안전 가드)
    /// </summary>
    private void Open()
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);
    }

    /// <summary>
    /// 역할: 패널 OFF + 텍스트 정리
    /// </summary>
    private void Close()
    {
        Clear();
        if (panelRoot != null && panelRoot.activeSelf)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// 역할: 현재 선택 영웅의 장착 아이템 효과를 텍스트로 출력
    /// </summary>
    public void Show(Job hero)
    {
        if (effectsText == null)
        {
            Debug.LogWarning("[EquipInfoBox] effectsText가 할당되지 않았습니다.");
            return;
        }

        if (hero == null || hero.equippedItem == null)
        {
            effectsText.text = "";
            return;
        }

        var item = hero.equippedItem;
        var sb = new StringBuilder();

        // 장비 지속효과만 출력 (effects: List<ItemEffectSpec>)
        if (item.effects != null && item.effects.Count > 0)
        {
            for (int i = 0; i < item.effects.Count; i++)
            {
                var eff = item.effects[i];
                if (!eff.persistent) continue;

                switch (eff.op)
                {
                    case EffectOp.AbilityMod:
                        {
                            var label = GetStatLabel(eff.stat);
                            var sign  = eff.value >= 0 ? "+" : "";
                            sb.AppendLine($"{label} {sign}{eff.value}");
                            break;
                        }
                    case EffectOp.Special:
                        {
                            sb.AppendLine(SpecialKeyToText(eff.specialKey));
                            break;
                        }
                    default:
                        break;
                }
            }
        }

        effectsText.text = sb.ToString();
    }

    /// <summary>
    /// 역할: 텍스트 초기화(패널은 건드리지 않음)
    /// </summary>
    public void Clear()
    {
        if (effectsText != null) effectsText.text = "";
    }

    // ---- 표시 문자열 매핑 ----
    private string SpecialKeyToText(string key)
    {
        switch (key)
        {
            case "Immune_Stun":  return "기절 면역";
            case "Immune_Bleed": return "출혈 면역";
            case "Immune_Burn":  return "화상 면역";
            case "Immune_Faint": return "기절 면역";
            default:             return string.IsNullOrEmpty(key) ? "특수 효과" : key;
        }
    }

    private string GetStatLabel(BuffType stat)
    {
        switch (stat)
        {
            case BuffType.Defense:    return "방어";
            case BuffType.Resistance: return "저항";
            case BuffType.Speed:      return "민첩";
            case BuffType.Hit:        return "명중";
            case BuffType.Damage:     return "공격";
            case BuffType.Heal:       return "회복량";
            default:                  return stat.ToString();
        }
    }
}
