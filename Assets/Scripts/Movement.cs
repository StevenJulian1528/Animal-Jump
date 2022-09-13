using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private Rigidbody2D rb;
    public float speed;
    public bool isRight = false;
    public bool isLeft = true;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        print("ini movement speed" + speed);
        if (isRight){
            transform.Translate(Vector2.right * (speed * Time.deltaTime));
        }
        if (isLeft) {
            transform.Translate(Vector2.left * (speed * Time.deltaTime));
        }

        //print("movement right"+isRight);
        //print("movement left" + isLeft);

    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals("pembatas") && !isRight)
        {
                isRight = true;
                isLeft = false;
        }
        else if (collision.gameObject.tag.Equals("pembatas") && !isLeft)
        {
                isLeft = true;
                isRight = false;
        }

    }
}