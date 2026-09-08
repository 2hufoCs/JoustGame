using TMPro;
using UnityEngine;
using System;
using DG.Tweening;

public class ScoreManager : MonoBehaviour
{
    private int _currentScore;
    private int _highscore;

    [SerializeField] private TextMeshProUGUI _currentScoreText;
    [SerializeField] private GameObject _scorePopupPrefab;
    [SerializeField] private Transform worldCanvas;
    [SerializeField] private GameObject _gameOverText;

    public static Action<int, Vector3> OnScoreGained;

    void OnEnable()
    {
        OnScoreGained += GainScore;
        PlayerOstrichInput.OnFinalGameOver += GameOver;
        EnemyWaveManager.OnGameLaunched += () => _gameOverText.SetActive(false);
    }

    void OnDisable()
    {
        OnScoreGained -= GainScore;
        PlayerOstrichInput.OnFinalGameOver -= GameOver;
        EnemyWaveManager.OnGameLaunched -= () => _gameOverText.SetActive(false);

    }

    private void GameOver()
    {
        DOTween.Sequence().AppendInterval(1).OnComplete(() => _gameOverText.SetActive(true));
        AudioManager.PlaySound(SoundType.GAMEOVER);
    }

    private void GainScore(int score, Vector3 posToPopup)
    {
        _currentScore += score;
        FormatScore(_currentScore);

        Vector2 randomOffset = posToPopup + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(-1.5f, 1.5f));
        GameObject scorePopup = Instantiate(_scorePopupPrefab, randomOffset, Quaternion.identity, worldCanvas);
        scorePopup.GetComponent<TextMeshProUGUI>().text = score.ToString();
        scorePopup.transform.localScale = Vector2.zero;
        
        AudioManager.PlaySound(SoundType.GAINSCORE, .9f);

        DOTween.Sequence()
            .Append(scorePopup.transform.DOScale(1, .2f))
            .Append(scorePopup.transform.DOMoveY(scorePopup.transform.position.y + .4f, .5f))
            .Join(scorePopup.transform.DOShakeRotation(.5f, 25, 50))
            .Append(scorePopup.transform.DOScale(0, .2f))
            .OnComplete(() => Destroy(scorePopup));
    }

    private void FormatScore(int score)
    {
        int zerosToAdd = 6 - score.ToString().Length;
        _currentScoreText.text = "";
        for (int i = 0; i < zerosToAdd; i++)
        {
            _currentScoreText.text += "0";
        }

        _currentScoreText.text += score.ToString();
    }
}