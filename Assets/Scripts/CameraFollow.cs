using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    public float damping;


    private Vector3 velocity = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        Camera.main.aspect = 9f / 18f;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = new Vector3(player.position.x + offset.x, player.position.y + offset.y, offset.z);
    }
    public void followCamera(bool followcamera)
    {
        if (followcamera)
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, (Camera.main.transform.position.y + damping), offset.z);
            //print("camera action");
            //Vector3 movePosition = offset;
            //transform.position = Vector3.SmoothDamp(transform.position, movePosition, ref velocity, damping);
            followcamera = false;

        }
    }

}
