using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;


[RequireComponent(typeof(OstrichMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class AIOstrichController : MonoBehaviour
{
    [SerializeField] private OstrichEntityData _data;
    [SerializeField] private Animator _animator;
    private Rigidbody2D _rb;
    private OstrichMovement _ostrichMovement;
    
    private Transform _player1;
    private Transform _player2;
    private Transform followingPlayer;

    private bool _freeze;

    private float _xCheckTimer;
    private float _yCheckTimer;
    private float _lavaCheckTimer;

    private float _currentJumpCooldown;
    private float _currentJumpCooldownTimer;
    private bool _jumpNextFrame;

    private float _spamJumpCooldown = .3f;
    private float  _spamJumpCooldownTimer;
    private bool _spamJump;

    private float _xDir;
    private float _yDir;
    
    private bool _changeXDir;
    private Vector2 previousFrameVel;

    private bool followPlayer1;

    void OnEnable()
    {
        PlayerOstrichInput.OnFinalGameOver += () => _freeze = true;
    }

    void OnDisable()
    {
        PlayerOstrichInput.OnFinalGameOver -= () => _freeze = true;
    }

    void Awake()
    {
        EnemyWaveManager.spawnedEnemies.Add(gameObject);

        _ostrichMovement = GetComponent<OstrichMovement>();
        _rb = GetComponent<Rigidbody2D>();

        var players = FindObjectsByType(typeof(PlayerOstrichInput), FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.GameObject().name == "Player1")
                _player1 = player.GameObject().transform;
            else if (player.GameObject().name == "Player2")
                _player2 = player.GameObject().transform;
        }

        // Spawn animation
        _freeze = true;
        SpawnAnimation();
    }

    void SpawnAnimation()
    {
        SpriteRenderer spriteRenderer = _animator.GetComponent<SpriteRenderer>();
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        spriteRenderer.size = new Vector2(spriteRenderer.size.x, 0);
        
        DOTween.Sequence()
        .Append(DOTween.To(() => spriteRenderer.size.y, x => spriteRenderer.size = new Vector2(spriteRenderer.size.x, x), 2.25f, .4f))
        .OnComplete(() =>
        {
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            _freeze = false;
        });
    }

    void OnDestroy()
    {
        EnemyWaveManager.spawnedEnemies.Remove(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _xCheckTimer = _data.xCheckFrequency;
        _yCheckTimer = _data.yCheckFrequency;
        _currentJumpCooldown = RollJumpCooldown();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_player1 && !_player2 || _freeze) return;
        
        if ((_player1.position - transform.position).magnitude < (_player2.position - transform.position).magnitude)
            followingPlayer = _player1;
        else followingPlayer = _player2;

        // Increment timers
        _xCheckTimer += Time.deltaTime;
        _yCheckTimer += Time.deltaTime;
        _currentJumpCooldownTimer += Time.deltaTime;
        _lavaCheckTimer += Time.deltaTime;
        _spamJumpCooldownTimer += Time.deltaTime;

        // Perform axes checks
        if (_xCheckTimer > _data.xCheckFrequency)
        {
            bool accurateFollow = Random.Range(0, 100) < _data.xCheckAccuracy;
            _xDir = (accurateFollow ^ followingPlayer.position.x > transform.position.x) ? -1 : 1;
            _xCheckTimer = 0;
            _changeXDir = true;
        }
        if (_yCheckTimer > _data.yCheckFrequency)
        {
            bool accurateFollow = Random.Range(0, 100) < _data.yCheckAccuracy;
            _yDir = (accurateFollow ^ followingPlayer.position.y > transform.position.y) ? -1 : 1;
            _yCheckTimer = 0;
        }
        
        // Lava check
        if (transform.position.y < -7.85f && _rb.linearVelocity.y < 0 && _lavaCheckTimer > _data.lavaCheckFrequency && Random.Range(0, 100) < _data.lavaCheckAccuracy)
        {
            Debug.Log("enemy jumped because of lava underneath");
            _spamJump = true;
        }

        if (_spamJump && (transform.position.y > -7.5f || _rb.linearVelocity.y > 0))
            _spamJump = false;
        
        // Jump cooldown
        if (_currentJumpCooldownTimer >= _currentJumpCooldown)
        {
            _jumpNextFrame = true;
            _currentJumpCooldownTimer = 0;
            _lavaCheckTimer = 0;
            _currentJumpCooldown = RollJumpCooldown();
        }

        AnimationLogic();
    }

    void FixedUpdate()
    {
        if (!_player1 && !_player2 || _freeze) return;

        if (_changeXDir)
        {
            _ostrichMovement.Move(_xDir);
            _changeXDir = false;
        }
        else // Move along acceleration (so change direction when bouncing against walls/ceiling)
            _ostrichMovement.Move(Mathf.Sign((_rb.linearVelocity.x - previousFrameVel.x) / Time.fixedDeltaTime));
        
        if (_jumpNextFrame)
        {
            _ostrichMovement.Jump();
            _jumpNextFrame = false;
        }

        if (_spamJump && _spamJumpCooldownTimer >= _spamJumpCooldown)
        {
            _ostrichMovement.Jump();
            _spamJumpCooldownTimer = 0;
        }

        previousFrameVel = _rb.linearVelocity;
    }

    void AnimationLogic()
    {
        _animator.SetFloat("xInput", _xDir);

        // Flip if xScale not updated
        if (_xDir >= .05f && _animator.transform.localScale.x < -.05f || _xDir <= -.05f && _animator.transform.localScale.x > .05f)
            _animator.transform.localScale *= new Vector2(-1, 1);

        // if not skidding, and vel not the same as x flip, re-flip player
        float xVel = _rb.linearVelocity.x;
        bool skid = _xDir < -.05f && xVel > .05f || _xDir > .05f && xVel < -.05f;
        if (!skid && (_animator.transform.localScale.x < -.05f && xVel > .05f || _animator.transform.localScale.x > .05f && xVel < -.05f))
        {
            _animator.SetFloat("xInput", Mathf.Sign(xVel));
            _animator.transform.localScale *= new Vector2(-1, 1);
        }
        
        // Finally, if not moving, flip towards xVel
        if (Mathf.Abs(_xDir) < .05f)
            _animator.transform.localScale = new Vector2(Mathf.Sign(xVel), _animator.transform.localScale.y);

        _animator.SetFloat("runSpeed", xVel * .15f);
    }

    // Cooldown between jump is randomized, and depends on the enemy type (harder enemy --> shorter cooldowns)
    float RollJumpCooldown()
    {
        if (_yDir > .001f)
            return RandomGaussian(_data.accurateJumpFrequency.x, _data.accurateJumpFrequency.y);
        return RandomGaussian(_data.inaccurateJumpFrequency.x, _data.inaccurateJumpFrequency.y);
    }

    /// <summary>
    /// Random distribution function, obtains value within minValue and maxValue at 99.7% chance, clamps otherwise
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        float u, v, S;

        do
        {
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;

        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }
}
