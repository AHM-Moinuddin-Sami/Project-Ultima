using System.Collections.Generic;
using UnityEngine;

/*
 * MeleeHitDetector
 * ----------------
 * Detects melee hits by tracing the sword between frames.
 *
 * Responsibilities:
 * - trace the blade tip through the space it crossed
 * - trace across the current blade length
 * - apply damage to Health components
 * - prevent the same target from being hit twice in one swing
 * - draw debug lines for tuning the traces
 *
 * BeginAttack and EndAttack are called from animation events.
 */

public class MeleeHitDetector : MonoBehaviour
{
    [Header("Blade")]
    [SerializeField] private Transform bladeBase;
    [SerializeField] private Transform bladeTip;

    [Header("Hit Detection")]
    [SerializeField] private float traceRadius = 0.08f;
    [SerializeField] private LayerMask hitMask;

    [Header("Damage")]
    [SerializeField] private float damage = 25f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    private bool attackActive;

    private Vector3 previousTipPosition;

    private readonly HashSet<Health> hitTargets = new();

    private void LateUpdate()
    {
        if (!attackActive)
        {
            return;
        }

        Vector3 currentTipPosition = bladeTip.position;

        Trace(previousTipPosition, currentTipPosition);
        Trace(bladeBase.position, currentTipPosition);

        if (drawDebug)
        {
            Debug.DrawLine(
                previousTipPosition,
                currentTipPosition,
                Color.red
            );

            Debug.DrawLine(
                bladeBase.position,
                currentTipPosition,
                Color.yellow
            );
        }

        previousTipPosition = currentTipPosition;
    }

    public void BeginAttack()
    {
        attackActive = true;

        hitTargets.Clear();

        previousTipPosition = bladeTip.position;
    }

    public void EndAttack()
    {
        attackActive = false;
        hitTargets.Clear();
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
            Health health = hit.collider.GetComponentInParent<Health>();

            if (health == null || hitTargets.Contains(health))
            {
                continue;
            }

            hitTargets.Add(health);
            health.TakeDamage(damage);

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