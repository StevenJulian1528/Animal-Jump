using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    public AudioSource audioCoin, audioGameplay;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void coin(bool audio)
    {
        audioCoin.Play();
    }
    public void musicGameplay(bool audio)
    {
        audioGameplay.Stop();
    }
}
