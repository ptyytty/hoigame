using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fill;          // Image type = Filled

    private Combatant bound;

    public void Bind(Combatant c)
    {
        if (bound != null) bound.OnHpChanged -= OnHpChanged;
        bound = c;
        if (bound != null)
        {
            bound.OnHpChanged += OnHpChanged;
            OnHpChanged(bound.currentHp, bound.maxHp); // 즉시 1회 갱신
        }
        else
        {
            // 언바인드 시 0 처리
            if (fill) fill.fillAmount = 0f;
        }
    }

    private void OnDestroy()
    {
        if (bound != null) bound.OnHpChanged -= OnHpChanged;
    }

    private void OnHpChanged(int cur, int max)
    {
        if (fill) fill.fillAmount = max > 0 ? (float)cur / max : 0f;
    }
}
