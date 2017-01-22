using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {

    public GameObject[] hazards;
    public Vector3 spawnPos;
    public int hazardCount;
    public float spawnWait;
    public float startWait;
    public float waveWait;

    public Text levelText;
    public Text restartText;
    public Text gameOverText;

    private bool gameOver;
    private bool restart;
    private int level;

    private Quaternion spawnRotation;

    void Start()
    {
        gameOver = false;
        restart = false;
        level = 0;
        restartText.text = "";
        gameOverText.text = "";
        levelText.text = "";
        UpdateLevel();
        StartCoroutine(SpawnWaves());
    }

    void Update()
    {
        if (restart)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene("main");
            }
        }
    }


    IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(startWait);
        while (true)
        {
            for (int i = 0; i < hazardCount; i++)
            {
                GameObject hazard = hazards[Random.Range(0, hazards.Length)];
                Vector3 spawnPosition = new Vector3(Random.Range(-spawnPos.x, spawnPos.x), spawnPos.y, spawnPos.z);
                Instantiate(hazard, spawnPosition, spawnRotation);
                yield return new WaitForSeconds(spawnWait);
            }
            UpdateLevel();
            yield return new WaitForSeconds(waveWait);

            if (gameOver)
            {
                restartText.text = "Press 'R' for Restart";
                restart = true;
                break;
            }
        }
    }
    
    void UpdateLevel()
    {
        level++;
        levelText.text = "Level: " + level;
    }

    public void GameOver()
    {
        gameOverText.text = "You got splashed!";
        gameOver = true;
    }
}
