using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CetakTile : MonoBehaviour
{
    TileManager tm;
    public bool isCetak = false;
    // Start is called before the first frame update
    void Start()
    {
        tm = GameObject.FindGameObjectWithTag("Gamemanager").GetComponent<TileManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("player") && !isCetak)
        {
            isCetak = true;
            tm.getParameterCreateTile(true);
        }
    }
}
