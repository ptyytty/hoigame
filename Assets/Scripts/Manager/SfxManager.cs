using UnityEngine;

/// <summary>
/// [역할] 전투/공용 효과음을 한곳에서 재생.
/// - 모든 유닛이 같은 공격 사운드를 쓸 때 OneShot으로 트리거
/// - 모바일 빌드 고려: 불필요한 GC 없는 PlayOneShot 사용
/// </summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("공용 효과음 클립")]
    [SerializeField] private AudioClip attackSfx; // [역할] 공격 임팩트 SFX (모든 유닛 공통)
    [Range(0f, 1f)] [SerializeField] private float volume = 0.85f;

    private AudioSource _src;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // [역할] 2D 오디오 소스(카메라/리스너 위치 기준, 공간화 불필요하면 2D 권장)
        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = false;
        _src.spatialBlend = 0f; // 0=2D, 1=3D
    }

    /// <summary>
    /// [역할] 공격 임팩트 SFX를 즉시 재생 (2D)
    /// </summary>
    public void PlayAttackSfx()
    {
        if (!attackSfx) return;
        _src.PlayOneShot(attackSfx, volume);
    }

    /// <summary>
    /// [역할] 월드 좌표 기준 3D로 재생하고 싶을 때 사용(선택).
    /// </summary>
    public void PlayAttackSfxAt(Vector3 worldPos)
    {
        if (!attackSfx) return;
        // 간단 구현: 필요 시 3D 공간감. (참고: 이 방식은 임시 오브젝트를 생성)
        AudioSource.PlayClipAtPoint(attackSfx, worldPos, volume);
    }
}
