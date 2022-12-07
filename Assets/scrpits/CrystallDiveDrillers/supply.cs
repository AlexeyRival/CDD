using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class supply : NetworkBehaviour
{
    [SyncVar]
    public int amount = 4;
    private bool moving=true;
    [SyncVar]
    public Vector3 finalpoint;
    public void SetVector(Vector3 point) 
    {
        finalpoint = point;
    }
    private void Start()
    {
        MissionControlVoice.only.PlayReplica(18);
    }
    private void Update()
    {
        if (!moving)
        {
            if (amount <= 0) {    
                enabled = false;
                GetComponent<Light>().enabled = false;
            }
        }
        else
        if (transform.position.y > finalpoint.y)
        {
            transform.Translate(0, -4f*Time.deltaTime, 0);
        }
        else { 
            moving = false;
            transform.GetChild(0).GetComponent<Animation>().Play();
        }
    }
}
