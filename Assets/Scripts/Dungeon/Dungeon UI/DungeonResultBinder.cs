using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DungeonResultBinder : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public Image portrait;
        public TMP_Text displayName;   // 수정 가능한 이름
        
        public TMP_Text className;   // 직업 이름
        public TMP_Text hpLabel;     // 예: "체력"
        public HealthBarUI hpBar;
        public TMP_Text hpValue;     // 예: "32 / 45"
        public TMP_Text levelText;
        public TMP_Text expLabel;    // 예: "경험치"
        public Image expFill;    // 예: "+120" (이번 던전에서 얻은 값)

    }

    [Header("4 Slots (index 0~3)")]
    [SerializeField] private List<Slot> slots = new(4);

    // ====== 외부에서 채워줄 데이터 DTO ======
    [Serializable]
    public struct HeroResult
    {
        public Sprite portrait;     // 초상
        public string editName;     // 표시명(수정 가능 이름)
        public string jobName;      // 직업명

        public int levelNow;        // 현재 레벨 (Max면 별도 처리)
        public int hpNow;           // 현재 HP
        public int hpMax;           // 최대 HP

        public float expProgress;   // 0~1, 현재 레벨 기준 경험치 진행도
        public bool leveledUp;      // 이번 던전에서 레벨업 했는지(스타일 강조 용)

        public static HeroResult Empty() => new HeroResult
        {
            portrait = null, editName = "-", jobName = "-",
            levelNow = 1, hpNow = 0, hpMax = 1, expProgress = 0f, leveledUp = false
        };
    }

    // 결과 패널을 비움(빈 슬롯 상태).
    public void ClearAll()
    {
        for (int i = 0; i < slots.Count; i++)
            BindSlot(i, HeroResult.Empty());
    }

    // 0~3 인덱스 순서로 결과를 바인딩.
    public void Bind(IReadOnlyList<HeroResult> results)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var r = (results != null && i < results.Count) ? results[i] : HeroResult.Empty();
            BindSlot(i, r);
        }
    }

    // 단일 슬롯에 HeroResult를 반영.
    private void BindSlot(int index, HeroResult r)
    {
        if (index < 0 || index >= slots.Count) return;
        var s = slots[index];

        // 초상
        if (s.portrait)
        {
            s.portrait.enabled = (r.portrait != null);
            s.portrait.sprite  = r.portrait;
        }

        // 이름/직업
        if (s.displayName) s.displayName.text = string.IsNullOrEmpty(r.editName) ? "-" : r.editName;
        if (s.className)   s.className.text   = string.IsNullOrEmpty(r.jobName) ? "-" : r.jobName;

        // 레벨
        if (s.levelText)
        {
            // Max 처리: 프로젝트 규칙에 맞게 수정 가능
            string lv = (r.levelNow >= 5) ? "Max" : $"Lv. {Mathf.Max(1, r.levelNow)}";
            s.levelText.text = lv;

            // 레벨업 강조(선택): 레벨업 했으면 색/굵기 등으로 표시
            if (r.leveledUp) s.levelText.fontStyle = FontStyles.Bold;
            else             s.levelText.fontStyle = FontStyles.Normal;
        }

        // 체력 바/수치
        int hpNow = Mathf.Max(0, r.hpNow);
        int hpMax = Mathf.Max(1, r.hpMax);
        if (s.hpBar)   s.hpBar.Set(hpNow, hpMax);
        if (s.hpValue) s.hpValue.text = $"{hpNow} / {hpMax}"; // hpValue를 쓰지 않으면 인스펙터에서 비워두면 됨

        // 경험치 바
        if (s.expFill)
        {
            // Image Type=Filled(Horizontal) 권장
            s.expFill.fillAmount = Mathf.Clamp01(r.expProgress);
        }
    }

    // --- 네이밍 유틸(원하면 프로젝트 룰에 맞게 교체) ---
    private static string GetClassLabelFromHeroName(string heroName)
    {
        // 간단 예시: 이름에 "기사"가 포함되면 "기사"
        if (string.IsNullOrEmpty(heroName)) return "직업";
        if (heroName.Contains("기사")) return "기사";
        // 필요 시 다른 규칙 추가
        return "직업";
    }

    //빈 이름 방어 로직.
    private static string SafeName(string n) => string.IsNullOrEmpty(n) ? "이름없음" : n;
}
