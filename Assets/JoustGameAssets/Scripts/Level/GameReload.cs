using UnityEngine;
using DG.Tweening;
using System;

public class GameReload : MonoBehaviour
{
    public static Action OnGameReset;

    [SerializeField] private Vector3 basePlayer1Pos;
    [SerializeField] private Vector3 basePlayer2Pos;
    [SerializeField] private Transform player1Transform;
    [SerializeField] private Transform player2Transform;

    [SerializeField] private Transform leftFire;
    [SerializeField] private Transform rightFire;
    [SerializeField] private Transform leftPlatform;
    [SerializeField] private Transform rightPlatform;
    
    void OnEnable()
    {
        PlayerOstrichInput.OnFinalGameOver += () => DOTween.Sequence().AppendInterval(3).OnComplete(ResetGame);
    }

    void OnDisable()
    {
        PlayerOstrichInput.OnFinalGameOver -= () => DOTween.Sequence().AppendInterval(3).OnComplete(ResetGame);
    }

    private void ResetGame()
    {
        OnGameReset();
        
        player1Transform.localPosition = basePlayer1Pos;
        player1Transform.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (player2Transform) player2Transform.localPosition = basePlayer2Pos;

        leftFire.localPosition = new Vector2(-16.5f, -9.625f);
        rightFire.localPosition = new Vector2(16.5f, -9.625f);
        
        leftPlatform.localPosition = new Vector2(-13, -9.125f);
        leftPlatform.localScale = Vector2.one;
        rightPlatform.localPosition = new Vector2(313, -9.125f);
        leftPlatform.localScale = new Vector2(-1, 1);
        
        leftPlatform.GetComponent<Animator>().SetTrigger("resetBridge");
        rightPlatform.GetComponent<Animator>().SetTrigger("resetBridge");
        leftFire.GetComponent<Animator>().SetTrigger("resetFire");
        rightFire.GetComponent<Animator>().SetTrigger("resetFire");
    }
}
