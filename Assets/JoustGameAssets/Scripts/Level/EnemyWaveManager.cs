using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using Random = UnityEngine.Random;

public class EnemyWaveManager : MonoBehaviour
{
    [SerializeField] private List<WaveData> _waveDatas;
    [SerializeField] private List<Transform> _spawnpoints = new();
    [SerializeField] private List<GameObject> platforms = new();
    private List<GameObject> spawnedPlatforms = new();
    
    [SerializeField] private Transform _player;
    [SerializeField] private TextMeshProUGUI _wavetxt;
    [SerializeField] private int currentWave = 0;

    [Header("Enemies")]
    [SerializeField] private GameObject[] enemyPrefab = new GameObject[4];

    [SerializeField] private Vector2 pteroMinMaxYSpawn;
    [SerializeField] private float enemySpawnInterval;
    [SerializeField] private float minDistanceSpawn;

    [Header("Eggs")] 
    [SerializeField] private GameObject eggPrefab;
    [SerializeField] private List<Transform> _eggPositions;
    public static Action<GameObject, Vector3> OnEggRespawn;

    [Header("Animations")] 
    [SerializeField] private Animator leftBridgeAnimator;
    [SerializeField] private Animator rightBridgeAnimator;
    [SerializeField] private Animator leftFireAnimator;
    [SerializeField] private Animator rightFireAnimator;


    public static List<GameObject> spawnedEnemies = new();
    private bool _spawningEnemies;
    
    public static Action OnGameLaunched;
    private bool _pause = true;

    void Update()
    {
        if (spawnedEnemies.Count == 0 && !_spawningEnemies && !_pause)
            OnNextWave();
    }

    void Start()
    {
        foreach (GameObject platform in platforms)
        {
            spawnedPlatforms.Add(platform);
        }
    }

    void OnEnable()
    {
        OnEggRespawn += SpawnSingleEnemy;
        OnGameLaunched += StartGame;
        GameReload.OnGameReset += ResetGame;
    }

    void OnDisable()
    {
        OnEggRespawn -= SpawnSingleEnemy;
        OnGameLaunched -= StartGame;
        GameReload.OnGameReset -= ResetGame;
    }

    void ResetGame()
    {
        _pause = true;
        currentWave = 0;
        
        foreach (GameObject go in spawnedEnemies)
        {
            Destroy(go);
        }
        spawnedEnemies.Clear();

        OnGameLaunched();
    }

    void StartGame()
    {
        _pause = false;
        OnNextWave();
    }

    void OnNextWave()
    {
        _spawningEnemies = true;

        if (currentWave < _waveDatas.Count) currentWave++;
        WaveData currentWaveData = _waveDatas[currentWave - 1];

        _wavetxt.text = "WAVE " + currentWave;
        _wavetxt.gameObject.SetActive(true);
        AudioManager.PlaySound(SoundType.NEXTWAVE, .9f);

        if (currentWave == 3)
            BridgeBurnAnim();
        
        //Debug.Log($"now on wave {currentWave}, type {currentWaveData.waveType}, with {currentWaveData.eggCount} eggs, {currentWaveData.bounders} bounders, {currentWaveData.hunters} hunters, and {currentWaveData.shadowLords} shadowLords");
        
        StartCoroutine(ModifyPlatforms(currentWaveData));

        DOTween.Sequence().AppendInterval(2.3f).OnComplete(() =>
        {
            _wavetxt.gameObject.SetActive(false);

            // Different actions depending on type of wave
            if (currentWaveData.waveType.HasFlag(WaveType.Normal) ||
                currentWaveData.waveType.HasFlag(WaveType.Pterodactyl))
                StartCoroutine(SpawnEnemies(currentWaveData));
            else if (currentWaveData.waveType.HasFlag(WaveType.Egg))
                SpawnEggWave(currentWaveData);
        });
    }

    void BridgeBurnAnim()
    {
        leftBridgeAnimator.SetTrigger("burnBridge");
        rightBridgeAnimator.SetTrigger("burnBridge");
        leftFireAnimator.SetTrigger("moveFire");
        rightFireAnimator.SetTrigger("moveFire");

    }

