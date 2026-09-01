using UnityEngine;

/*
 * EnemyAnimationEvents
 * ---------------------
 * Receives the Animation Events baked into each combo swing and
 * forwards them to EnemyCombat:
 * - "SwingStart"  -> this swing has begun (near t=0 of the clip) --
 *                    resets the Startup/Recovery bookkeeping so a
 *                    chained hit (Combo2, Combo3, Combo4...) gets its
 *                    own Startup phase instead of inheriting
 *                    "already been active" from the previous swing
 * - "Impact"      -> the hit actually connects, damage is applied
 * - "ActiveStart" -> the weapon enters the range where it could
 *                    plausibly connect (start of the danger window)
 * - "ActiveEnd"   -> the weapon has passed through and is no longer
 *                    a threat (start of recovery)
 *
 * Together these carve each swing into Startup / Active / Recovery,
 * rather than the whole clip counting as one undifferentiated
 * "attacking" blob -- see EnemyCombat.CurrentPhase.
 *
 * Animation Events only call methods on components attached to the
 * Animator's own GameObject, so this has to live on the animated
 * model rather than on the root object alongside EnemyCombat.
 */

public class EnemyAnimationEvents : MonoBehaviour
{
    [SerializeField] private EnemyCombat enemyCombat;

    public void SendEvent(string eventName)
    {
        switch (eventName)
        {
            case "SwingStart":
                enemyCombat.OnSwingStart();
                break;

            case "ActiveStart":
                enemyCombat.OnActiveWindowStart();
                break;

            case "ActiveEnd":
                enemyCombat.OnActiveWindowEnd();
                break;

            default:
                if (eventName.StartsWith("Impact"))
                {
                    enemyCombat.OnAttackImpact();
                }
                break;
        }
    }
}
