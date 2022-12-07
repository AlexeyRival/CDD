using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GPUInstancer : MonoBehaviour
{
    public class ObjData 
    {
        public Vector3 pos;
        public Vector3 scale;
        public Quaternion rot;

        public ObjData(Vector3 pos, Vector3 scale, Quaternion rot)
        {
            this.pos = pos;
            this.scale = scale;
            this.rot = rot;
        }

        public Matrix4x4 matrix 
        {
            get { return Matrix4x4.TRS(pos, rot, scale); }
        }
    }
    public int Instances;
    public Mesh mesh;
    public Material material;
    private List<List<ObjData>> Batches = new List<List<ObjData>>();
    public static GPUInstancer only;

    private void RenderBatches() 
    {
        foreach(var batch in Batches)
        {
                Graphics.DrawMeshInstanced(mesh, 0, material, batch.Select((a)=>a.matrix).ToList());
        }
    }
    private void Start()
    {
        only = this;
        //SetObjects();
    }
    // Start is called before the first frame update
    public void SetObjects(TurboMarching[] marchings)
    {
        if (marchings.Length == 0) { return; }
        int batchIndexNum = 0;
        List<ObjData> currBatch = new List<ObjData>();
        for (int i = 0; i < marchings.Length; ++i)
        if(marchings[i].decorations!=null&&marchings[i].decorations.Count>0)
        {
            if (marchings[i].decorations.Count + batchIndexNum < 1000)
            {
                currBatch.AddRange(marchings[i].decorations);
                batchIndexNum += marchings[i].decorations.Count;
            }
            else 
            {
                for (int j = 0; j < marchings[i].decorations.Count; ++j) 
                {
                    currBatch.Add(marchings[i].decorations[j]);
                    ++batchIndexNum;
                    if (batchIndexNum >= 1000) 
                    {
                        Batches.Add(currBatch);
                        currBatch = BuildNewBatch();
                        batchIndexNum = 0;
                    }
                }
            }
        }
        if (batchIndexNum != 0) 
        {
            Batches.Add(currBatch);
        }
    }
    public void SetObjects()
    {
        int batchIndexNum = 0;
        List<ObjData> currBatch = new List<ObjData>();
        for (int i = 0; i < Instances; ++i) 
        {
            AddObj(currBatch, i);
            ++batchIndexNum;
            if (batchIndexNum >= 1000)
            {
                Batches.Add(currBatch);
                currBatch = BuildNewBatch();
                batchIndexNum = 0;
            }
            else if (i == Instances - 1) 
            {
                Batches.Add(currBatch);
            }
        }
    }
    private List<ObjData> BuildNewBatch() 
    {
        return new List<ObjData>();
    }
    private void AddObj(List<ObjData> currBatch, int i) 
    {
        Vector3 position = new Vector3(Random.Range(-70, 70), 0, Random.Range(-70, 70));
        Vector3 scale = new Vector3(2, 2, 2);
        Quaternion rotation = Quaternion.identity;
        currBatch.Add(new ObjData(position, scale, rotation));
    }
    void Update()
    {
        RenderBatches();
    }
}
