using UnityEngine;

public class KillTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out OstrichMovement movement))
            movement.Die(null);
        else if (collision.gameObject.TryGetComponent(out OstrichEgg egg))
            egg.Die();
    }
}
