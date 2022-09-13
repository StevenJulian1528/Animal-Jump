using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class coin : MonoBehaviour
{
     Gamemanager gm;
    SoundController sc;
    // Start is called before the first frame update
    void Start()
    {
        sc = GameObject.FindGameObjectWithTag("SoundController").GetComponent<SoundController>();
        gm = GameObject.FindGameObjectWithTag("Gamemanager").GetComponent<Gamemanager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("player"))
        {
            sc.coin(true);
            print("kena coin");
            gm.cetakCoin(true);
            Destroy(gameObject);
        }
    }
}
