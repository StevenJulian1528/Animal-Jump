using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private int coin, highscore, skinUsed = 0;
    private int[,] shopItem = { { 0, 0, 50 }, { 1, 1, 0 }, { 2, 0, 200 } };
    public int m_coin
    {
        get { return coin; }
        set { coin = value; }
    }

    public int m_highScore
    {
        get { return highscore; }
        set { highscore = value; }
    }
    public int m_skin
    {
        get { return skinUsed; }
        set { skinUsed = value; }
    }

    public void buyItem(int skin)
    {
        shopItem[skin, 1] = 1;
        coin = coin - shopItem[skin, 2];
        skinUsed = skin;
        SavePlayer();
        print(skinUsed);
    }

    public int[,] m_shopItem
    {
        get { return shopItem; }
        set { shopItem = value; }
    }

    public void mergeCoin(int coinGet)
    {
        m_coin = m_coin + coinGet;
        SavePlayer();
    }
    public void checkHighScore(int scoreGet)
    {
        if(scoreGet>highscore)
        {
            highscore = scoreGet;
            SavePlayer();
        }
    }
    public void SavePlayer()
    {
        SaveSystem.SavePlayer(this);
    }
    public void LoadPlayer()
    {
        playerData data = SaveSystem.LoadPlayer();

        coin = data.coin;
        highscore = data.highscore;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                shopItem[i, j] = data.shopItem[i, j];
            }
        }
        m_skin = data.selectedSkin;
    }

    private void Update()
    {
        LoadPlayer();
    }
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void changeSkin(int skin)
    {
        m_skin = skin;
        print(m_skin);
        SavePlayer();

    }
}
