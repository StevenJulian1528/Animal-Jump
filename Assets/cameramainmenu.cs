using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameramainmenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera.main.aspect = 9f / 18f;
    }
}
