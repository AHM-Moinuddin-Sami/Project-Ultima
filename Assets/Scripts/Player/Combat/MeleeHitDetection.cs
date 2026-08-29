using System.Collections.Generic;
using UnityEngine;

/*
 * MeleeHitDetector
 * ----------------
 * Detects melee hits by tracing the animated weapon between frames.
 *
 * Responsibilities:
 * - trace the blade tip through its movement
 * - trace along the current blade length
 * - support different damage values per attack
 * - support optional bonus reach beyond the blade tip
 * - prevent the same target from being damaged twice per swing
 */

public class MeleeHitDetector : MonoBehaviour
{
    [Header("Blade")]
    [SerializeField] private Transform bladeBase;
    [SerializeField] private Transform bladeTip;

    [Header("Hit Detection")]
    [SerializeField] private float traceRadius = 0.08f;
    [SerializeField] private LayerMask hitMask;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    private float currentDamage;
    private float currentBonusRange;

    private bool attackActive;

    private Vector3 previousTraceTip;

    private readonly HashSet<Health> hitTargets = new();

    /*
     * Sets the properties used by the next/current attack.
     */
    public void ConfigureAttack(float damage, float bonusRange)
    {
        currentDamage = damage;
        currentBonusRange = bonusRange;
    }

    public void BeginAttack()
    {
        attackActive = true;
        hitTargets.Clear();

        previousTraceTip = GetTraceTip();
    }

    public void EndAttack()
    {
        attackActive = false;
        hitTargets.Clear();
    }

    private void LateUpdate()
    {
        if (!attackActive)
        {
            return;
        }

        Vector3 currentTraceTip = GetTraceTip();

        // Sweep the effective blade tip between frames.
        Trace(previousTraceTip, currentTraceTip);

        // Check along the current blade, including bonus range.
        Trace(bladeBase.position, currentTraceTip);

        if (drawDebug)
        {
            Debug.DrawLine(
                previousTraceTip,
                currentTraceTip,
                Color.red
            );

            Debug.DrawLine(
                bladeBase.position,
                currentTraceTip,
                Color.yellow
            );
        }

        previousTraceTip = currentTraceTip;
    }

    /*
     * Returns the real blade tip plus any additional reach
     * configured for the current attack.
     */
    private Vector3 GetTraceTip()
    {
        Vector3 bladeDirection =
            (bladeTip.position - bladeBase.position).normalized;

        return bladeTip.position +
               bladeDirection * currentBonusRange;
    }

    private void Trace(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            return;
        }

        RaycastHit[] hits = Physics.SphereCastAll(
            start,
            traceRadius,
            direction.normalized,
            distance,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            Health health =
                hit.collider.GetComponentInParent<Health>();

            if (health == null || hitTargets.Contains(health))
            {
                continue;
            }

            hitTargets.Add(health);

            health.TakeDamage(currentDamage);

            if (drawDebug)
            {
                Debug.DrawRay(
                    hit.point,
                    hit.normal * 0.3f,
                    Color.green,
                    0.5f
                );
            }
        }
    }
}