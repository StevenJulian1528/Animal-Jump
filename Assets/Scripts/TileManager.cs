using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    float posisiY = 4.30f;
    bool spawnnow = false;
    public Gamemanager gm;
    public GameObject[] tiles;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(gm.score % 5 == 1)
        {

            spawnTiles();
        }
    }

    void spawnTiles()
    {
        if(spawnnow)
        {
            print("spawnnow jalan");
            posisiY = posisiY + 3.25f;
            int x1 = Random.Range(0, 5);
            Instantiate(tiles[x1], new Vector3(-0.05f, posisiY, 0), Quaternion.identity);
            if (spawnnow)
            {
                posisiY = posisiY + 3.25f;
                int x2 = Random.Range(0, 5);
                Instantiate(tiles[x2], new Vector3(-0.05f, posisiY, 0), Quaternion.identity);

                if(spawnnow)
                {
                    posisiY = posisiY + 3.25f;
                    int x3 = Random.Range(0, 5);
                    Instantiate(tiles[x3], new Vector3(-0.05f, posisiY, 0), Quaternion.identity);
                    spawnnow = false;

                }
            }
        }
    }

    public void getParameterCreateTile(bool create)
    {
        print("spawnnow adalah "+spawnnow);
        spawnnow = true;
    }
}
