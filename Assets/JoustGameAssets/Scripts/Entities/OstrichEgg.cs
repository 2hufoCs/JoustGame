using UnityEngine;
using System.Collections.Generic;

public class OstrichEgg : MonoBehaviour
{
    public Rigidbody2D rb;
    public float baseForce;
    [SerializeField] private GameObject bounceParticles;
    
    public float respawnCooldown;
    [SerializeField] private AnimationCurve shakeInterval;
    private float _shakeIntervalTimer;
    private bool _rotatingRight;
    
    public List<GameObject> ostrichList;
    
    private GameObject _ostrichToRespawn;
    private float respawnCooldownTimer;
    
    private float _spawnInvicibilityTime = .2f;
    private bool _invicible = true;

    private const int _eggScore = 250;
    
    public void InitializeEggData(GameObject ostrichToRespawn, float _respawnCooldown)
    {
        foreach (GameObject go in ostrichList)
        {
            // Either instance or prefab
            if (go.name == ostrichToRespawn.name || go.name + "(Clone)" == ostrichToRespawn.name)
            {
                this._ostrichToRespawn = go;
            }
        }
        if (this._ostrichToRespawn == null)
            Debug.LogError("spawned egg with no ostrich to respawn");
        
        this.respawnCooldown = _respawnCooldown;
    }

    private void Awake()
    {
        EnemyWaveManager.spawnedEnemies.Add(gameObject);
    }

    private void Update()
    {
        respawnCooldownTimer += Time.deltaTime;
        if (respawnCooldownTimer >= respawnCooldown)
        {
            Debug.Log("ostrich egg: " + _ostrichToRespawn.name);
            EnemyWaveManager.OnEggRespawn(_ostrichToRespawn, transform.position);
            Die();
        }
        
        // Invicibility
        if (respawnCooldownTimer >= _spawnInvicibilityTime)
            _invicible = false;
        
        // Move shake
        
        
        // Rotate shake
        float normalizedTime = respawnCooldownTimer / respawnCooldown;
        _shakeIntervalTimer += Time.deltaTime;
        if (_shakeIntervalTimer >= shakeInterval.Evaluate(normalizedTime))
        {
            _shakeIntervalTimer = 0;
            _rotatingRight = (_rotatingRight && transform.localEulerAngles.z < 180) || (!_rotatingRight && transform.localEulerAngles.z > .1f);

            transform.localEulerAngles += _rotatingRight ? new Vector3(0, 0, -45) : new Vector3(0, 0, 45);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !_invicible)
            Die();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        Instantiate(bounceParticles, other.GetContact(0).point, Quaternion.identity, transform); 
    }

    public void Die()
    {
        EnemyWaveManager.spawnedEnemies.Remove(gameObject);
        ScoreManager.OnScoreGained(_eggScore, transform.position);
        Destroy(gameObject);
    }
}
