using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TouchJump : MonoBehaviour
{
    Rigidbody2D rb;
    public float jumpForce = 600f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && rb.velocity.y <= 0.3 || Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && rb.velocity.y <= 0.3)
        {
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Force);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag.Equals("end"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
