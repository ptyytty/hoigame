// Scripts/Visual/HeroVisualBinder.cs
using System.Collections.Generic;
using Unity.VisualScripting;
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
        public GameObject Instance => _instance;

        [SerializeField] private Transform spawnRoot;
        [SerializeField] private HeroDefinition heroDef;

        private GameObject _instance;
        private Animator _anim;

        private static readonly int SpeedHash  = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HitHash    = Animator.StringToHash("Hit");

        // [역할] 외부에서 영웅 정의를 주입
        public void SetHeroDefinition(HeroDefinition def) => heroDef = def;

        // [역할] 프리팹을 스폰하고 CombatAnimator에 오버라이드 적용만 맡김
        public void SpawnAndBind()
        {
            if (!heroDef || !heroDef.characterPrefab || !heroDef.baseController)
            {
                Debug.LogError("[HeroVisualBinder] HeroDefinition 설정 오류");
                return;
            }

            // 1) 프리팹 생성 및 기본 트랜스폼 세팅
            _instance = Instantiate(heroDef.characterPrefab, spawnRoot ? spawnRoot : transform);
            var t = _instance.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.Euler(0f, -90f, 0f);
            t.localScale = Vector3.one;
            _instance.name = $"{heroDef.name}_Instance";

            // 2) CombatAnimator 확보 후, 이 스크립트에 '오버라이드 전담' 시킴
            var ca = _instance.GetComponentInChildren<CombatAnimator>() ?? _instance.AddComponent<CombatAnimator>();
            ca.ApplyHeroDefinition(heroDef); // ← 여기서 AOC 생성 + Attack/Idle/Run/Hit 교체 (단일 경로)

            // 3) Animator 캐시 및 모바일 친화 설정
            _anim = _instance.GetComponent<Animator>() ?? _instance.AddComponent<Animator>();
            _anim.applyRootMotion = false;
            _anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        // [역할] 이동 블렌드 파라미터 제어(0=Idle, 1=Run)
        public void SetMoveSpeed01(float speed01)
        {
            if (!_anim) return;
            _anim.SetFloat(SpeedHash, Mathf.Clamp01(speed01));
        }

        // [역할] 공격 트리거 발화
        public void PlayAttack()
        {
            if (!_anim) return;
            _anim.ResetTrigger(AttackHash);
            _anim.SetTrigger(AttackHash);
        }

        // [역할] 피격 트리거 발화
        public void PlayHit()
        {
            if (!_anim) return;
            _anim.ResetTrigger(HitHash);
            _anim.SetTrigger(HitHash);
        }

        // [역할] 인스턴스 정리
        public void DisposeInstance()
        {
            if (_instance) Destroy(_instance);
            _instance = null;
            _anim = null;
        }
    }
}
