using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("Cam zoom")]
    [SerializeField] Transform _mainCam;
    [SerializeField] Vector3 _zoomedOutCamPos;
    [SerializeField] Vector3 _zoomedInCamPos;

    [Header("TV Screen")] 
    [SerializeField] private MeshRenderer tvScreen;
    [SerializeField] private Material tvGameRender;
    [SerializeField] private Texture joustMenuTex;
    [SerializeField] private Texture joustGameTex;
    [SerializeField] private VideoPlayer introSequenceVideo;
    
    private bool _zoomedIn = false;
    private bool _gameLaunched = false;

    public void OnConfirm(InputAction.CallbackContext context)
    {
        if (context.performed && !_zoomedIn)
        {
            Debug.Log("zooming in");
            
            Vector3 newCamPos = _zoomedIn ? _zoomedOutCamPos : _zoomedInCamPos;
            _zoomedIn = !_zoomedIn;

            DOTween.Sequence().Append(_mainCam.DOLocalMove(newCamPos, .9f))
            .AppendInterval(1)
            .OnComplete(() =>
            {
                tvScreen.material = tvGameRender;
                introSequenceVideo.Play();
                tvScreen.material.SetTexture("_GameRenderTex", joustMenuTex);
            });
        }
        
        //if (context.performed) Debug.Log("game launched: " + _gameLaunched + ", same material: " + (tvScreen.material.shader == tvGameRender.shader));
        
        else if (context.performed && !_gameLaunched && tvScreen.material.shader == tvGameRender.shader)
        {
            Debug.Log("launching game");
            _gameLaunched = true;
            
            tvScreen.material.SetTexture("_GameRenderTex", joustGameTex);
            introSequenceVideo.Stop();
            EnemyWaveManager.OnGameLaunched();
        }
    }
}
