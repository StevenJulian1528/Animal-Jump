using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    Player player;
    // Start is called before the first frame update
    void Start()
    {

        Time.timeScale = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void btnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void btnMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
