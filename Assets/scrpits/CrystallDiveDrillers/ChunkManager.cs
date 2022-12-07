using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public GameObject[] chunks;
    public marchingspace[] marchingspaces;
    public TurboMarching[] turboMarchings;
    public GameObject spheredestroyer;
    private GameObject[] destroyers;
    private Vector3[] centers;
    private int[] sizes;
    public bool TURBOMODE;
    private Dictionary<GameObject,Vector3> objs;

    private Generator generator;

    private void Start()
    {
        generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
    }
    public void Recalculate()
    {

        objs = new Dictionary<GameObject, Vector3>();
        GameObject[] spaces = GameObject.FindGameObjectsWithTag("Chunk");
        turboMarchings = new TurboMarching[spaces.Length];
        centers = new Vector3[spaces.Length];
        sizes = new int[spaces.Length];
        for (int i = 0; i < spaces.Length; ++i)
        {
            turboMarchings[i] = spaces[i].GetComponent<TurboMarching>();
            centers[i] = spaces[i].GetComponent<TurboMarching>().center;
            sizes[i] = spaces[i].GetComponent<TurboMarching>().sizeXYZ;
        }
        generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
        for (int i = 0; i < turboMarchings.Length; ++i)
        {
            for (int ii = i; ii < turboMarchings.Length; ++ii) if (i != ii)
                {
                    //if (Generator.FastDist(turboMarchings[i].transform.position, turboMarchings[ii].transform.position, turboMarchings[i].sizeXYZ+ 1))
                    if (Generator.FastDist(turboMarchings[i].transform.position, turboMarchings[ii].transform.position, (turboMarchings[i].sizeXYZ * turboMarchings[i].step) * (turboMarchings[i].sizeXYZ * turboMarchings[i].step) + 1))
                    {
                        turboMarchings[i].neighbors.Add(turboMarchings[ii]);
                        turboMarchings[ii].neighbors.Add(turboMarchings[i]);
                    }
                }
        }
        GPUInstancer.only.SetObjects(turboMarchings);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameObject.FindGameObjectWithTag("Destroyer")) {
            destroyers = GameObject.FindGameObjectsWithTag("Destroyer");
            List<int> dd = new List<int>();
            for (int d = 0; d < destroyers.Length; ++d)
                if (!objs.ContainsKey(destroyers[d]) || Vector3.Distance(objs[destroyers[d]], destroyers[d].transform.position) > destroyers[d].transform.localScale.x * 0.25f)
                {
                    dd.Add(d);
                    if (!objs.ContainsKey(destroyers[d])) { objs.Add(destroyers[d], destroyers[d].transform.position); } else { objs[destroyers[d]] = destroyers[d].transform.position; }
                }
                for (int i = 0; i < turboMarchings.Length; ++i) {
                List<Vector4> updater = new List<Vector4>();
                    bool isChanged = false;
                for (int d = 0; d < dd.Count; ++d)
                {
                    //
                      //  

                        {
                            /*if (Vector3.Distance(destroyers[d].transform.position, turboMarchings[i].center) < turboMarchings[i].sizeXYZ + destroyers[d].transform.localScale.x)
                            {
                                turboMarchings[i].CheckUpdate(destroyers[d]);
                                isChanged = true;
                            }*/
                            if (generator.IsChunkContainSphere(turboMarchings[i], destroyers[dd[d]].transform.position, destroyers[dd[d]].transform.localScale.x))
                            {
                                updater.Add(new Vector4(destroyers[dd[d]].transform.position.x, destroyers[dd[d]].transform.position.y, destroyers[dd[d]].transform.position.z, destroyers[dd[d]].transform.localScale.x));
                                isChanged = true;
                            }
                        }

                        //TODO обновлять!!!!
                        // if(generator.isServer&&isChanged)generator.UpdateWalkGroup();
                    
                }
                if (isChanged) 
                {
                    turboMarchings[i].UpdateMesh(updater.ToArray());
                }
            }
        }
    }
}
