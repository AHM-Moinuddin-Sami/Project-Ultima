using System.Collections;
using UnityEngine;

/*
 * EnemyCombat
 * -----------
 * Handles enemy attack resolution and combat reactions.
 *
 * Responsibilities:
 * - request attack animations, including a 4-hit combo
 *   (Combo1 -> Combo2 -> Combo3 -> Combo4)
 * - report whether an attack animation is currently active, and which
 *   phase of it (Startup / Active / Recovery) is currently playing
 * - damage the player when the animation impact event fires
 * - detect blocked/parried attacks
 * - stagger after a successful parry
 * - prevent attacking while staggered or on cooldown
 *
 * The combo chain itself lives in the Animator: each ComboN state has
 * two exit-time transitions -- one to ComboN+1 (early, right after the
 * step's own impact) gated on the ComboContinue bool, one unconditional
 * fallback (later, near the end of the step's own follow-through)
 * straight to Idle. There used to be a dedicated ComboN_Stop clip on
 * that fallback, but those clips are actually the pack's "attack
 * blocked" reaction animations (a raised parry/guard pose), not a
 * recovery-to-idle motion -- using them made every non-chained hit look
 * like it was swinging twice. Letting each step just finish its own
 * animation and blend to Idle reads as one clean swing.
 * SetComboContinuation() is the only thing this script needs to drive
 * that: it's expected to be
 * called every frame while IsAttacking is true (EnemyAI does this,
 * based on whether the target is still in range). Whatever step is
 * currently mid-swing always finishes -- there's no cosmetic way to
 * cut a swing short without a dedicated cancel animation -- but as
 * soon as ComboContinue reads false at that step's exit time, the
 * chain stops there instead of continuing, which is what "stop the
 * combo at any step" means in practice. A hard interrupt (stagger,
 * death) still overrides all of this instantly via the Animator's
 * Any State transitions, regardless of which combo step is playing.
 *
 * Attack phase: IsAttacking (the Animator's "Attack" tag) spans the
 * *entire* swing -- startup, the actual hit, and recovery -- and that
 * stays true, because movement genuinely needs to stay locked for all
 * of it. But "is this swing currently dangerous" is a much narrower
 * window inside that: each combo clip fires ActiveStart/ActiveEnd
 * Animation Events bracketing the moment the weapon could plausibly
 * connect (see EnemyAnimationEvents). CurrentPhase exposes that
 * breakdown for anything that needs to react to the real windup/
 * strike/recovery shape of the attack rather than treating the whole
 * clip as one undifferentiated block -- e.g. a future punish-on-
 * recovery mechanic, or hyper-armor that only applies during Active.
 *
 * Animator requirement:
 * - all attack/combo states must use the Animator tag "Attack"
 * - each combo clip fires ActiveStart / Impact / ActiveEnd events
 */

public class EnemyCombat : MonoBehaviour
{
    public enum AttackPhase
    {
        None,
        Startup,
        Active,
        Recovery
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Attack")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Parry")]
    [SerializeField] private float parryStaggerDuration = 1f;

    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    private static readonly int StaggerHash =
        Animator.StringToHash("Stagger");

    private static readonly int ComboContinueHash =
        Animator.StringToHash("ComboContinue");

    private bool isStaggered;
    private float nextAttackTime;
    private bool isInActiveWindow;
    private bool hasBeenActiveThisSwing;

    public bool IsStaggered => isStaggered;

    public bool CanAttack =>
        !isStaggered && Time.time >= nextAttackTime;

    public bool IsAttacking
    {
        get
        {
            AnimatorStateInfo currentState =
                animator.GetCurrentAnimatorStateInfo(0);

            if (currentState.IsTag("Attack"))
            {
                return true;
            }

            // Also count a transition INTO an attack as attacking.
            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextState =
                    animator.GetNextAnimatorStateInfo(0);

                return nextState.IsTag("Attack");
            }

            return false;
        }
    }

    /*
     * True only for the narrow window bracketed by that swing's
     * ActiveStart/ActiveEnd events -- the moment the weapon could
     * plausibly connect, not the whole animation. Guarded by
     * IsAttacking so a swing that got hard-interrupted (stagger,
     * death) mid-active-window can't leave this stuck true.
     */
    public bool IsInActiveWindow => isInActiveWindow && IsAttacking;

    /*
     * Startup / Active / Recovery breakdown of the current swing.
     * See the class comment for what each phase is for.
     */
    public AttackPhase CurrentPhase
    {
        get
        {
            if (!IsAttacking)
            {
                return AttackPhase.None;
            }

            if (isInActiveWindow)
            {
                return AttackPhase.Active;
            }

            return hasBeenActiveThisSwing
                ? AttackPhase.Recovery
                : AttackPhase.Startup;
        }
    }

    /*
     * Returns true only when an attack was successfully requested.
     * This lets EnemyAI know whether it should enter its Attack state.
     */
    public bool TryAttack()
    {
        if (!CanAttack)
        {
            return false;
        }

        nextAttackTime = Time.time + attackCooldown;

        // Default to not chaining until told otherwise -- EnemyAI
        // calls SetComboContinuation() every frame once the combo is
        // under way, but this keeps a fresh combo's first step honest
        // if nothing has called it yet at the moment we start.
        animator.SetBool(ComboContinueHash, false);
        animator.SetTrigger(AttackHash);

        // Fresh swing -- start back in Startup.
        isInActiveWindow = false;
        hasBeenActiveThisSwing = false;

        return true;
    }

    // Called by EnemyAnimationEvents at the start of each combo step
    // (Combo1, Combo2, ...), so every chained hit gets its own fresh
    // Startup phase rather than inheriting "already been active" from
    // whichever step came before it.
    public void OnSwingStart()
    {
        isInActiveWindow = false;
        hasBeenActiveThisSwing = false;
    }

    // Called by EnemyAnimationEvents at the start/end of each combo
    // step's active (danger) window.
    public void OnActiveWindowStart()
    {
        isInActiveWindow = true;
        hasBeenActiveThisSwing = true;
    }

    public void OnActiveWindowEnd()
    {
        isInActiveWindow = false;
    }

    /*
     * Call every frame while IsAttacking is true to control whether
     * the combo keeps chaining. See the class comment for how this
     * connects to the Animator's own transitions.
     */
    public void SetComboContinuation(bool shouldContinue)
    {
        animator.SetBool(ComboContinueHash, shouldContinue);
    }

    // Called by EnemyAnimationEvents when the attack clip's impact
    // event fires.
    public void OnAttackImpact()
    {
        HitPlayer(playerHealth);
    }

    public void HitPlayer(PlayerHealth playerHealth)
    {
        if (isStaggered)
        {
            return;
        }

        HitResult result = playerHealth.TakeDamage(damage);

        if (result == HitResult.Parried)
        {
            StartCoroutine(ParryStagger());
        }
    }

    private IEnumerator ParryStagger()
    {
        isStaggered = true;

        animator.SetTrigger(StaggerHash);

        yield return new WaitForSeconds(parryStaggerDuration);

        isStaggered = false;
    }
}