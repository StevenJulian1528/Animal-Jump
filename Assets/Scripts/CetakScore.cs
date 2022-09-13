using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CetakScore : MonoBehaviour
{
    Gamemanager gm;
    public bool isScored = false;
    // Start is called before the first frame update
    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("Gamemanager").GetComponent<Gamemanager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag.Equals("player") && !isScored)
        {
            isScored = true;
            gm.cetakScore(true);
        }
    }
}
