using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class shopManager : MonoBehaviour
{
    public Button btnLandak,btnDuck,btnOwl;
    public Text txtLandak, txtDuck, txtOwl;
    Player player;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindObjectOfType<Player>();
        if (player.m_skin==1)
        {
            txtDuck.text = "Used!";

            if (player.m_shopItem[0, 1] == 1)
            {
                txtLandak.text = "Not Used!";
            }
            if (player.m_shopItem[2, 1] == 1)
            {
                txtOwl.text = "Not Used!";
            }
            if (player.m_shopItem[0, 1] == 0)
            {
                txtLandak.text = "Not Buyed!";
            }
            if (player.m_shopItem[2, 1] == 0)
            {
                txtOwl.text = "Not Buyed!";
            }
        }


        if (player.m_skin == 0)
        {
            txtLandak.text = "Used!";

            if (player.m_shopItem[2, 1] == 1)
            {
                txtOwl.text = "Not Used!";
            }
            if (player.m_shopItem[2, 1] == 0)
            {
                txtOwl.text = "Not Buyed!";
            }
            txtDuck.text = "Not Used!";
        }

        if(player.m_skin == 2)
        {
            txtOwl.text = "Used!";

            if (player.m_shopItem[0, 1] == 1)
            {
                txtLandak.text = "Not Used!";
            }
            if (player.m_shopItem[0, 1] == 0)
            {
                txtLandak.text = "Not Buyed!";
            }
            txtDuck.text = "Not Used!";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void btnLandakClicked()
    {
        if (player.m_shopItem[0, 1] != 1)
        {
            if (player.m_coin >= player.m_shopItem[0, 2])
            {
                player.buyItem(0);
                player.changeSkin(0);
                txtLandak.text = "Used!";
                txtDuck.text = "Not Used!";
                if (player.m_shopItem[2, 1] == 1)
                {
                    txtOwl.text = "Not Used!";
                }
                else if (player.m_shopItem[2, 1] == 0)
                {
                    txtOwl.text = "Not Buyed!";
                }

            }
        }

        if (player.m_shopItem[0, 1] == 1)
        {
            player.changeSkin(0);
            txtLandak.text = "Used!";
            txtDuck.text = "Not Used!";
            if (player.m_shopItem[2, 1] == 1)
            {
                txtOwl.text = "Not Used!";
            }
            else if (player.m_shopItem[2, 1] == 0)
            {
                txtOwl.text = "Not Buyed!";
            }
        }


    }

    public void btnOwlClicked()
    {
        if (player.m_shopItem[2, 1] == 0)
        {
            if (player.m_coin >= player.m_shopItem[2, 2])
            {
                player.buyItem(2);
                player.changeSkin(2);
                txtOwl.text = "Used!";
                txtDuck.text = "Not Used!";
                if (player.m_shopItem[2, 1] == 1)
                {
                    txtLandak.text = "Not Used!";
                }
                else if (player.m_shopItem[2, 1] == 0)
                {
                    txtLandak.text = "Not Buyed!";
                }

            }
        }

        if (player.m_shopItem[2, 1] == 1)
        {
            player.changeSkin(2);
            txtOwl.text = "Used!";
            txtDuck.text = "Not Used!";
            if (player.m_shopItem[2, 1] == 1)
            {
                txtLandak.text = "Not Used!";
            }
            else if (player.m_shopItem[2, 1] == 0)
            {
                txtLandak.text = "Not Buyed!";
            }

        }
    }
     

    public void btnDuckClicked()
    {
        player.changeSkin(1);
        txtDuck.text = "Used!";

        if(player.m_shopItem[0, 1] == 1)
        {
            txtLandak.text = "Not Used!";
        }
        if (player.m_shopItem[2, 0] == 1)
        {
            txtOwl.text = "Not Used!";
        }
        if (player.m_shopItem[0, 1] == 0)
        {
            txtLandak.text = "Not Buyed!";
        }
        if (player.m_shopItem[2, 0] == 0)
        {
            txtOwl.text = "Not Buyed!";
        }
    }
}
