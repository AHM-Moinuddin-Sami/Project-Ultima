using UnityEngine;
using UnityEngine.InputSystem;

/*
 * FirstPersonCombat
 * -----------------
 * Handles first-person melee attacks.
 *
 * Responsibilities:
 * - three-hit buffered light combo
 * - single heavy attack
 * - configure damage and bonus range per attack
 * - prevent attacks from overlapping
 * - control melee damage windows through animation events
 */

public class FirstPersonCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private MeleeHitDetector hitDetector;

    [Header("Light Attack 1")]
    [SerializeField] private float light1Damage = 20f;
    [SerializeField] private float light1BonusRange = 0f;

    [Header("Light Attack 2")]
    [SerializeField] private float light2Damage = 25f;
    [SerializeField] private float light2BonusRange = 0f;

    [Header("Light Attack 3")]
    [SerializeField] private float light3Damage = 35f;
    [SerializeField] private float light3BonusRange = 0.1f;

    [Header("Heavy Attack")]
    [SerializeField] private float heavyDamage = 50f;
    [SerializeField] private float heavyBonusRange = 0.4f;

    private static readonly int LightAttack1Hash =
        Animator.StringToHash("LightAttack1");

    private static readonly int LightAttack2Hash =
        Animator.StringToHash("LightAttack2");

    private static readonly int LightAttack3Hash =
        Animator.StringToHash("LightAttack3");

    private static readonly int HeavyAttackHash =
        Animator.StringToHash("HeavyAttack");

    private static readonly int IsBlockingHash =
        Animator.StringToHash("IsBlocking");

    private int comboIndex;

    private bool isAttacking;
    private bool attackQueued;
    private bool canContinueCombo;
    private bool isHeavyAttack;
    private bool isBlocking;
    private bool blockHeld;
    private bool blockQueued;

    public bool IsBlocking => isBlocking;

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || isHeavyAttack || isBlocking)
        {
            return;
        }

        if (!isAttacking)
        {
            comboIndex = 1;
            isAttacking = true;

            PlayCurrentAttack();
            return;
        }

        if (comboIndex < 3)
        {
            attackQueued = true;

            if (canContinueCombo)
            {
                ContinueCombo();
            }
        }
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || isAttacking || isBlocking)
        {
            return;
        }

        isAttacking = true;
        isHeavyAttack = true;

        hitDetector.ConfigureAttack(
            heavyDamage,
            heavyBonusRange
        );

        animator.SetTrigger(HeavyAttackHash);
    }

    public void OpenComboWindow()
    {
        if (isHeavyAttack)
        {
            // Heavy attacks can also cancel into block once their
            // cancel window has been reached.
            if (blockQueued && blockHeld)
            {
                CancelAttackIntoBlock();
            }

            return;
        }

        canContinueCombo = true;

        // Blocking takes priority over continuing the light combo.
        if (blockQueued && blockHeld)
        {
            CancelAttackIntoBlock();
            return;
        }

        if (attackQueued)
        {
            ContinueCombo();
        }
    }

    public void FinishAttack()
    {
        if (isHeavyAttack)
        {
            ResetCombat();
            return;
        }

        if (attackQueued && comboIndex < 3)
        {
            ContinueCombo();
            return;
        }

        ResetCombat();
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
                hitDetector.ConfigureAttack(
                    light1Damage,
                    light1BonusRange
                );

                animator.SetTrigger(LightAttack1Hash);
                break;

            case 2:
                hitDetector.ConfigureAttack(
                    light2Damage,
                    light2BonusRange
                );

                animator.SetTrigger(LightAttack2Hash);
                break;

            case 3:
                hitDetector.ConfigureAttack(
                    light3Damage,
                    light3BonusRange
                );

                animator.SetTrigger(LightAttack3Hash);
                break;
        }
    }

    private void ResetCombat()
    {
        comboIndex = 0;

        isAttacking = false;
        isHeavyAttack = false;
        attackQueued = false;
        canContinueCombo = false;

        hitDetector.EndAttack();
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            blockHeld = true;

            if (isAttacking)
            {
                // Remember the input and block as soon as the
                // current attack reaches its cancel window.
                blockQueued = true;
            }
            else
            {
                StartBlock();
            }
        }

        if (context.canceled)
        {
            blockHeld = false;
            blockQueued = false;

            StopBlock();
        }
    }

    private void StartBlock()
    {
        if (isAttacking)
        {
            return;
        }

        isBlocking = true;

        animator.SetBool(IsBlockingHash, true);
    }

    private void StopBlock()
    {
        if (!isBlocking)
        {
            return;
        }

        isBlocking = false;

        animator.SetBool(IsBlockingHash, false);
    }

    private void CancelAttackIntoBlock()
    {
        hitDetector.EndAttack();

        comboIndex = 0;

        isAttacking = false;
        isHeavyAttack = false;

        attackQueued = false;
        canContinueCombo = false;

        blockQueued = false;

        StartBlock();
    }
}