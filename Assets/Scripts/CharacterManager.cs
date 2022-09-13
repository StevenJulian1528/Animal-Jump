using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public Movement mv;
    public Gamemanager gm;
    float time = 3f;
    bool slowpot = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (slowpot)
        {
            time = time - Time.deltaTime;
            print(time);

            if (time <= 0)
            {
                mv.speed = 3;
                time = 3f;
                slowpot = false;
            }
        }
    }

    public void slowPot(bool slowpot)
    {
        this.slowpot = slowpot;
        if(slowpot)
        {
            mv.speed = 0.5f;

        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals("gameover"))
        {
            gm.gameOver(true);
        }


    }
}
