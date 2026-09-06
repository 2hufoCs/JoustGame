using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.LowLevelPhysics2D;

[RequireComponent(typeof(OstrichMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerOstrichInput : MonoBehaviour
{
    public static List<PlayerOstrichInput> players = new();
    public int playerId;
    
    [SerializeField] private int lives;
    [SerializeField] private Transform healthUI;
    [SerializeField] private GameObject healthSprite;
    [SerializeField] private Transform _playerSpawnpoint;
    private OstrichMovement _ostrichMovement;
    private Rigidbody2D _rb;
    
    [SerializeField] private Animator _animator;
    [SerializeField] private float _jumpCooldown;
    private float _jumpCooldownTimer;

    private bool _isMoving;
    private bool _isJumping;

    private int _moveDir;

    private bool _freeze;
    public bool _invicible;
    private bool _dead;

    // Int parameter is which player died
    public Action OnPlayerDeath;
    public Action OnPlayerGameOver;
    public Action OnPlayerFreeze;
    public static Action OnFinalGameOver;


    void Freeze()
    {
        _freeze = true;
        
        _isMoving = false;  
        _moveDir = 0;
        _isJumping = false;
    }

    void Ded()
    {
        _dead = true;
        players.Remove(this);
        if (players.Count == 0)
            OnFinalGameOver();
        
        GetComponent<OstrichMovement>().enabled = false;
        GetComponent<CapsuleCollider2D>().enabled = false;
        _animator.GetComponent<SpriteRenderer>().enabled = false;
        Freeze();
    }

    void Start()
    {
        _ostrichMovement = GetComponent<OstrichMovement>();
        _rb = GetComponent<Rigidbody2D>();
        
        Freeze();
    }

    void OnEnable()
    {
        OnPlayerDeath += Respawn;
        OnPlayerGameOver += Ded;
        OnPlayerFreeze += Freeze;
        
        EnemyWaveManager.OnGameLaunched += LaunchNewGame;
    }

    void OnDisable()
    {
        OnPlayerDeath -= Respawn;
        OnPlayerGameOver -= Ded;
        OnPlayerFreeze -= Freeze;

        EnemyWaveManager.OnGameLaunched -= LaunchNewGame;
    }

    void Update()
    {
        if (_freeze)
            return;
            
        AnimationLogic();
    }

    void FixedUpdate()
    {
        if (_freeze) return;
        
        _ostrichMovement.Move(_moveDir);  
        
        if (_isJumping && _jumpCooldownTimer >= _jumpCooldown)
        {
            _ostrichMovement.Jump();
            _jumpCooldownTimer = 0;
        }

        _jumpCooldownTimer += Time.fixedDeltaTime;
    }

    void LaunchNewGame()
    {
        players.Add(this);
        _dead = false;
        lives = 3;
        for (int i = 0; i < lives + 1; i++)
            Instantiate(healthSprite, healthUI);
        
        GetComponent<OstrichMovement>().enabled = true;
        GetComponent<CapsuleCollider2D>().enabled = true;
        _animator.GetComponent<SpriteRenderer>().enabled = true;
        _freeze = false;
    }

    void AnimationLogic()
    {
        _animator.SetFloat("xInput", _moveDir);

        if (_moveDir >= .05f && _animator.transform.localScale.x < -.05f || _moveDir <= -.05f && _animator.transform.localScale.x > .05f)
            _animator.transform.localScale *= new Vector2(-1, 1);

        // if not skidding, and vel not the same as x flip, re-flip player
        float xVel = _rb.linearVelocity.x;
        bool skid = _moveDir < -.05f && xVel > .05f || _moveDir > .05f && xVel < -.05f;
        if (!skid && (_animator.transform.localScale.x < -.05f && xVel > .05f || _animator.transform.localScale.x > .05f && xVel < -.05f))
        {
            _animator.SetFloat("xInput", Mathf.Sign(xVel));
            _animator.transform.localScale *= new Vector2(-1, 1);
        }

        _animator.SetFloat("runSpeed", xVel * .15f);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_freeze) return;
        if (context.performed)
        {
            Debug.Log("moving as player " + playerId);
            _isMoving = true;
            _moveDir = Mathf.RoundToInt(context.ReadValue<float>());
        }
        else if (context.canceled)
        {
            _isMoving = false;  
            _moveDir = 0;
        }
            
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (_freeze) return;
        if (context.performed)
        {
            _isJumping = true;       
            _jumpCooldownTimer = _jumpCooldown;    
        }
        else    
            _isJumping = false;
    }

    void Respawn()
    {
        if (_dead) return;
        
        Destroy(healthUI.GetChild(0).gameObject);
        if (lives <= 0)
        {
            OnPlayerGameOver();
            return;
        }
        lives--;
        
        DOTween.Sequence().AppendInterval(2f/3f).OnComplete(() => _animator.GetComponent<SpriteRenderer>().enabled = false);
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static;

        OnPlayerFreeze();
        _invicible = true;

        DOTween.Sequence().AppendInterval(1).OnComplete(() =>
        {
            transform.position = _playerSpawnpoint.position;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            
            _animator.GetComponent<SpriteRenderer>().enabled = true;
            _animator.SetTrigger("respawnTrigger");

            _freeze = false;
        });
        DOTween.Sequence().AppendInterval(3).OnComplete(() =>
        {
            _invicible = false;
        });
    }
}
