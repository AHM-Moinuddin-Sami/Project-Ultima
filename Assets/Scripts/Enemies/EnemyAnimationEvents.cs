using UnityEngine;

/*
 * EnemyAnimationEvents
 * ---------------------
 * Receives the Animation Events baked into the LongswordAnimsetPro
 * clips (SendEvent("ImpactX")) and forwards attack-impact events to
 * EnemyCombat.
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
        if (eventName.StartsWith("Impact"))
        {
            enemyCombat.OnAttackImpact();
        }
    }
}
