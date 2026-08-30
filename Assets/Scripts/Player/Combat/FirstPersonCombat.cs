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

    [Header("Parry")]
    [SerializeField] private float parryWindow = 0.2f;

    [Header("Recovery")]
    [SerializeField] private float light1Recovery = 0.45f;
    [SerializeField] private float light2Recovery = 0.5f;
    [SerializeField] private float light3Recovery = 0.65f;
    [SerializeField] private float heavyRecovery = 0.85f;

    [Header("Attack Speed")]
    [SerializeField] private float attackSpeed = 1f;

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
    public bool IsBlocking => isBlocking;

    private bool isParrying;
    private float parryTimer;
    public bool IsParrying => isParrying;
    private bool attackRecoveryActive;
    private float attackRecoveryTimer;

    private bool bufferedLightAttack;
    private bool bufferedHeavyAttack;

    private void Update()
    {
        if (attackRecoveryActive)
        {
            attackRecoveryTimer -= Time.deltaTime;

            if (attackRecoveryTimer <= 0f)
            {
                attackRecoveryActive = false;

                TryUseBufferedAttack();
            }
        }

        if (isParrying)
        {
            parryTimer -= Time.deltaTime;

            if (parryTimer <= 0f)
            {
                isParrying = false;
            }
        }
    }

    public void OnLightAttack(InputAction.CallbackContext context)
    {
        if (!context.performed || isBlocking || isHeavyAttack)
        {
            return;
        }

        // Start a new combo.
        if (!isAttacking)
        {
            if (attackRecoveryActive)
            {
                bufferedLightAttack = true;
                return;
            }

            comboIndex = 1;
            isAttacking = true;

            PlayCurrentAttack();
            return;
        }

        // Queue the next attack in the current combo.
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
        if (!context.performed || isBlocking || isAttacking)
        {
            return;
        }

        if (attackRecoveryActive)
        {
            bufferedHeavyAttack = true;
            return;
        }

        StartHeavyAttack();
    }

    private void StartHeavyAttack()
    {
        isAttacking = true;
        isHeavyAttack = true;

        SetAttackSpeed();

        hitDetector.ConfigureAttack(
            heavyDamage,
            heavyBonusRange
        );

        StartRecovery(heavyRecovery);

        animator.SetTrigger(HeavyAttackHash);
    }

    public void OpenComboWindow()
    {
        if (isHeavyAttack)
        {
            return;
        }

        canContinueCombo = true;

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
        SetAttackSpeed();

        switch (comboIndex)
        {
            case 1:
                hitDetector.ConfigureAttack(
                    light1Damage,
                    light1BonusRange
                );

                StartRecovery(light1Recovery);

                animator.SetTrigger(LightAttack1Hash);
                break;

            case 2:
                hitDetector.ConfigureAttack(
                    light2Damage,
                    light2BonusRange
                );

                StartRecovery(light2Recovery);

                animator.SetTrigger(LightAttack2Hash);
                break;

            case 3:
                hitDetector.ConfigureAttack(
                    light3Damage,
                    light3BonusRange
                );

                StartRecovery(light3Recovery);

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

        ResetAnimationSpeed();
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            StartBlock();
        }

        if (context.canceled)
        {
            StopBlock();
        }
    }

    private void StartBlock()
    {
        if (isBlocking)
        {
            return;
        }

        if (isAttacking)
        {
            CancelCurrentAttack();
        }

        isBlocking = true;

        // Every new block starts with a short parry window.
        isParrying = true;
        parryTimer = parryWindow;

        animator.SetBool(IsBlockingHash, true);
    }

    private void StopBlock()
    {
        if (!isBlocking)
        {
            return;
        }

        isBlocking = false;
        isParrying = false;

        animator.SetBool(IsBlockingHash, false);
    }

    private void CancelCurrentAttack()
    {
        hitDetector.EndAttack();

        comboIndex = 0;

        isAttacking = false;
        isHeavyAttack = false;

        attackQueued = false;
        canContinueCombo = false;

        animator.ResetTrigger(LightAttack1Hash);
        animator.ResetTrigger(LightAttack2Hash);
        animator.ResetTrigger(LightAttack3Hash);
        animator.ResetTrigger(HeavyAttackHash);

        ResetAnimationSpeed();
    }

    private void SetAttackSpeed()
    {
        animator.speed = attackSpeed;
    }

    private void ResetAnimationSpeed()
    {
        animator.speed = 1f;
    }

    // Recovery

    private void StartRecovery(float duration)
    {
        attackRecoveryActive = true;
        attackRecoveryTimer = duration;
    }

    // Buffered Attacks

    private void TryUseBufferedAttack()
    {
        if (isBlocking || isAttacking)
        {
            return;
        }

        if (bufferedHeavyAttack)
        {
            bufferedHeavyAttack = false;
            StartHeavyAttack();
            return;
        }

        if (bufferedLightAttack)
        {
            bufferedLightAttack = false;

            comboIndex = 1;
            isAttacking = true;

            PlayCurrentAttack();
        }
    }
}