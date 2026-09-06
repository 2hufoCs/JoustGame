using Pathfinding;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AIDestinationSetter))]
[RequireComponent(typeof(Rigidbody2D))]
public class PterodactylAI : MonoBehaviour
{
    private AIDestinationSetter _destinationSetter;
    private Rigidbody2D _rb;
    private Transform player1Transform;
    private Transform player2Transform;

    [SerializeField] private float minXVel;
    [SerializeField] private float xPosEnd;
    
    [SerializeField] Transform worldOriginTransform;

    private bool canTargetPlayer1;
    private bool canTargetPlayer2;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _destinationSetter = GetComponent<AIDestinationSetter>();
        _rb = GetComponent<Rigidbody2D>();
        
        // Get players during runtime
        PlayerOstrichInput[] players = FindObjectsByType<PlayerOstrichInput>(FindObjectsSortMode.None);
        foreach (PlayerOstrichInput player in players)
        {
            if (player.playerId == 1)
                player1Transform = player.transform;
            else if (player.playerId == 2)
                player2Transform = player.transform;
        }
    }

    void OnEnable()
    {
        EnemyWaveManager.spawnedEnemies.Add(gameObject);
    }

    void OnDestroy()
    {
        EnemyWaveManager.spawnedEnemies.Remove(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        ChooseTarget();

        if (transform.position.x < xPosEnd)
            Destroy(gameObject);
    }

    void LateUpdate()
    {
        // Clamp x velocity so pterodactyl continues going to the left
        float xVel = Mathf.Clamp(_rb.linearVelocity.x, -100, minXVel);
        _rb.linearVelocity = new Vector2(xVel, _rb.linearVelocity.y);
        Debug.Log("xvel is now " + _rb.linearVelocity.x);
    }

    void ChooseTarget()
    {
        // If some players are already dead
        canTargetPlayer1 = player1Transform && Vector2.Angle(player1Transform.position - transform.position, Vector2.left) < 60;
        canTargetPlayer2 = player2Transform && Vector2.Angle(player2Transform.position - transform.position, Vector2.left) < 60;

        if (!canTargetPlayer1 && !canTargetPlayer2)
            _destinationSetter.target = worldOriginTransform;
        else if (!canTargetPlayer1)
            _destinationSetter.target = player2Transform;
        else if (!canTargetPlayer2)
            _destinationSetter.target = player1Transform;
        else
        {
            // Choose closest player
            bool player1Closest = (player1Transform.position - transform.position).magnitude <
                                  (player2Transform.position - transform.position).magnitude;
            _destinationSetter.target = player1Closest ? player1Transform : player2Transform;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
            collision.gameObject.GetComponent<OstrichMovement>().Die(null);
    }
}
