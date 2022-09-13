using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spellSlow : MonoBehaviour
{
    CharacterManager cm;
    // Start is called before the first frame update
    void Start()
    {
        cm = GameObject.FindGameObjectWithTag("player").GetComponent<CharacterManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag.Equals("player"))
        {
            cm.slowPot(true);
            Destroy(gameObject);
        }
    }
}
