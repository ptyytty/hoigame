// Scripts/Visual/HeroVisualBinder.cs
using System.Collections.Generic;
using UnityEngine;

namespace Game.Visual
{
    /// <summary>
    /// 파티 슬롯에 영웅 프리팹을 스폰하고
    /// 공통 컨트롤러(AOC)에서 Attack_Base를 영웅 전용으로 교체
    /// + Idle/Run/Hit도 필요 시 교체
    /// + 이동/공격/피격 재생 API 제공
    /// </summary>
    public class HeroVisualBinder : MonoBehaviour
    {
        public GameObject Instance => _instance; // 생성된 영웅 인스턴스 접근자 제공

        [SerializeField] private Transform spawnRoot; // 캐릭터가 놓일 부모(파티 슬롯)
        [SerializeField] private HeroDefinition heroDef;

        private GameObject _instance;
        private Animator _anim;
        private AnimatorOverrideController _aoc;

        // 파라미터 해시(모바일 최적화)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        /// <summary>
        /// [역할] 외부에서 영웅 정의를 주입
        /// </summary>
        public void SetHeroDefinition(HeroDefinition def) => heroDef = def;

        /// <summary>
        /// [역할] 프리팹을 인스턴스화하고 Animator/AOC를 준비
        /// </summary>
        public void SpawnAndBind()
        {
            if (!heroDef || !heroDef.characterPrefab || !heroDef.baseController)
            {
                Debug.LogError("[HeroVisualBinder] HeroDefinition 설정 오류");
                return;
            }

            // 프리팹 생성
            _instance = Instantiate(heroDef.characterPrefab, spawnRoot ? spawnRoot : transform);
            var t = _instance.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.Euler(0f, -90f, 0f); // ← 여기!
            t.localScale = Vector3.one;
            _instance.name = $"{heroDef.name}_Instance";

            // Animator 확보
            _anim = _instance.GetComponent<Animator>();
            if (!_anim) _anim = _instance.AddComponent<Animator>();
            if (heroDef.avatar) _anim.avatar = heroDef.avatar;

            // 공통 컨트롤러를 AOC로 감싼 뒤 적용
            _aoc = new AnimatorOverrideController(heroDef.baseController);
            _anim.runtimeAnimatorController = _aoc;

            // 공격/공통 상태 오버라이드 실제 반영
            ApplyOverrides();

            // 모바일 최적화
            _anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms; // 오프스크린 비용 절감
            _anim.applyRootMotion = false; // 전투는 보통 Off (특정 스킬만 On)
        }

        /// <summary>
        /// [역할] AOC Override 테이블을 채워서
        /// Attack_Base → 영웅 전용 공격으로 교체하고,
        /// Idle/Run/Hit도 있으면 교체.
        /// </summary>
        private void ApplyOverrides()
        {
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            _aoc.GetOverrides(pairs);

            for (int i = 0; i < pairs.Count; i++)
            {
                var original = pairs[i].Key;   // 컨트롤러가 참조하는 원본(베이스) 클립
                var replacement = pairs[i].Value; // 현재 할당된(초기엔 Base)

                if (!original) continue;

                // ★ 핵심: Attack_Base 슬롯만 영웅 전용으로 교체
                if (heroDef.overrideAttack && original.name == "Attack_Base")
                    replacement = heroDef.overrideAttack;

                // 선택: 공통 기본 동작도 개별 교체 가능
                else if (heroDef.idleOverride && original.name == "Idle_Base")
                    replacement = heroDef.idleOverride;
                else if (heroDef.runOverride && original.name == "Run_Base")
                    replacement = heroDef.runOverride;
                else if (heroDef.hitOverride && original.name == "Hit_Base")
                    replacement = heroDef.hitOverride;

                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
            }

            _aoc.ApplyOverrides(pairs);
        }

        /// <summary>
        /// [역할] 이동 블렌딩 제어(0=Idle, 1=Run)
        /// </summary>
        public void SetMoveSpeed01(float speed01)
        {
            if (!_anim) return;
            _anim.SetFloat(SpeedHash, Mathf.Clamp01(speed01));
        }

        /// <summary>
        /// [역할] 공격 재생(영웅 전용 공격 클립이 발동)
        /// </summary>
        public void PlayAttack()
        {
            if (!_anim) return;
            _anim.ResetTrigger(AttackHash);
            _anim.SetTrigger(AttackHash);
        }

        /// <summary>
        /// [역할] 피격 반응 재생
        /// </summary>
        public void PlayHit()
        {
            if (!_anim) return;
            _anim.ResetTrigger(HitHash);
            _anim.SetTrigger(HitHash);
        }

        /// <summary>
        /// [역할] 생성된 비주얼 인스턴스 정리(씬 이동/교체 시)
        /// </summary>
        public void DisposeInstance()
        {
            if (_instance) Destroy(_instance);
            _instance = null;
            _anim = null;
            _aoc = null;
        }
    }
}
