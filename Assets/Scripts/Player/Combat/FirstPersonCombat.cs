using UnityEngine;
using UnityEngine.InputSystem;

/*
 * FirstPersonCombat
 * -----------------
 * Handles a buffered three-hit light attack combo.
 *
 * Responsibilities:
 * - start the first light attack
 * - queue the next attack when the player clicks during a combo
 * - progress through LightAttack1 -> LightAttack2 -> LightAttack3
 * - reset the combo when the player stops attacking
 * - forward animation events to melee hit detection
 *
 * Animation Events control when the next queued attack may begin.
 */

public class FirstPersonCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private MeleeHitDetector hitDetector;

    private static readonly int LightAttack1Hash =
        Animator.StringToHash("LightAttack1");

    private static readonly int LightAttack2Hash =
        Animator.StringToHash("LightAttack2");

    private static readonly int LightAttack3Hash =
        Animator.StringToHash("LightAttack3");

    private int comboIndex;

    private bool isAttacking;
    private bool attackQueued;
    private bool canContinueCombo;

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        // Start a new combo.
        if (!isAttacking)
        {
            comboIndex = 1;
            isAttacking = true;

            PlayCurrentAttack();
            return;
        }

        // While an attack is already playing, remember the next click.
        if (comboIndex < 3)
        {
            attackQueued = true;

            // If we're already inside the combo continuation window,
            // immediately advance to the next attack.
            if (canContinueCombo)
            {
                ContinueCombo();
            }
        }
    }

    /*
     * Animation Event:
     * Place this late in Attack 1 and Attack 2, where the next
     * combo attack is allowed to begin.
     */
    public void OpenComboWindow()
    {
        canContinueCombo = true;

        if (attackQueued)
        {
            ContinueCombo();
        }
    }

    /*
     * Animation Event:
     * Marks the end of the current attack.
     *
     * If no attack was queued, the combo ends.
     */
    public void FinishAttack()
    {
        if (attackQueued && comboIndex < 3)
        {
            ContinueCombo();
            return;
        }

        ResetCombo();
    }

    public void BeginAttack()
    {
        hitDetector.BeginAttack();
    }

    public void EndAttack()
    {
        hitDetector.EndAttack();
    }

    private void ContinueCombo()
    {
        if (!attackQueued || comboIndex >= 3)
        {
            return;
        }

        attackQueued = false;
        canContinueCombo = false;

        comboIndex++;

        PlayCurrentAttack();
    }

    private void PlayCurrentAttack()
    {
        switch (comboIndex)
        {
            case 1:
                animator.SetTrigger(LightAttack1Hash);
                break;

            case 2:
                animator.SetTrigger(LightAttack2Hash);
                break;

            case 3:
                animator.SetTrigger(LightAttack3Hash);
                break;
        }
    }

    private void ResetCombo()
    {
        comboIndex = 0;

        isAttacking = false;
        attackQueued = false;
        canContinueCombo = false;
    }
}