using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class marchingspace : MonoBehaviour
{
    public int sizeX, sizeY, sizeZ;
    public bool[,,] space;
    public float[,,] shift;
    public bool isGizmosDraws;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public float step = 0.5f;
    private Mesh mesh;
    public Vector3 center;
    public List<Resource> resources;
    public GameObject[] flowers;
    public GameObject[] grasses;
    public GameObject[] stalagmites;
    public Generator generator;
    private FastNoiseLite noise, secondnoise,thirdnoise;
    public List<marchingspace> neighbors, friends;
 //   public Dictionary<Vector3, Generator.walkpoint> walkpoints;
    public Dictionary<Vector3, byte> borderpoints;
    public Dictionary<Vector3Int, GameObject> spawneddecorations;
    
    //поиск пути
    public bool isChecked;
    public int weight;
    /**
    void Start()
    {
        friends = new List<marchingspace>();
        neighbors = new List<marchingspace>();
   //     walkpoints = new Dictionary<Vector3, Generator.walkpoint>();
        borderpoints = new Dictionary<Vector3, byte>();
        spawneddecorations = new Dictionary<Vector3Int, GameObject>();
        generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
        space = new bool[sizeX, sizeY, sizeZ];
        shift = new float[sizeX, sizeY, sizeZ];
        center = transform.position + new Vector3(sizeX * step * transform.localScale.x, sizeY * step * transform.localScale.y, sizeZ * step * transform.localScale.z) * 0.5f;
        Generate();
        BakeMesh();
    }
    private void Generate()
    {
        noise = new FastNoiseLite();
        secondnoise = new FastNoiseLite();
        thirdnoise = new FastNoiseLite();
        noise.SetSeed(generator.seed);// 1212391999);//Random.Range(0,9763245));
        secondnoise.SetSeed(-generator.seed);// 1212391999);//Random.Range(0,9763245));
        thirdnoise.SetSeed((int)(((long)generator.seed)*125/144));
        //noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        secondnoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        thirdnoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        //print(noise.GetNoise((Random.Range(0,sizeX) + transform.position.x) * 3, (Random.Range(0, sizeY) + transform.position.y) * 3, (Random.Range(0, sizeZ) + transform.position.z) * 3));
        int vertscount = sizeX * sizeY * sizeZ;
        float f, sf;
        GameObject ob;


        int[] mineralscount = new int[resources.Count];
        for (int i = 0; i < mineralscount.Length; ++i) mineralscount[i] = Random.Range(15, 30);
        int x, y, z;
        bool maxpoint = false;

        List<Vector3> cavepoints = new List<Vector3>();
        for (int i = 0; i < generator.cavepoints.Count; ++i) if (Vector3.Distance(transform.position, generator.cavepoints[i]) < 36)
            {
                cavepoints.Add(generator.cavepoints[i]);
            }
        List<Vector3> tunnelpoints = new List<Vector3>();
        for (int i = 0; i < generator.tunnelpoints.Count; ++i) if (Vector3.Distance(transform.position, generator.tunnelpoints[i]) < 36)
            {
                tunnelpoints.Add(generator.tunnelpoints[i]);
            }
        Vector3 gencenter = Generator.center;
        for (x = 0; x < sizeX; ++x)
            for (y = 0; y < sizeY; ++y)
                for (z = 0; z < sizeZ; ++z)
                {
                    //шумы
                    f = noise.GetNoise((x + transform.position.x) * 4, (y + transform.position.y) * 4, (z + transform.position.z) * 4);//3
                    sf = secondnoise.GetNoise((x + transform.position.x) * 4, (y + transform.position.y) * 4, (z + transform.position.z) * 4);//3

                    //первая пещера
                    if (
                            Vector3.Distance(gencenter, new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z)) + f * 4 < 20
                            && Mathf.Abs(gencenter.y - (y + transform.position.y)) + f * 2 < 10)
                    {
                        space[x, y, z] = true;
                    }

                    shift[x, y, z] = f;
                    //0;

                    //другие пещеры

                    for (int i = 0; i < cavepoints.Count; ++i) if (
                            //Vector3.Distance(cavepoints[i],new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z))+f*3<10
                            Generator.FastDist(cavepoints[i], new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z), 100 - (f * 3 * f * 3))
                            && Mathf.Abs(cavepoints[i].y - (y + transform.position.y)) + f * 2 < 5

                            )
                        {
                            space[x, y, z] = true;
                        }
                    for (int i = 0; i < tunnelpoints.Count; ++i) if (
                            Vector3.Distance(tunnelpoints[i], new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z)) + f * 2 < 4

                            )
                        {
                            space[x, y, z] = true;
                        }

                    //руды
                    if (
                        (x != 0 && y != 0 && z != 0 && x != sizeX - 1 && y != sizeY - 1 && z != sizeZ - 1) &&
                        !space[x, y, z] && (space[x + 1, y, z] | space[x - 1, y, z] | space[x, y + 1, z] | space[x, y - 1, z] | space[x, y, z + 1] | space[x, y, z - 1]))
                    {
                        //герит
                        if (//f < 0.0001f && f > -0.0001f &&
                            sf > 0.4f && sf < 0.6f &&
                            f > 0.3f && f < 0.6f &&
                            mineralscount[0] > 0)
                        {
                            ob = Instantiate(resources[0].orePrefab, new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z), Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), transform);
                            ob.name = resources[0].id.ToString();
                            --mineralscount[0];
                        }

                        //греадит
                        if (//f < 0.0001f && f > -0.0001f &&
                            sf > -0.5f && sf < -0.35f &&
                            f > 0.4f && f < 0.5f &&
                            mineralscount[1] > 0)
                        {
                            ob = Instantiate(resources[1].orePrefab, new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z), Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), transform);
                            ob.name = resources[1].id.ToString();
                            --mineralscount[1];
                        }

                        //Нирр
                        if (//f < 0.0001f && f > -0.0001f &&
                            sf > -0.5f && sf < -0.35f &&
                            f > 0f && f < 0.3f &&
                            mineralscount[2] > 0)
                        {
                            ob = Instantiate(resources[2].orePrefab, new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z), Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), transform);
                            ob.name = resources[2].id.ToString();
                            --mineralscount[2];
                        }
                        //Фальдареит
                        if (
                            sf > 0.2f && sf < 0.5f &&
                            f > 0.7f &&
                            mineralscount[2] > 0)
                        {
                            ob = Instantiate(resources[3].orePrefab, new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z), Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), transform);
                            ob.name = resources[3].id.ToString();
                            --mineralscount[3];
                        }
                    }
                    //центр всего
                    if (
                            Vector3.Distance(gencenter, new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z)) + f * 2 < 15
                            && Mathf.Abs(gencenter.y - (y + transform.position.y)) + f < 8
                            && f < 0.5f && f > -0.5f
                        //f < -0.99f &&
                        //(Mathf.Pow(x + transform.position.x - Generator.center.x, 2) + Mathf.Pow(y + transform.position.y - Generator.center.y, 2) + Mathf.Pow(z + transform.position.z - Generator.center.z, 2) < 30 * 30)
                        && !maxpoint)
                    {
                        generator.startpoints.Add(new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z));
                        maxpoint = true;
                    }

                    if (space[x, y, z] && Random.Range(0, 100) == 0) { generator.bugspawnpoints.Add(new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z)); }

                    //пол
                    if (y > 0 && space[x, y, z] && !space[x, y - 1, z])
                    {
                        if (sf > 0.8f)
                        {
                            spawneddecorations.Add(new Vector3Int(x, y, z), Instantiate(flowers[Random.Range(0, flowers.Length)], new Vector3(x + transform.position.x + Random.Range(-0.5f, 0.5f), y + transform.position.y - 0.3f, z + transform.position.z + Random.Range(-0.5f, 0.5f)), Quaternion.Euler(0, Random.Range(0, 360), 0), transform));
                        } else if (((sf<0.8f&&sf>0.5f)|| (sf > -0.8f && sf < -0.5f))&& Random.Range(0, 7) == 0) { 
                            Instantiate(stalagmites[Random.Range(0, stalagmites.Length)], new Vector3(x + transform.position.x + Random.Range(-0.5f, 0.5f), y + transform.position.y-1f, z + transform.position.z + Random.Range(-0.5f, 0.5f)), Quaternion.Euler(180, Random.Range(0, 360), 0), transform);
                        }
                        else
                        if (sf > 0f)
                        {
                            spawneddecorations.Add(new Vector3Int(x, y, z), Instantiate(grasses[Random.Range(0, grasses.Length)], new Vector3(x + transform.position.x + Random.Range(-0.125f, 0.125f), y + transform.position.y - 0.3f, z + transform.position.z + Random.Range(-0.125f, 0.125f)), Quaternion.Euler(0, Random.Range(0, 360), 0), transform));
                        }
                    }
                    //потолок
                    if (y > 0 && !space[x, y, z] && space[x, y - 1, z])
                    {
                        //сталагмиты
                        if (((sf > -0.5f && sf < -0.3f) || (sf > 0.3f && sf < 0.5f)) && Random.Range(0, 5) == 0)
                        {
                            Instantiate(stalagmites[Random.Range(0, stalagmites.Length)], new Vector3(x + transform.position.x + Random.Range(-0.5f, 0.5f), y + transform.position.y, z + transform.position.z + Random.Range(-0.5f, 0.5f)), Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
                        }
                    }
                }
        /*int borders,matches;
        
        for (x = 0; x < sizeX; ++x)
            for (y = 0; y < sizeY; ++y)
                for (z = 0; z < sizeZ; ++z)
                {
                    borders = ((x == 0) ? 1 : 0) + ((x == sizeX - 1) ? 1 : 0) + ((y == 0) ? 1 : 0) + ((y == sizeY - 1) ? 1 : 0) + ((z == 0) ? 1 : 0) + ((z == sizeZ - 1) ? 1 : 0);
                    matches = ((x == 0 || !space[x - 1, y, z]) ? 1 : 0) + ((x == sizeX - 1 || !space[x + 1, y, z])?1:0)
                        + ((y == 0 || !space[x, y-1, z]) ? 1 : 0) + ((y == sizeY - 1 || !space[x, y+1, z]) ? 1 : 0)
                        + ((z == 0 || !space[x, y, z-1]) ? 1 : 0) + ((z == sizeZ - 1 || !space[x, y, z+1]) ? 1 : 0);
                    if (
                    space[x,y,z]&&(matches>0&&matches>borders))
                    {
                        generator.walkpoints.Add(new Generator.walkpoint(new Vector3(x + transform.position.x, y + transform.position.y, z + transform.position.z)));
                    }
                }
        //for (int i = 0; i < Random.Range(15, 30); ++i) {
        //Instantiate(ore, new Vector3(Random.Range(0, sizeX)*step, Random.Range(0, sizeY) * step, Random.Range(0, sizeZ) * step)+transform.position, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
        //}

    }
    public void BakeMesh()
    {
        mesh = new Mesh();
        List<Vector3> fuckthislist = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
     //   walkpoints = new Dictionary<Vector3, Generator.walkpoint>();
        borderpoints = new Dictionary<Vector3, byte>();
        meshData bufdata;
        float f,sf,tf;
        float fcon = 256;//4 32 128
        int max = 0;
        int x, y, z, i;
        int borders, matches;
        string smellname;
        for (x = 0; x < sizeX - 1; ++x)
        {
            for (y = 0; y < sizeY - 1; ++y)
            {
                for (z = 0; z < sizeZ - 1; ++z)
                {
                    smellname = "-1";
                    bufdata = getData(getMC(getMCId(new bool[] { space[x, y, z], space[x + 1, y, z], space[x + 1, y, z + 1], space[x, y, z + 1], space[x, y + 1, z], space[x + 1, y + 1, z], space[x + 1, y + 1, z + 1], space[x, y + 1, z + 1] })));
                    for (i = 0; i < bufdata.verts.Length; ++i)
                    {
                        bufdata.verts[i] *= step;
                        bufdata.verts[i] = new Vector3(bufdata.verts[i].x + x * step, bufdata.verts[i].y + y * step, bufdata.verts[i].z + z * step);
                        f = noise.GetNoise((bufdata.verts[i].x + transform.position.x) * fcon, (bufdata.verts[i].y + transform.position.y) * fcon, (bufdata.verts[i].z + transform.position.z) * fcon)*0.175f;//0.25f
                        sf = secondnoise.GetNoise((bufdata.verts[i].x + transform.position.x) * fcon, (bufdata.verts[i].y + transform.position.y) * fcon, (bufdata.verts[i].z + transform.position.z) * fcon) * 0.175f;
                        tf = thirdnoise.GetNoise((bufdata.verts[i].x + transform.position.x) * fcon, (bufdata.verts[i].y + transform.position.y) * fcon, (bufdata.verts[i].z + transform.position.z) * fcon) * 0.175f;
                        bufdata.verts[i] += new Vector3(sf, f, tf);
                        //uvs.Add(new Vector2(Mathf.Sin(bufdata.verts[i].x / sizeX), Mathf.Cos(bufdata.verts[i].z / sizeZ) * 0.5f + Mathf.Cos(bufdata.verts[i].y / sizeY) * 0.5f));
                        //uvs.Add(new Vector2(bufdata.verts[i].x/sizeX*0.5f+bufdata.verts[i].y/sizeY*0.5f,bufdata.verts[i].z / sizeZ*0.5f + bufdata.verts[i].y / sizeY * 0.5f));
                    }
                    fuckthislist.AddRange(bufdata.verts);
                    if (triangles.Count > 0) for (i = 0; i < bufdata.tris.Length; ++i)
                        {
                            bufdata.tris[i] += max + 1;
                        }
                    for (i = 0; i < bufdata.tris.Length; ++i)
                    {
                        if (bufdata.tris[i] > max) max = bufdata.tris[i];
                    }

                    triangles.AddRange(bufdata.tris);
                    if (space[x, y, z]) {
                        if (x == 0 || x == sizeX - 1)
                        {
                            for (i = 0; i < neighbors.Count; ++i) {
                                if (!friends.Contains(neighbors[i]))
                                {
                                    if (neighbors[i].space[sizeX - 1 - x, y, z])
                                    {
                                        friends.Add(neighbors[i]);
                                        neighbors[i].friends.Add(this);
                                        break;
                                    }
                                }
                            }
                        }
                        if (y == 0 || y == sizeY - 1)
                        {
                            for (i = 0; i < neighbors.Count; ++i) {
                                if (!friends.Contains(neighbors[i]))
                                {
                                    if (neighbors[i].space[x, sizeY - 1 - y, z])
                                    {
                                        friends.Add(neighbors[i]);
                                        neighbors[i].friends.Add(this);
                                        break;
                                    }
                                }
                            }
                        }
                        if (z == 0 || z == sizeZ - 1)
                        {
                            for (i = 0; i < neighbors.Count; ++i) {
                                if (!friends.Contains(neighbors[i]))
                                {
                                    if (neighbors[i].space[x, y, sizeZ - 1 - z])
                                    {
                                        friends.Add(neighbors[i]);
                                        neighbors[i].friends.Add(this);
                                        break;
                                    }
                                }
                            }
                        }
                        if (generator.isServer) {
                            borders = ((x == 0) ? 1 : 0) + ((x == sizeX - 1) ? 1 : 0) + ((y == 0) ? 1 : 0) + ((y == sizeY - 1) ? 1 : 0) + ((z == 0) ? 1 : 0) + ((z == sizeZ - 1) ? 1 : 0);
                            matches = ((x == 0 || !space[x - 1, y, z]) ? 1 : 0) + ((x == sizeX - 1 || !space[x + 1, y, z]) ? 1 : 0)
                                + ((y == 0 || !space[x, y - 1, z]) ? 1 : 0) + ((y == sizeY - 1 || !space[x, y + 1, z]) ? 1 : 0)
                                + ((z == 0 || !space[x, y, z - 1]) ? 1 : 0) + ((z == sizeZ - 1 || !space[x, y, z + 1]) ? 1 : 0);
                            Vector3 calculatedvector = new Vector3(step * x + transform.position.x, step * y + transform.position.y, step * z + transform.position.z);

                            if (matches > 0 && borders != matches)
                            {
                            //    if (!walkpoints.ContainsKey(calculatedvector))
                                {
                              //      walkpoints.Add(calculatedvector, new Generator.walkpoint(calculatedvector,true));
                                    if (x < 2 || x > sizeX - 3 || y < 2 || y > sizeY - 3 || z < 2 || z > sizeZ - 3) {
                                    borderpoints.Add(calculatedvector, 0);
                                    }
                                }
                            }/*
                        else
                        {
                            if (walkpoints.ContainsKey(calculatedvector)) {
                                walkpoints.Remove(calculatedvector);
                            }
                        }
                        } }
                }
            }
        }
        if (generator.isServer)
        {
          //  foreach (var point in walkpoints)
            {
                //   if (point.Value.isBorder)
                {
                    for (i = 0; i < neighborsTable.Length; ++i)
                    {
                     //   if (walkpoints.ContainsKey(point.Value.position + neighborsTable[i] * step) && !point.Value.friends.Contains(point.Value.position + neighborsTable[i] * step))
                        {
                       //     point.Value.friends.Add(point.Value.position + neighborsTable[i] * step);
                     //       walkpoints[point.Value.position + neighborsTable[i] * step].friends.Add(point.Key);
                        }
                    }
                }
            }
           //     generator.walkpointscount += walkpoints.Count;
        }
        mesh.vertices = fuckthislist.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateTangents();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
    public Vector3 CalculateWeights(Vector3 finalpoint) {
        Vector3 resualt=SetWeights(finalpoint);
        SetCorners();
        return resualt;
    }
    public Vector3 CalculateWeightsFast(Vector3 finalpoint) {
        Vector3 resualt=SetWeightsFast(finalpoint);
        SetCorners();
        return resualt;
    }
    private Vector3 SetWeights(Vector3 finalpoint) {
        int alivepoints, prealivepoints = -1;
        List<Vector3> buffer;
        float minweight;
        int minid;
        int k = 0;
        
        while (k < 100)
        {
            ++k;
            alivepoints = 0;
            buffer = new List<Vector3>();
          //  foreach (var point in walkpoints)
            {
                //if (point.Value.weight == 0) {
                    //for (int i = 0; i < point.Value.friends.Count; ++i) {
                        if (walkpoints[point.Value.friends[i]].weight != 0) {
                            buffer.Add(point.Key);
                            break;
                        }
                    }
                    ++alivepoints;
                }
            }
            for (int i = 0; i < buffer.Count; ++i)
            {
                minweight = 999f;
                minid = -1;
                for (int ii = 0; ii < walkpoints[buffer[i]].friends.Count; ++ii) {
                    if (walkpoints[walkpoints[buffer[i]].friends[ii]].weight != 0 && walkpoints[walkpoints[buffer[i]].friends[ii]].weight < minweight) {
                        minid = ii;
                        minweight = walkpoints[walkpoints[buffer[i]].friends[ii]].weight;
                    }
                }
                if (minid != -1) {
                    walkpoints[buffer[i]].weight = walkpoints[walkpoints[buffer[i]].friends[minid]].weight + 1;
                }
                if (Generator.FastDist(buffer[i], finalpoint, 4))
                {
                    return buffer[i];
                }
            }
            if (alivepoints == prealivepoints) { break; }
            prealivepoints = alivepoints;
        }
        return new Vector3(-1, -1, -1);
    }
    private Vector3 SetWeightsFast(Vector3 finalpoint) {
        List<Vector3> buffer = new List<Vector3>();
        List<Vector3> secondbuffer = new List<Vector3>();
        int k = 0;

        foreach (var point in walkpoints)
        {
            if (point.Value.weight != 0)
            {
               buffer.Add(point.Key);
            }
        }
        while (k < 20)
        {
            ++k;
            for (int i = 0; i < buffer.Count; ++i)
            {
                if (Generator.FastDist(buffer[i], finalpoint, 4))
                {
                    return buffer[i];
                }
                for (int ii = 0; ii < walkpoints[buffer[i]].friends.Count; ++ii) 
                {
                    if (walkpoints[walkpoints[buffer[i]].friends[ii]].weight == 0) {
                        walkpoints[walkpoints[buffer[i]].friends[ii]].weight = walkpoints[buffer[i]].weight+1;
                        secondbuffer.Add(walkpoints[buffer[i]].friends[ii]);
                    }
                }
            }
            buffer = secondbuffer;
            secondbuffer = new List<Vector3>();
            if (buffer.Count == 0) { break; }
        }
        return new Vector3(-1, -1, -1);
    }
    private void SetCorners() {
        int count = 0;
        foreach (var point in borderpoints) if (walkpoints[point.Key].weight != 0)//
            {
                for (int i = 0; i < friends.Count; ++i)
                {
                    for (int ii = 0; ii < neighborsTable.Length; ++ii)
                    {
                        if (friends[i].borderpoints.ContainsKey(point.Key + neighborsTable[ii]))
                        {
                            ++count;
                            if (friends[i].walkpoints[point.Key + neighborsTable[ii]].weight == 0)
                            {
                                friends[i].walkpoints[point.Key + neighborsTable[ii]].weight = walkpoints[point.Key].weight + 1;
                                Debug.DrawRay(point.Key, neighborsTable[ii] * step, new Color(0.77f, 0.34f, 0.44f, 0.34f), 10f);
                                break;
                            }
                        }
                    }
                }
            }
    }
    public void ClearWeights() {
        weight = 9999;
        foreach (var point in walkpoints) {
            point.Value.weight = 0;
        }
    }
    private int getMCId(bool[] arr) {
        return (arr[0] ? 0 : 1) + (arr[1] ? 0 : 2) + (arr[2] ? 0 : 4) + (arr[3] ? 0 : 8) + (arr[4] ? 0 : 16) + (arr[5] ? 0 : 32) + (arr[6] ? 0 : 64) + (arr[7] ? 0 : 128);
    }
    private int[] getMC(int id) {
        List<int> list = new List<int>();
        for (int i = 0; i < 16; ++i) {
            if (triangulationTable[id, i] == -1) break;
            list.Add(triangulationTable[id,i]);
        }
        return list.ToArray();
    }
    private meshData getData(int[] MCs) {
        List<Vector3> points = new List<Vector3>();
        List<int> stupidIds = new List<int>();
        for (int i = 0; i < MCs.Length; ++i) {
            if (!points.Contains(trianglesTable[MCs[i]]))
            {
                stupidIds.Add(points.Count);
                points.Add(trianglesTable[MCs[i]]);
            }
            else {
                stupidIds.Add(points.IndexOf(trianglesTable[MCs[i]]));
            }
        }
        meshData md = new meshData(points.ToArray(),stupidIds.ToArray());
        return md;
    }
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (isGizmosDraws)
            {
                Gizmos.color = Color.cyan;
                /*for (int x = 0; x < sizeX; ++x)
                    for (int y = 0; y < sizeY; ++y)
                        for (int z = 0; z < sizeZ; ++z)
                        {
                            if (!space[x, y, z]) Gizmos.DrawCube(transform.position + new Vector3(x * step, y * step, z * step), new Vector3(0.1f, 0.1f, 0.1f));
                        }
            }
            if (maxx != 0) {
                Gizmos.color = new Color(0.43f, 0.93f, 0.2f, 0.4f);
                Gizmos.DrawWireCube(new Vector3((minx + maxx) * 0.5f, (miny + maxy) * 0.5f, (minz + maxz) * 0.5f)+transform.position, new Vector3(minx - maxx, miny - maxy, minz - maxz));
            }
            for (int i = 0; i < friends.Count; ++i)
            {
                Debug.DrawLine(center, friends[i].center, Color.blue, 1f);
            }
            foreach (var point in walkpoints)
            {
                Gizmos.color = new Color(0.83f, 0.93f, 0.2f, 0.4f);
                if (point.Value.weight!=0)
                {
                    for (int ii = 0; ii < point.Value.friends.Count; ++ii) {
               //        Debug.DrawLine(point.Value.position, walkpoints[point.Value.friends[ii]].position,new Color(0.83f, 0.93f, 0.2f, 0.2f));
                    }//new Color(0.83f-Mathf.Sin(3*point.Value.weight*0.05f), Mathf.Sin(3 * point.Value.weight * 0.05f), Mathf.Sin(1.5f * point.Value.weight * 0.05f), 0.4f);
                    Gizmos.color = new Color(0.83f-(point.Value.weight*0.03f), (point.Value.weight * 0.3f), 0.2f, 0.4f);
                }
                Gizmos.DrawWireCube(point.Value.position, new Vector3(0.5f, 0.5f, 0.5f));
            }
            if (mesh.vertexCount != 0)
            {
                Gizmos.color = !isChecking ? Color.white : Color.magenta;
                Gizmos.DrawWireCube(center, new Vector3(sizeX, sizeY, sizeZ));
            }
        }
    }
    private int minx;
    private int maxx;
    private int miny;
    private int maxy;
    private int minz;
    private int maxz;
    private bool isChanged;
    private bool isChecking=false;
    public void CheckUpdate(GameObject sph)
    {
        isChecking = true;
            isChanged = false;
                try
                {
                    Vector3 vec = sph.transform.position;
                    float scale = sph.transform.localScale.x;
                    //if (Vector3.Distance(vec, center) > sizeX*1.6f * step) { continue; }
                    minx = (int)((vec.x - transform.position.x) - scale * 1.5f);
                    maxx = (int)((vec.x - transform.position.x) + scale * 1.5f);
                    miny = (int)((vec.y - transform.position.y) - scale * 1.5f);
                    maxy = (int)((vec.y - transform.position.y) + scale * 1.5f);
                    minz = (int)((vec.z - transform.position.z) - scale * 1.5f);
                    maxz = (int)((vec.z - transform.position.z) + scale * 1.5f);

                    /*
                    if (maxx < 0) continue;
                    if (minx > sizeX) continue;
                    if (maxy < 0) continue;
                    if (miny > sizeY) continue;
                    if (maxz < 0) continue;
                    if (minz > sizeZ) continue;
                    
                    if (minx < 0) minx = 0;
                    if (maxx > sizeX) maxx = sizeX;
                    if (miny < 0) miny = 0;
                    if (maxy > sizeY) maxy = sizeY;
                    if (minz < 0) minz = 0;
                    if (maxz > sizeZ) maxz = sizeZ;
            for (int x = minx; x < maxx; ++x)
                for (int y = miny; y < maxy; ++y)
                    for (int z = minz; z < maxz; ++z) if (!space[x, y, z])
                        {
                            //        if (Vector3.Distance(vec, new Vector3(x, y+shift[x,y,z], z) + transform.position) < scale) {
                            if (Generator.FastDist(vec, new Vector3(x, y + shift[x, y, z], z) + transform.position, scale * scale))
                            {
                                space[x, y, z] = true; isChanged = true;
                            }
                        }
                        else
                        {
                            if (Generator.FastDist(vec, new Vector3(x, y + shift[x, y, z], z) + transform.position, scale * scale))
                            {
                                if (spawneddecorations.ContainsKey(new Vector3Int(x, y, z))) {  Destroy(spawneddecorations[new Vector3Int(x, y, z)]); spawneddecorations.Remove(new Vector3Int(x, y, z)); }
                            }
                        }
                }
                catch
                {
                    print("error");
                }
                //     Destroy(sph);
            
            if (isChanged) BakeMesh();

        
    }
    private void OnCollisionEnter(Collision collision)
    {
   //     if (collision.transform.CompareTag("Destroyer")) { CheckUpdate(collision.gameObject); }
    }
    private struct meshData {
        public Vector3[] verts;
        public int[] tris;
        public meshData(Vector3[] verts, int[] tris)
        {
            this.verts = verts;
            this.tris = tris;
        }
    }

    public static readonly Vector3[] neighborsTable = {
        new Vector3(-1,-1,-1),
        new Vector3(-1,-1,0),
        new Vector3(-1,-1,1),
        new Vector3(-1,0,-1),
        new Vector3(-1,0,0),
        new Vector3(-1,0,1),
        new Vector3(-1,1,-1),
        new Vector3(-1,1,0),
        new Vector3(-1,1,1),
        new Vector3(0,-1,-1),
        new Vector3(0,-1,0),
        new Vector3(0,-1,1),
        new Vector3(0,0,-1),
        new Vector3(0,0,1),
        new Vector3(0,1,-1),
        new Vector3(0,1,0),
        new Vector3(0,1,1),
        new Vector3(1,-1,-1),
        new Vector3(1,-1,0),
        new Vector3(1,-1,1),
        new Vector3(1,0,-1),
        new Vector3(1,0,0),
        new Vector3(1,0,1),
        new Vector3(1,1,-1),
        new Vector3(1,1,0),
        new Vector3(1,1,1)
    };
    private readonly Vector3[] trianglesTable = {
        new Vector3(0.5f,0,0),
        new Vector3(1f,0,0.5f),
        new Vector3(0.5f,0,1),
        new Vector3(0,0,0.5f),
        new Vector3(0.5f,1f,0),
        new Vector3(1f,1f,0.5f),
        new Vector3(0.5f,1f,1f),
        new Vector3(0,1f,0.5f),
        new Vector3(0,0.5f,0),
        new Vector3(1f,0.5f,0),
        new Vector3(1f,0.5f,1),
        new Vector3(0,0.5f,1),
    };
    private readonly int[,] triangulationTable = 
{{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
{3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
{3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
{3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
{9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
{2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
{8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
{4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
{3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
{1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
{4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
{4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
{5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
{2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
{9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
{0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
{2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
{10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
{5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
{5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
{9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
{1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
{10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
{8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
{2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
{7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
{2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
{11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
{5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
{11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
{11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
{9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
{2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
{6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
{3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
{6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
{10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
{6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
{8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
{7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
{3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
{0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
{9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
{8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
{5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
{0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
{6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
{10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
{10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
{8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
{1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
{0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
{10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
{3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
{6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
{9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
{8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
{3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
{6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
{0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
{10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
{10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
{2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
{7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
{7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
{2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
{1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
{11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
{8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
{0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
{7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
{10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
{2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
{6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
{7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
{2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
{10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
{10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
{0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
{7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
{6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
{8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
{9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
{6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
{4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
{10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
{8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
{0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
{1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
{8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
{10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
{4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
{10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
{11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
{9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
{6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
{7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
{3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
{7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
{3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
{6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
{9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
{1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
{4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
{7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
{6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
{3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
{0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
{6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
{0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
{11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
{6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
{5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
{9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
{1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
{1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
{10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
{0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
{5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
{10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
{11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
{9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
{7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
{2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
{8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
{9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
{9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
{1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
{9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
{5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
{0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
{10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
{2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
{0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
{0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
{9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
{5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
{3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
{5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
{8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
{0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
{9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
{1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
{3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
{4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
{9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
{11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
{11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
{2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
{9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
{3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
{1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
{4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
{3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
{0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
{1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}};
**/
}
