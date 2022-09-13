using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CetakCoin : MonoBehaviour
{
    Gamemanager gm;
    // Start is called before the first frame update
    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("Gamemanager").GetComponent<Gamemanager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag.Equals("player"))
        {
            gm.cetakCoin(true);
            Destroy(gameObject);
        }
    }
}
