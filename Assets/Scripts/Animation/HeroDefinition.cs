// Scripts/Visual/HeroDefinition.cs
using UnityEngine;

namespace Game.Visual
{
    /// <summary>
    /// 영웅 비주얼/애니메이션 정의를 담는 SO
    /// - 어떤 프리팹을 쓸지
    /// - 어떤 공통 컨트롤러를 쓸지
    /// - 공격(필수), 기타 공통 동작(선택) 오버라이드
    /// </summary>
    [CreateAssetMenu(fileName = "HeroDefinition", menuName = "Game/Hero Definition")]
    public class HeroDefinition : ScriptableObject
    {
        [Header("Prefab & Animator")]
        public GameObject characterPrefab;                // 이 영웅의 3D 프리팹(Animator 포함 권장)
        public RuntimeAnimatorController baseController;  // Common_Humanoid.controller

        [Header("Per-Hero Attack (필수)")]
        public AnimationClip overrideAttack;              // 이 영웅 전용 공격 클립

        [Header("Optional Common Overrides")]
        public AnimationClip idleOverride;                // 필요 시 대기 교체
        public AnimationClip runOverride;                 // 필요 시 달리기 교체
        public AnimationClip hitOverride;                 // 필요 시 피격 교체

        [Header("Identity")]
        public int jobId; // 파티/세이브의 id_job과 1:1 매칭에 사용

        public Avatar avatar; // 프리팹에 Avatar가 있으면 비워도 됨
    }
}
