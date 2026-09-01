using UnityEngine;
using UnityEngine.InputSystem;

/*
 * DamagePlayerTest
 * ----------------
 * Temporary input-driven test for enemy attacks against the player.
 *
 * Only reacts to the Input System's performed phase so one key press
 * produces exactly one attack.
 */

public class DamagePlayerTest : MonoBehaviour
{
    [SerializeField] private EnemyCombat enemyCombat;
    [SerializeField] private PlayerHealth playerHealth;

    public void OnDamagePlayer(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        enemyCombat.HitPlayer(playerHealth);
    }
}