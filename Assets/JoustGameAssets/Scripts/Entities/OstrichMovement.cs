using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class OstrichMovement : MonoBehaviour
{
    private Rigidbody2D _rb;

    [SerializeField] private OstrichEntityData _data;
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject eggPrefab;

    [Header("Bounce")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float bounceMinThreshold;
    [SerializeField, Range(0, 1)] private float bounceCoefficient;
    
    [SerializeField] private GameObject bounceParticles;
    [SerializeField] private GameObject flapParticles;

    [SerializeField] private float fightTieMargin;

    private const float groundDist = 1.5f;
    private float _runSfxTimer;
    private float _skidSfxTimer;

    private bool _dying = false;
    private bool _isPlayer;
    private bool _skid;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _isPlayer = GetComponent<PlayerOstrichInput>();
    }

    void Update()
    {
        _animator.SetBool("isFlying", !IsGrounded());
        _animator.SetFloat("xVel", _rb.linearVelocity.x);
        _animator.SetFloat("xVelAbs", Mathf.Abs(_rb.linearVelocity.x));
        
        float xVel = Mathf.Abs(_rb.linearVelocity.x);
        // Run sfx
        if (_isPlayer && !_skid && IsGrounded() && xVel > .2f)
        {
            _runSfxTimer += Time.deltaTime;
            if (_runSfxTimer >= 1.8f / xVel)
            {
                _runSfxTimer = 0;
                AudioManager.PlaySound(SoundType.RUN, .7f);
            }
        }
    }

    public void Move(float dir)
    {
        float targetVel = _data.maxMoveSpeed * dir;
        float velDiff = targetVel - _rb.linearVelocity.x;

        // Different strength depending on being on ground/air, and accelerating/decelerating
        float amount;
        if (dir == 0)
            amount = IsGrounded() ? _data.groundDeceleration : _data.airDeceleration;
        else
            amount = IsGrounded() ? _data.groundAcceleration : _data.airAcceleration;

        float acceleratedForce = Mathf.Pow(Mathf.Abs(velDiff), amount) * Mathf.Sign(velDiff);
        _rb.AddForce(new Vector2(acceleratedForce, 0));
        
        // Skid SFX
        float xVel = _rb.linearVelocity.x;
        _skid = ((dir < -.05f && xVel > .05f) || (dir > .05f && xVel < -.05f)) && IsGrounded();
        
        if (_isPlayer && _skid)
        {
            _skidSfxTimer += Time.fixedDeltaTime;
            if (_skidSfxTimer >= .08f)
            {
                _skidSfxTimer = 0;
                AudioManager.PlaySound(SoundType.SKID, .6f);
            }
        }
    }

    public void Jump()
    {
        _rb.AddForce(Vector2.up * _data.jumpForce, ForceMode2D.Impulse);
        if (!_dying) _animator.SetTrigger("jumpTrigger");
        if (_isPlayer) AudioManager.PlaySound(SoundType.FLAP, .7f);
        
        Instantiate(flapParticles, transform.position + new Vector3(0, -1, -.001f), Quaternion.Euler(-90, 0, 0), transform);
    }

    bool IsGrounded()
    {
        return Physics2D.CircleCast(transform.position, .99f, Vector3.down, groundDist + .02f, groundLayer);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        // Bounce particles
        Instantiate(bounceParticles, (Vector3)other.GetContact(0).point - Vector3.back * .001f, Quaternion.identity, transform); 
        
        // Bounce against surfaces (except when standing on them)
        if (other.gameObject.CompareTag("Ground"))
        {
            Vector3 normal = -other.GetContact(0).normal;

            // Don't bounce against downwards platform
            if (Vector2.Dot(normal, Vector2.down) > bounceMinThreshold)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
            }
            else AudioManager.PlaySound(SoundType.BOUNCE);
        }

        if (other.gameObject.CompareTag("Player") && gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("Enemy") && gameObject.CompareTag("Player"))
        {
            if (transform.position.y - other.transform.position.y > fightTieMargin)
                other.gameObject.GetComponent<OstrichMovement>().Die(other);
            else if (other.transform.position.y - transform.position.y > fightTieMargin)
                Die(other);
        }
    }

    private bool _spawnedEgg;
    public void Die(Collision2D other)
    {
        if (_spawnedEgg)
            return;
        
        if (!gameObject.CompareTag("Player") && !_spawnedEgg)
        {
            _dying = true;
            
            GameObject egg = Instantiate(eggPrefab, transform.position, Quaternion.identity, transform.parent);
            OstrichEgg eggScript = egg.GetComponent<OstrichEgg>();
            eggScript.InitializeEggData(enemyPrefab, _data.eggRespawnTime);

            if (other != null)
            {
                Vector3 forceDir = (transform.position - other.transform.position).normalized;
                eggScript.rb.AddForce(new Vector2(Mathf.Sign(forceDir.x) * eggScript.baseForce, forceDir.y * eggScript.baseForce / 2), ForceMode2D.Impulse);
            }

            _spawnedEgg = true;
            ScoreManager.OnScoreGained(_data.pointsOnDeath, transform.position);
            
            _animator.SetTrigger("dieTrigger");
            AudioManager.PlaySound(SoundType.ENEMYDEATH, .5f);

            GetComponent<CapsuleCollider2D>().enabled = false;
            _rb.bodyType = RigidbodyType2D.Static;
            DOTween.Sequence().AppendInterval(2f/3f).OnComplete(() => Destroy(gameObject));
            return;
        }

        PlayerOstrichInput player = GetComponent<PlayerOstrichInput>();
        if (!player) return;
        if (player._invicible) return;
        
        _animator.SetTrigger("dieTrigger");
        AudioManager.PlaySound(SoundType.PLAYERDEATH, .5f);
        player.OnPlayerDeath();
    }
}
