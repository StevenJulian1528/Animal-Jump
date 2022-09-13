using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Text textGold,textGoldShop;
    Player data;
    public GameObject shop, mainmenu;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1;
        data = GameObject.FindGameObjectWithTag("PlayerData").GetComponent<Player>();

    }

    // Update is called once per frame
    void Update()
    {
        textGold.text = "" + data.m_coin;
        textGoldShop.text = "" + data.m_coin;
    }
    public void btnPlay()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void btnShop()
    {
        shop.SetActive(true);
        mainmenu.SetActive(false);
    }
    public void btnBack()
    {
        shop.SetActive(false);
        mainmenu.SetActive(true);
    }



}
