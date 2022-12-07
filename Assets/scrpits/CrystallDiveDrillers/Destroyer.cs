using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyer : MonoBehaviour
{
    public List<Resource> resources;
    public GameObject effector;
    private Vector3 oldpos = new Vector3(-1,-1,-1);
    private void Start()
    {
        Destroy(Instantiate(effector, transform.position, transform.rotation), 2);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.CompareTag("Resource"))
        {
            int id = int.Parse(collision.collider.gameObject.name);
            resources[id].SpawnItem(transform.position);
            Destroy(collision.collider.gameObject);
        }
        if (collision.collider.gameObject.CompareTag("Decoration"))
        { 
            Destroy(collision.collider.gameObject);
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.gameObject.CompareTag("Resource"))
        {
            int id = int.Parse(collision.collider.gameObject.name);
            resources[id].SpawnItem(transform.position);
            Destroy(collision.collider.gameObject);
        }
        if (collision.collider.gameObject.CompareTag("Decoration"))
        { 
            Destroy(collision.collider.gameObject);
        }
        if (collision.gameObject.CompareTag("Arudanich")&&Random.Range(0,50)==0) 
        {
            MissionControlVoice.only.PlayReplica(3);
        }
    }
}
