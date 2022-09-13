using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gamemanager : MonoBehaviour
{
    CameraFollow cf;
    public float score = 0;
    public Text _scoretext, _cointext;
    public float coin = 0;

    Player player;

    public GameObject[] character;

    public GameObject GameOverUI;
    public SoundController sc;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1;
        player = GameObject.FindObjectOfType<Player>();
        cf = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraFollow>();
    }

    // Update is called once per frame
    void Update()
    {
        checkKarakter();
        print(player.m_skin);
        //if(score !=0 && score % 2 == 0)
        //{
        //    cf.followCamera(true);
        //}
    }
    public void cetakScore(bool cetak)
    {
        score = score+1;
        _scoretext.text = ""+score;
        if (score != 1)
        {
            print("kamera jalan!");
            cf.followCamera(true);
        }
    }

    public void cetakCoin(bool cetak)
    {
        coin = coin + 1;
        _cointext.text = ""+ coin; 
    }

    public void gameOver(bool gameover)
    {
        player.checkHighScore((int)score);
        player.mergeCoin((int)coin);
        GameOverUI.SetActive(true);
        sc.musicGameplay(true);
    }
    public void checkKarakter()
    {
      character[player.m_skin].SetActive(true);

    }
}
