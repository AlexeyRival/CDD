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


    //растяжка по времени
    private int iterer,diterer;
    private List<Vector5> vectors = new List<Vector5>();
    private void Start()
    {
        generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
    }
    public void Recalculate()
    {

        objs = new Dictionary<GameObject, Vector3>();
        instances = new Dictionary<int, Vector5>();
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
    private Dictionary<int, Vector5> instances;
    void Update()
    {
        if (GameObject.FindGameObjectWithTag("Destroyer")) {
            destroyers = GameObject.FindGameObjectsWithTag("Destroyer");
            List<int> dd = new List<int>();
            foreach (var ins in instances) 
            {
                if (ins.Value.i == iterer) 
                {
                    dd.Add(ins.Key);
                }
            }
            for (int i = 0; i < dd.Count; ++i)
            {
                instances.Remove(dd[i]);
            }
            for (int d = 0; d < destroyers.Length; ++d)
            {
             //   if (!objs.ContainsKey(destroyers[d]) || Vector3.Distance(objs[destroyers[d]], destroyers[d].transform.position) > destroyers[d].transform.localScale.x * 0.25f)
                {
                    Vector5 ob = new Vector5();
                    if (!instances.ContainsKey(destroyers[d].GetInstanceID()))
                    {
                        ob.i = iterer;
                        ob.x = destroyers[d].transform.position.x;
                        ob.y = destroyers[d].transform.position.y;
                        ob.z = destroyers[d].transform.position.z;
                        ob.w = destroyers[d].transform.localScale.x;
                        instances.Add(destroyers[d].GetInstanceID(), ob);
                    }
                        //dd.Add(d);
                    //if (!objs.ContainsKey(destroyers[d])) { objs.Add(destroyers[d], destroyers[d].transform.position); } else { objs[destroyers[d]] = destroyers[d].transform.position; }
                }
            }
                for (int i = 0; i < turboMarchings.Length; ++i) if(i%8==iterer)
                {
                List<Vector4> updater = new List<Vector4>();
                    bool isChanged = false;
                //for (int d = 0; d < dd.Count; ++d)// if (d % 8 == diterer)
              foreach (var ins in instances)
                    {
                        //
                        //  

                        {
                            /*if (Vector3.Distance(destroyers[d].transform.position, turboMarchings[i].center) < turboMarchings[i].sizeXYZ + destroyers[d].transform.localScale.x)
                            {
                                turboMarchings[i].CheckUpdate(destroyers[d]);
                                isChanged = true;
                            }*/
                            if (generator.IsChunkContainSphere(turboMarchings[i], ins.Value.pos, ins.Value.w))
                            {
                                updater.Add(ins.Value.v4);
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

        for (int i = 0; i < turboMarchings.Length; ++i) 
        if(i%8==iterer){
                turboMarchings[i].FlipUpdate();
        }
        ++iterer;
        if (iterer > 8) { iterer = 0; ++diterer; }
        if (diterer > 8) { diterer = 0; }
    }
    public struct Vector5 
    {
        public float x, y, z,w;
        public int i;
        public Vector5(float x, float y, float z, float w, int i)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
            this.i = i;
        }
        public Vector3 pos { get { return new Vector3(x, y, z); } }
        public Vector4 v4 { get { return new Vector4(x, y, z, w); } }
    }
}
