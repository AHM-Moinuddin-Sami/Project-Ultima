using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * FirstPersonCombat
 * -----------------
 * Handles first-person melee attack input and attack timing.
 *
 * Responsibilities:
 * - trigger light attack animations
 * - prevent overlapping attacks
 * - forward animation events to melee hit detection
 */

public class FirstPersonCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private MeleeHitDetector hitDetector;

    [Header("Light Attack")]
    [SerializeField] private float lightAttackDuration = 0.8f;

    private static readonly int LightAttackHash =
        Animator.StringToHash("LightAttack");

    private bool isAttacking;

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || isAttacking)
        {
            return;
        }

        StartCoroutine(LightAttack());
    }

    private IEnumerator LightAttack()
    {
        isAttacking = true;

        animator.SetTrigger(LightAttackHash);

        yield return new WaitForSeconds(lightAttackDuration);

        isAttacking = false;
    }

    public void BeginAttack()
    {
        hitDetector.BeginAttack();
    }

    public void EndAttack()
    {
        hitDetector.EndAttack();
    }
}