    IEnumerator ModifyPlatforms(WaveData data)
    {
        for (int i = 0; i < platforms.Count; i++)
        {
            float firstDuration = data.missingPlatforms.Contains(i+1) && spawnedPlatforms.Contains(platforms[i]) ? .1f : 
                !data.missingPlatforms.Contains(i+1) && !spawnedPlatforms.Contains(platforms[i]) ? .4f : -1;

            if (firstDuration < 0)
                continue;
            else if (firstDuration < .11f)
                spawnedPlatforms.Remove(platforms[i]);
            else
                spawnedPlatforms.Add(platforms[i]);
            
            
            // Spawn/despawn platform
            DOTween.Sequence()
                .Append(platforms[i].transform.DOScaleX(1.05f, firstDuration))
                .Append(platforms[i].transform.DOScaleX(firstDuration < .11f ? 0 : 1, firstDuration < .11f ? .4f : .1f));

            yield return new WaitForSeconds(.3f);
        }
    } 

    IEnumerator SpawnEnemies(WaveData waveData)
    {
        List<Transform> availableSpawns = GetSpawnPoints(waveData);

        int enemiesCount = waveData.bounders + waveData.hunters + waveData.shadowLords;
        for (int j = 0; j < enemiesCount; j++)
        {
            if (j < waveData.bounders)
                Instantiate(enemyPrefab[0], availableSpawns[Random.Range(0, availableSpawns.Count)].position, Quaternion.identity, transform);  
            else if (j < waveData.bounders + waveData.hunters)
                Instantiate(enemyPrefab[1], availableSpawns[Random.Range(0, availableSpawns.Count)].position, Quaternion.identity, transform);  
            else
                Instantiate(enemyPrefab[2], availableSpawns[Random.Range(0, availableSpawns.Count)].position, Quaternion.identity, transform); 
            yield return new WaitForSeconds(enemySpawnInterval);
        }
        
        // also spawn pterodactyl if there's one
        for (int i = 0; i < waveData.pteros + 1; i++)
        {
            Debug.Log("about to spawn ptero");
            if (i != 0)
            {
                Vector2 spawnPos = new Vector2(68, Random.Range(pteroMinMaxYSpawn.x, pteroMinMaxYSpawn.y));
                Instantiate(enemyPrefab[3], spawnPos,  Quaternion.identity, transform);
            }

            if (i == waveData.pteros) break;
            yield return new WaitForSeconds(Random.Range(0, 15));
        }
        
        _spawningEnemies = false;
    }

    void SpawnSingleEnemy(GameObject enemyToSpawn, Vector3 pos)
    {
        Instantiate(enemyToSpawn, pos, Quaternion.identity, transform);  
    }

    List<Transform> GetSpawnPoints(WaveData data)
    {
        List<Transform> availableSpawns = new();
        for (int i = 0; i < _spawnpoints.Count; i++)
        {
            if (i == 3 && data.missingPlatforms.Contains(i))
                continue;
            
            if ((_player.position - _spawnpoints[i].position).magnitude > minDistanceSpawn)
                availableSpawns.Add(_spawnpoints[i]);
        }
        return availableSpawns;
    }

    void SpawnEggWave(WaveData data)
    {
        for (int i = 0; i < _eggPositions.Count; i++)
        {
            GameObject newEgg = Instantiate(eggPrefab,  _eggPositions[i].position, Quaternion.identity, transform);
            OstrichEgg eggScript = newEgg.GetComponent<OstrichEgg>();
            eggScript.InitializeEggData(data.ostrichToRespawn, data.eggRespawnCooldown);
        }
        _spawningEnemies = false;
    }
}

[Flags] public enum WaveType { Normal = 1, Egg = 1 << 1, Pterodactyl = 1 << 2}

[Serializable]
public class WaveData
{
    [Header("General info")]
    public WaveType waveType;
    public List<int> missingPlatforms; 

    [Header("Enemy-related data")]

    [HideIf("waveType", WaveType.Egg), AllowNesting] public int bounders;
    [HideIf("waveType", WaveType.Egg), AllowNesting] public int hunters;
    [HideIf("waveType", WaveType.Egg), AllowNesting] public int shadowLords;
    [HideIf("waveType", WaveType.Egg), AllowNesting] public int pteros;

    [ShowIf("waveType", WaveType.Egg), AllowNesting] public int eggCount;
    [ShowIf("waveType", WaveType.Egg), AllowNesting] public float eggRespawnCooldown;
    [ShowIf("waveType", WaveType.Egg), AllowNesting] public GameObject ostrichToRespawn;
}