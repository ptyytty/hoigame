using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 스킨드 메시의 컬링/바운즈로 인한 아웃라인 깜빡임을 방지하기 위한 보정 유틸.
/// - 모든 SkinnedMeshRenderer에 updateWhenOffscreen 적용
/// - localBounds 확장(애니메이션 변형으로 바운즈가 벗어나 컬링되는 문제 완화)
/// - (선택) 아웃라인 머티리얼을 같은 SkinnedMeshRenderer에 추가로 부착
/// 모바일 빌드 환경을 고려하여 런타임 오버헤드는 매우 낮습니다.
/// </summary>
public class OutlineFixer : MonoBehaviour
{
    [Header("Bounds 확장 배율 (1=확장없음)")]
    [Tooltip("팔/꼬리 등 크게 휘는 캐릭터는 1.5~3 권장")]
    [Range(1f, 4f)] public float boundsScale = 2f;

    [Header("오프스크린에서도 업데이트")]
    [Tooltip("프러스텀 컬링으로 인한 깜빡임 방지")]
    public bool updateWhenOffscreen = true;

    [Header("아웃라인 머티리얼 자동 부착 (선택)")]
    [Tooltip("같은 SkinnedMeshRenderer에 아웃라인 머티리얼을 추가로 넣습니다.")]
    public Material outlineMaterial; // 인버티드-헐 셰이더를 할당

    [Tooltip("이미 아웃라인 머티리얼이 들어가 있으면 중복 추가하지 않습니다.")]
    public bool avoidDuplicateOutlineMat = true;

    void Awake()
    {
        var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
        foreach (var smr in smrs)
        {
            // (1) 컬링 안정화
            smr.updateWhenOffscreen = updateWhenOffscreen;

            // (2) 넉넉한 바운즈
            var b = smr.localBounds;
            b.extents = b.extents * boundsScale;
            smr.localBounds = b;

            // (3) 아웃라인 머티리얼 추가(선택)
            if (outlineMaterial != null)
            {
                var mats = smr.sharedMaterials;
                bool has = false;
                if (avoidDuplicateOutlineMat)
                {
                    foreach (var m in mats) if (m == outlineMaterial) { has = true; break; }
                }
                if (!has)
                {
                    var newMats = new Material[mats.Length + 1];
                    for (int i = 0; i < mats.Length; i++) newMats[i] = mats[i];
                    newMats[newMats.Length - 1] = outlineMaterial;
                    smr.sharedMaterials = newMats;
                }
            }

            // (4) ★중요: 기본 상태는 항상 "두께 0"으로 시작시킴
            //     -> 스폰되자마자 아웃라인이 보이는 문제를 원천 차단
            var mpb = new MaterialPropertyBlock();
            smr.GetPropertyBlock(mpb);
            mpb.SetFloat(Shader.PropertyToID("_OutlineWidth"), 0f);
            smr.SetPropertyBlock(mpb);
        }
    }

    void Start()
    {
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            FixBounds(smr, 2f); // 2배 확장
            smr.updateWhenOffscreen = true; // 화면 밖에서도 업데이트 유지
        }

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.updateWhenOffscreen = true;
        }

    }

    // [역할] SkinnedMeshRenderer의 Bounds를 확장해 아웃라인 클리핑 방지
    void FixBounds(SkinnedMeshRenderer smr, float scale = 2f)
    {
        var bounds = smr.localBounds;
        bounds.extents *= scale; // 경계 박스를 확장
        smr.localBounds = bounds;
    }


}
