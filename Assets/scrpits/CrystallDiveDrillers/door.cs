using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class door : MonoBehaviour
{
    private Vector3 startpos, endpos;
    private bool isopen;
    // Start is called before the first frame update
    void Start()
    {
        startpos = transform.position;
        endpos = startpos + new Vector3(0, -8, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (isopen)
        {
            if (transform.position != endpos) { transform.position = Vector3.Slerp(transform.position,endpos,Time.deltaTime*2f); }
        }
        else 
        {
            if (transform.position != startpos) { transform.position = Vector3.Slerp(transform.position, startpos, Time.deltaTime * 2f); }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            isopen = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            isopen = false;
        }
    }
}
