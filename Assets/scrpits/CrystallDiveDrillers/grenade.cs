using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class grenade : NetworkBehaviour
{
    public GameObject explosion,destroyer;
    public bool isContact=true;
    public int explosioncount=1;
    public float time;
    public int maxcount = 4;
    public TextMesh timer;
    private void OnCollisionEnter(Collision collision)
    {
        if (isContact)
        {
            if (!collision.gameObject.CompareTag("Player"))
            {
                Destroy(gameObject);
            }
        }
        else
        {
            if (!collision.gameObject.CompareTag("Player"))
            {
                GetComponent<Rigidbody>().isKinematic = true;
                transform.LookAt(Camera.main.transform.position);
            //    transform.parent = collision.transform;
            }
        }
    }
    private void Update()
    {
        if (!isContact) 
        {
            if (time > 0)
            {
                time -= Time.deltaTime;
                timer.text =time.ToString("#.##");// "00:" + ((time >= 10) ? ((int)time + "") : ("0" + ((int)time)));
            }
            else 
            {
                Destroy(gameObject);
            }
        }
    }
    private bool quitting;
    private void OnApplicationQuit()
    {
        quitting = true;
    }
    private void OnDestroy()
    {
        if (quitting) { return; }
        int k = explosioncount;
        for (int i = 0; i < k; ++i)
        {
            if (isServer)
            {
                GameObject ob = Instantiate(destroyer, transform.position, transform.rotation);
                if (!isContact) { ob.transform.localScale *= 2f; }
                Destroy(ob, 0.2f);
                NetworkServer.Spawn(ob);
            }
            Destroy(Instantiate(explosion, transform.position, transform.rotation), 1f);
        }
    }
}
