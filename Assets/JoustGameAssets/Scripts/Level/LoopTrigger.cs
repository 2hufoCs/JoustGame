using UnityEngine;

public class LoopTrigger : MonoBehaviour
{
    [SerializeField] Transform targetTeleport;
    static bool teleportedThisFrame = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Egg")) && !teleportedThisFrame)
        {
            teleportedThisFrame = true;
            other.transform.position = new Vector2(targetTeleport.position.x, other.transform.position.y);

            Invoke(nameof(AllowTeleport), .1f);
        }
    }

    void AllowTeleport() => teleportedThisFrame = false;
}
