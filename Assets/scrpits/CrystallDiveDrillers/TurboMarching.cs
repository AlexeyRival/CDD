using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TurboMarching : MonoBehaviour
{

    //необходимое
    public MeshFilter filter;
    public MeshCollider collider;
    public ComputeShader shader, destroyerShader, EnterpriseShader, optshader;
    public int sizeXYZ = 80;
    public float size = 0.05f;
    public float step = 1.25f;
    public float isolevel = 0;
    public bool isDebug;
    private float[] space;
    private Triangle[] tris;
    private int[] trisconnections;
    public bool updateconnections;
    private bool updateconnectionslocal;

    public GameObject[] allRotationObjects, grasses, flowers;

    // Buffers
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer triCountBuffer;
    private ComputeBuffer connectorsBuffer;
    private ComputeBuffer destroybuffer;

    //соединения и поиск пути
    public bool cX, cY, cZ;
    public bool isChecked;
    public float weight;

    //октодеревья
    private const int maxgenerations = 5;
    public List<OctoTree> octos = new List<OctoTree>();

    //кроме основы
    public Generator generator;
    public Vector3 center, modelcenter;
    public List<TurboMarching> neighbors;
    public List<TurboMarching> friends;
    private FastNoiseLite noise, secondnoise, thirdnoise;

    //ссылки на всю траву и прочие объекты
    public List<GPUInstancer.ObjData> decorations;

    public void Start()
    {
        center = transform.position + (new Vector3(sizeXYZ * step, sizeXYZ * step, sizeXYZ * step)) * 0.5f;
        Debug.DrawRay(center, Vector3.up, Color.magenta, 30f);
        decorations = new List<GPUInstancer.ObjData>();
        octos = new List<OctoTree>();
        octos.Add(new OctoTree(0, center, 10f, false));
        Generate();
    }
    public bool CheckUpdate(Vector4[] destroyers)
    {
        int ii = 0;
        Vector3 vec;
        Vector3 cvec;
        bool isChanged = false;
        bool dontchange=false;
        for (int i = 0; i < space.Length; ++i)
        {
            vec = new Vector3((i % sizeXYZ) * step, (i / sizeXYZ % sizeXYZ) * step, (i / sizeXYZ / sizeXYZ) * step);
            for (ii = 0; ii < destroyers.Length; ++ii)
            {
                cvec = new Vector3(destroyers[ii].x, destroyers[ii].y, destroyers[ii].z);
                //if (!generator.IsChunkContainSphere(this,cvec,destroyers[ii].w)) { continue; }
                if (Generator.FastDist(cvec, vec, destroyers[ii].w* destroyers[ii].w))
                {
                    dontchange = space[i] < isolevel;
                    space[i] -= destroyers[ii].w;// multipler;
                    isChanged = isChanged || (!dontchange&&space[i] < isolevel);
                }
            }
        }




        return isChanged;
    }
    public void TurboUpdate(Vector3 centerpoint, float radius, Vector4[] points,Vector4 canion) {
        noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
        noise.SetSeed(generator.seed);
        // Debug.DrawLine(point, center, Color.green, 10f);
        //point = new Vector3(point.x - transform.position.x, point.y - transform.position.y, point.z - transform.position.z) / step;


        // radius *= step;

        int numPoints = sizeXYZ * sizeXYZ * sizeXYZ;
        int numVoxelsPerAxis = sizeXYZ;
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
        ComputeBuffer CaveBuffer = new ComputeBuffer(points.Length, sizeof(float) * 4);
        CaveBuffer.SetData(points);

        int threadGroupSize = 8;
        int numThreadsPerAxis = Mathf.CeilToInt((numPoints) / (float)threadGroupSize);
        //int numThreadsPerAxis = Mathf.CeilToInt((numVoxelsPerAxis) / (float)threadGroupSize);
        //print(numThreadsPerAxis);

        pointsBuffer.SetData(space);

        int _kernelindex = destroyerShader.FindKernel("boom");

        destroyerShader.SetBuffer(_kernelindex, "points", pointsBuffer);
        destroyerShader.SetBuffer(_kernelindex, "caves", CaveBuffer);
        destroyerShader.SetVector("worldpos", transform.position);
        destroyerShader.SetFloat("step", step);
        destroyerShader.SetInt("numPointsPerAxis", numVoxelsPerAxis);
        destroyerShader.SetFloat("radius", radius);
        destroyerShader.SetVector("desPoint", centerpoint);
        destroyerShader.SetVector("canion", canion);

        //destroyerShader.Dispatch(_kernelindex,numThreadsPerAxis,numThreadsPerAxis,numThreadsPerAxis);
        destroyerShader.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);

        space = new float[numPoints];
        pointsBuffer.GetData(space, 0, 0, numPoints);
        //pointsBuffer.GetData(_bspace);
        //for (int i = 0; i < space.Length; ++i) { space[i] = _bspace[i]; }
        int id;
        for (int i = 0; i < 40; ++i) {
            id = Random.Range(0, space.Length);
            if (space[id] < isolevel - 0.05f)
            {
                generator.bugspawnpoints.Add(GetXYZfromId(id, step) + new Vector3(transform.position.x, transform.position.y, transform.position.z));
                if (Random.Range(0, 50) == 0) { generator.startpoints.Add(GetXYZfromId(id, step) + new Vector3(transform.position.x, transform.position.y, transform.position.z)); }
            }
            if (space[id] > isolevel && space[id] < isolevel + 0.05f && Random.Range(0, 5) == 0)
            {
                Debug.DrawLine(GetXYZfromId(id, step) + new Vector3(transform.position.x, transform.position.y, transform.position.z), center, Color.cyan, 10f);
                generator.orepoints.Add(GetXYZfromId(id, step) + new Vector3(transform.position.x, transform.position.y, transform.position.z));
            }
        }

        pointsBuffer.Release();
        CaveBuffer.Release();
        CaveBuffer.Dispose();
    }
    private Vector3 GetXYZfromId(int id,float scale) 
    {
        return new Vector3((id % sizeXYZ) * scale, (id / sizeXYZ % sizeXYZ) * scale, (id / sizeXYZ / sizeXYZ) * scale);
    }
    public void Generate()
    {
        space = new float[sizeXYZ * sizeXYZ * sizeXYZ];
        noise = new FastNoiseLite();
        secondnoise = new FastNoiseLite();
        thirdnoise = new FastNoiseLite();
        noise.SetSeed(generator.seed);
        secondnoise.SetSeed(-generator.seed);
        thirdnoise.SetSeed((int)(((long)generator.seed) * 125 / 144));
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        secondnoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        thirdnoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);


        int i;

        List<Vector4> cavepoints = new List<Vector4>();
        for (i = 0; i < generator.cavepoints.Count; ++i)
        {
            cavepoints.Add(new Vector4(generator.cavepoints[i].x, generator.cavepoints[i].y, generator.cavepoints[i].z, 18));
        }
        List<Vector4> tunnelpoints = new List<Vector4>();
        for (i = 0; i < generator.tunnelpoints.Count; ++i)
        {
            tunnelpoints.Add(new Vector4(generator.tunnelpoints[i].x, generator.tunnelpoints[i].y, generator.tunnelpoints[i].z, 7));
        }

        int x, y, z;
     /*   for (x = 0; x < sizeXYZ; ++x)
            for (y = 0; y < sizeXYZ; ++y)
                for (z = 0; z < sizeXYZ; ++z)
                {
                    //space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = 10;// new Vector4(x * step, y * step, z * step, 10);
                    space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = (noise.GetNoise((x * step + transform.position.x) * size, (y * step + transform.position.y) * size, (z * step + transform.position.z) * size) + 1f) * 10f;
        //   space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = new Vector4(x * step, y * step, z * step, (noise.GetNoise((x * step + transform.position.x) * size, (y * step + transform.position.y) * size, (z * step + transform.position.z) * size) + 1f) * 10f);
    }*/
        tunnelpoints.AddRange(cavepoints);
        Vector4[] arr = tunnelpoints.ToArray();
        TurboUpdate(Generator.center, 35, arr,generator.canion);
        UpdateMesh();
        //int l = walkpoints.Length;
        float n,fn;
        RaycastHit hit;
        for (i = 0; i < Random.Range(16, 29); ++i)
        {
            Vector3 raypos = center + new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            if (noise.GetNoise(raypos.x, raypos.y, raypos.z) > 0)
            {
                if (Physics.Raycast(raypos, Vector3.down, out hit, 10f) && hit.transform == transform)
                {
                    n = noise.GetNoise(hit.point.x, hit.point.y, hit.point.z);
                    Debug.DrawRay(hit.point, hit.normal, Color.red, 1f);
                    GameObject ob = Instantiate(allRotationObjects[(int)Mathf.Abs(n * 100 + i) % allRotationObjects.Length], hit.point, Quaternion.LookRotation(-hit.normal), transform);
                    ob.transform.Translate(0, 0, 1f);
                }
            }
            else 
            {
                if (Physics.Raycast(raypos, Vector3.up, out hit, 10f) && hit.transform == transform)
                {
                    n = noise.GetNoise(hit.point.x, hit.point.y, hit.point.z);
                    Debug.DrawRay(hit.point, hit.normal, Color.red, 1f);
                    GameObject ob = Instantiate(allRotationObjects[(int)Mathf.Abs(n * 100 + i) % allRotationObjects.Length], hit.point, Quaternion.LookRotation(-hit.normal), transform);
                    ob.transform.Translate(0, 0, 1f);
                }
            }
        }
        if (false)//QualitySettings.GetQualityLevel() > 2) 
        {
            System.Random rotrand = new System.Random();
            for (i = sizeXYZ; i < space.Length - 4; i += 4)
            {
                if (rotrand.Next(0,4)==0&&space[i] < 7 && space[i - sizeXYZ] > 7 && GetXYZfromId(i, step).y != 0 && secondnoise.GetNoise(GetXYZfromId(i, step) + transform.position) < 0.3f && thirdnoise.GetNoise(GetXYZfromId(i, step) + transform.position) < -.4)
                {
                    //Debug.DrawRay(GetXYZfromId(i, step)+transform.position, Vector3.up, Color.green, 10f);
                    decorations.Add(new GPUInstancer.ObjData(GetXYZfromId(i, step) + transform.position, new Vector3(1, 1, 1), Quaternion.Euler(0, rotrand.Next(-180, 180), 0)));
                }
                if (rotrand.Next(0, 4) == 0 && space[i + 1] < 7 && GetXYZfromId(i + 1, step).y != 0 && space[i + 1 - sizeXYZ] > 7 && secondnoise.GetNoise(GetXYZfromId(i + 1, step) + transform.position) < 0.3f && thirdnoise.GetNoise(GetXYZfromId(i + 1, step) + transform.position) < -.4)
                {
                    //Debug.DrawRay(GetXYZfromId(i + 1, step) + transform.position, Vector3.up, Color.green, 10f);
                    decorations.Add(new GPUInstancer.ObjData(GetXYZfromId(i + 1, step) + transform.position, new Vector3(1, 1, 1), Quaternion.Euler(0, rotrand.Next(-180, 180), 0)));
                }
                if (rotrand.Next(0, 4) == 0 && space[i + 2] < 7 && GetXYZfromId(i + 2, step).y != 0 && space[i + 2 - sizeXYZ] > 7 && secondnoise.GetNoise(GetXYZfromId(i + 2, step) + transform.position) < 0.3f && thirdnoise.GetNoise(GetXYZfromId(i + 2, step) + transform.position) < -.4)
                {
                    //Debug.DrawRay(GetXYZfromId(i + 2, step) + transform.position, Vector3.up, Color.green, 10f);
                    decorations.Add(new GPUInstancer.ObjData(GetXYZfromId(i + 2, step) + transform.position, new Vector3(1, 1, 1), Quaternion.Euler(0, rotrand.Next(-180, 180), 0)));
                }
                if (rotrand.Next(0, 4) == 0 && space[i + 3] < 7 && GetXYZfromId(i + 3, step).y != 0 && space[i + 3 - sizeXYZ] > 7 && secondnoise.GetNoise(GetXYZfromId(i + 3, step) + transform.position) < 0.3f && thirdnoise.GetNoise(GetXYZfromId(i, step) + transform.position) < -.4)
                {
                    //Debug.DrawRay(GetXYZfromId(i + 3, step) + transform.position, Vector3.up, Color.green, 10f);
                    decorations.Add(new GPUInstancer.ObjData(GetXYZfromId(i + 3, step) + transform.position, new Vector3(1, 1, 1), Quaternion.Euler(0, rotrand.Next(-180, 180), 0)));
                }
            } 
        }
        //for (i = 0; i < l; ++i) if (i % 3 == 0)// && Random.Range(0, 8) == 0)
        {
                /*
                n = noise.GetNoise(walkpoints[i].pos.x + transform.position.x, walkpoints[i].pos.y + transform.position.y, walkpoints[i].pos.z + transform.position.z);
                fn = secondnoise.GetNoise(walkpoints[i].pos.x + transform.position.x, walkpoints[i].pos.y + transform.position.y, walkpoints[i].pos.z + transform.position.z);
                 
                if (QualitySettings.GetQualityLevel()>2)if (walkpoints[i].angle == 240 && n > 0f) 
                {
                        //  print("тра ва");
                    decorations.Add(walkpoints[i].pos,Instantiate(grasses[(int)(n*20)%grasses.Length], walkpoints[i].pos + transform.position, Quaternion.Euler(0, (fn * 100000f) % 100, 0), transform));
                }
                if (walkpoints[i].angle == 240 && (n < -0.4f||n>0.4f)&& (int)(Mathf.Sin(-n) * 200) % 9 ==0) 
                {
                    Instantiate(allRotationObjects[(int)(Mathf.Abs(n) * 20) % allRotationObjects.Length], walkpoints[i].pos + transform.position+new Vector3(0,-1,0), Quaternion.Euler(180, (fn * 100000f) % 100, 0), transform);
                }
                if (walkpoints[i].angle == 15 && (n < -0.4f || n > 0.4f) && (int)(Mathf.Cos(-n) * 200) % 9 == 0)
                {
                    Instantiate(allRotationObjects[(int)(Mathf.Abs(n) * 20) % allRotationObjects.Length], walkpoints[i].pos + transform.position + new Vector3(0, 1, 0), Quaternion.Euler(0, (fn * 100000f) % 100, 0), transform);
                }
                if (QualitySettings.GetQualityLevel() > 2) if (walkpoints[i].angle == 240 && n > -0.3f&&n<0 && (int)(Mathf.Sin(-n) * 200) % 7 ==0)
                {
                    decorations.Add(walkpoints[i].pos, Instantiate(flowers[(int)(-n * 20) % flowers.Length], walkpoints[i].pos + transform.position, Quaternion.Euler(0, (fn*100000f)%100, 0), transform));
                }*/
                //else print(walkpoints[i].angle);
            }
        /*
        for (x = 0; x < sizeXYZ; ++x)
            for (y = 0; y < sizeXYZ; ++y)
                for (z = 0; z < sizeXYZ; ++z)
                {
                    space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = new Vector4(x * step, y * step, z * step, (noise.GetNoise((x * step + transform.position.x) * size, (y * step + transform.position.y) * size, (z * step + transform.position.z) * size) + 1f) * 10f);
                    //space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ] = new Vector4(x*step,y * step, z * step, Mathf.Sin((x * step + transform.position.x) * size+ (y * step + transform.position.y) * size+ (z * step + transform.position.z) * size));
                }*/

    }
    private void OnDestroy()
    {
        if (pointsBuffer != null) pointsBuffer.Dispose();
        if (triangleBuffer != null) triangleBuffer.Dispose();
        if (triCountBuffer != null) triCountBuffer.Dispose();
        if (triangleBuffer != null) triangleBuffer.Dispose();
    }
    public void UpdateMesh() { UpdateMesh(new Vector4[0]); }
    public void UpdateMesh(Vector4[] destroyers)
    {
        bool justup=false;
        int numPoints = sizeXYZ * sizeXYZ * sizeXYZ;
        int numVoxelsPerAxis = sizeXYZ - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        int[] cons = new int[4];
        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed

        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 9, ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        connectorsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.Raw);

        if (destroyers.Length == 0)
        {
            destroyers = new Vector4[] { new Vector4(0, 0, 0, 0) };
            justup = true;
        }
        else
        {
            Vector3 bv;
            for (int i = 0; i < destroyers.Length; ++i) 
            {
                bv = new Vector3(destroyers[i].x, destroyers[i].y, destroyers[i].z) - transform.position;
                destroyers[i] = new Vector4(bv.x, bv.y, bv.z, destroyers[i].w);
            }
        }
        destroybuffer = new ComputeBuffer(destroyers.Length, sizeof(float) * 4, ComputeBufferType.Raw);
        int threadGroupSize = 8;

        Mesh mesh = new Mesh();

        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);
        bool changed = false;
        //int numThreadsPerAxis = 8;
        int _kernelindex;
        if (customNetworkHUD.DestroyMode)
        {
            pointsBuffer.SetData(space);
            connectorsBuffer.SetData(cons);
            destroybuffer.SetData(destroyers);

            _kernelindex = shader.FindKernel("Dest");
            shader.SetBuffer(_kernelindex, "points", pointsBuffer);
            shader.SetBuffer(_kernelindex, "destroyers", destroybuffer);
            shader.SetBuffer(_kernelindex, "connectors", connectorsBuffer);
            shader.SetInt("numPointsPerAxis", sizeXYZ);
            shader.SetFloat("scale", step);
            shader.Dispatch(_kernelindex, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
            // получение изменения
            pointsBuffer.GetData(space);
            connectorsBuffer.GetData(cons);
            changed = cons[3] == 1;
        }
        else 
        {
            changed = CheckUpdate(destroyers);
        }
        if (changed||justup)
        {
            pointsBuffer.SetData(space);
            _kernelindex = optshader.FindKernel("March");
            triangleBuffer.SetCounterValue(0);
            optshader.SetBuffer(_kernelindex, "points", pointsBuffer);
            optshader.SetBuffer(_kernelindex, "triangles", triangleBuffer);
            optshader.SetBuffer(_kernelindex, "connectors", connectorsBuffer);
            optshader.SetBuffer(_kernelindex, "destroyers", destroybuffer);
            optshader.SetInt("numPointsPerAxis", sizeXYZ);
            optshader.SetFloat("isoLevel", isolevel);
            optshader.SetFloat("scale", step);
            optshader.SetInt("seed", generator.seed);
            optshader.SetVector("chunkpos", transform.position);

            optshader.Dispatch(_kernelindex, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

            //навигационные кубы


            // Get number of triangles in the triangle buffer
            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
            int[] triCountArray = { 0 };
            triCountBuffer.GetData(triCountArray);
            int numTris = triCountArray[0];


            // Get triangle data from shader
            Triangle[] tris = new Triangle[numTris];
            triangleBuffer.GetData(tris, 0, 0, numTris);

            // получение коннекторов
            connectorsBuffer.GetData(cons);
            cX = cons[0] == 1;
            cY = cons[1] == 1;
            cZ = cons[2] == 1;

            pointsBuffer.GetData(space, 0, 0, space.Length);

            mesh.Clear();

            var vertices = new Vector3[numTris * 3];
            var meshTriangles = new int[numTris * 3];

            for (int i = 0; i < numTris; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    meshTriangles[i * 3 + j] = i * 3 + j;
                    vertices[i * 3 + j] = tris[i][j];
                }
            }
            mesh.vertices = vertices;
            //mesh.color TODO !!!!!!!
            mesh.triangles = meshTriangles;
            //print(mesh.triangles.Length);
            mesh.RecalculateTangents();
            mesh.RecalculateNormals();
            //mesh.RecalculateBounds();
            mesh.bounds = new Bounds(new Vector3(4.875f, 4.875f, 4.875f), new Vector3(9.75f, 9.75f, 9.75f));

            filter.mesh = mesh;
            collider.sharedMesh = mesh;

            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
            connectorsBuffer.Release();
            connectorsBuffer.Dispose();
            destroybuffer.Release();
            destroybuffer.Dispose();
            //свет
            //UpdateLight();

            //навигация
            //UpdateNav();
        }
        else
        {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
            connectorsBuffer.Release();
            connectorsBuffer.Dispose();
            destroybuffer.Release();
            destroybuffer.Dispose();
        }
        UpdateOctos();
    }

    private void PlayOneShot(string eventname)
    {
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventname);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        instance.start();
        instance.release();
    }
    public void UpdateOctos()
    {
        octos = new List<OctoTree>();
        if (filter.mesh.vertexCount == 0) { return; }
        octos.Add(new OctoTree(0, center, 10f, false));
        int vc = GetComponent<MeshFilter>().mesh.vertexCount;
        bool isContains;
        List<OctoTree> buflist;
        for (int k = 0; k < maxgenerations; ++k) 
        {
            buflist = new List<OctoTree>();
            for (int i = 0; i < octos.Count; ++i) if (!octos[i].isChecked)
                {
                    octos[i].isChecked = true;
                    if (Physics.CheckBox(octos[i].position, new Vector3(octos[i].size*0.49f, octos[i].size * 0.49f, octos[i].size * 0.49f),Quaternion.identity,LayerMask.GetMask("Water")))
                    {
                        if (k < maxgenerations - 1)
                        {
                            buflist.AddRange(octos[i].Subdevide());
                        }
                        else
                        {
                            octos[i].isContain = true;
                        }
                    }
                }
             octos.AddRange(buflist);  //else { octos.AddRange(buflist); }
        }
        modelcenter = new Vector3();
        int c=0;
        for (int i = 0; i < octos.Count; ++i) if(octos[i].isContain)
        {
            modelcenter += octos[i].position;
                ++c;
        }
        modelcenter /= c;

    }
    public void UpdateFriends()
    {
        friends = new List<TurboMarching>();
        Collider[] colliders = Physics.OverlapBox(center, new Vector3(6, 6, 6));
        if (colliders.Length > 0) 
        {
            for (int i = 0; i < colliders.Length; ++i) if (colliders[i].GetComponent<TurboMarching>() && colliders[i].GetComponent<TurboMarching>() != this)
                {
                    friends.Add(colliders[i].GetComponent<TurboMarching>());
                }
        }
        /*
        for (int i = 0; i < neighbors.Count; ++i)
        {
            if (neighbors[i].transform.position.x < transform.position.x && cX) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.x > transform.position.x && neighbors[i].cX) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.y < transform.position.y && cY) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.y > transform.position.y && neighbors[i].cY) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.z < transform.position.z && cZ) { friends.Add(neighbors[i]); continue; }
            if (neighbors[i].transform.position.z > transform.position.z && neighbors[i].cZ) { friends.Add(neighbors[i]); continue; }
        }
        for (int i = 0; i < friends.Count; ++i)
        {
            //Debug.DrawLine(center, friends[i].center, Color.magenta, 10f);
        }*/
    }
    public List<Vector3> MakeOctoPath(Vector3 from,Vector3 to) 
    {
        List<Vector3> path = new List<Vector3>();
        float min=9999f;
        int minid=-1;
        int startid=-1;
        float startmin=9999f;
        for (int i = 0; i < octos.Count; ++i)if(octos[i].isContain&&!octos[i].haveChild)
        {
            if (Vector3.Distance(octos[i].position,from)<startmin)//generator.IsCubeContainPoint(octos[i].position, octos[i].size*0.5f, from)) 
            {
                    startid = i;
                    startmin = Vector3.Distance(octos[i].position, from);
            }
            if (Vector3.Distance(octos[i].position, to)<min) 
            {
                    minid = i;
                    min = Vector3.Distance(octos[i].position, to);
                }
        }
        if (minid != -1) 
        {
            Debug.DrawLine(octos[minid].position + new Vector3(-octos[minid].size * 0.5f, -octos[minid].size * 0.5f, -octos[minid].size * 0.5f), octos[minid].position + new Vector3(-octos[minid].size * 0.5f, -octos[minid].size * 0.5f, octos[minid].size * 0.5f), Color.cyan, 10f);
            Debug.DrawLine(octos[minid].position + new Vector3(-octos[minid].size * 0.5f, -octos[minid].size * 0.5f, octos[minid].size * 0.5f), octos[minid].position + new Vector3(-octos[minid].size * 0.5f, octos[minid].size * 0.5f, -octos[minid].size * 0.5f), Color.cyan, 10f);
            Debug.DrawLine(octos[minid].position + new Vector3(-octos[minid].size * 0.5f, octos[minid].size * 0.5f, -octos[minid].size * 0.5f), octos[minid].position + new Vector3(-octos[minid].size * 0.5f, octos[minid].size * 0.5f, octos[minid].size * 0.5f), Color.cyan, 10f);
            Debug.DrawLine(octos[minid].position + new Vector3(-octos[minid].size * 0.5f, octos[minid].size * 0.5f, octos[minid].size * 0.5f), octos[minid].position + new Vector3(octos[minid].size * 0.5f, -octos[minid].size * 0.5f, -octos[minid].size * 0.5f), Color.cyan, 10f);
            Debug.DrawLine(octos[minid].position + new Vector3(octos[minid].size * 0.5f, -octos[minid].size * 0.5f, -octos[minid].size * 0.5f), octos[minid].position + new Vector3(octos[minid].size * 0.5f, -octos[minid].size * 0.5f, octos[minid].size * 0.5f), Color.cyan, 10f);
            Debug.DrawLine(octos[minid].position + new Vector3(octos[minid].size * 0.5f, -octos[minid].size * 0.5f, octos[minid].size * 0.5f), octos[minid].position + new Vector3(octos[minid].size * 0.5f, octos[minid].size * 0.5f, -octos[minid].size * 0.5f), Color.cyan, 10f);
            Debug.DrawLine(octos[minid].position + new Vector3(octos[minid].size * 0.5f, octos[minid].size * 0.5f, -octos[minid].size * 0.5f), octos[minid].position + new Vector3(octos[minid].size * 0.5f, octos[minid].size * 0.5f, octos[minid].size * 0.5f), Color.cyan, 10f);
            Debug.DrawLine(octos[minid].position + new Vector3(octos[minid].size * 0.5f, octos[minid].size * 0.5f, octos[minid].size * 0.5f), octos[minid].position + new Vector3(-octos[minid].size * 0.5f, -octos[minid].size * 0.5f, -octos[minid].size * 0.5f), Color.cyan, 10f);

        }
        if (startid != -1) 
        {
                Debug.DrawLine(octos[startid].position + new Vector3(-octos[startid].size*0.5f, -octos[startid].size*0.5f, -octos[startid].size*0.5f), octos[startid].position + new Vector3(-octos[startid].size*0.5f, -octos[startid].size*0.5f, octos[startid].size*0.5f), Color.green, 10f);
                Debug.DrawLine(octos[startid].position + new Vector3(-octos[startid].size*0.5f, -octos[startid].size*0.5f, octos[startid].size*0.5f), octos[startid].position + new Vector3(-octos[startid].size*0.5f, octos[startid].size*0.5f, -octos[startid].size*0.5f), Color.green, 10f);
                Debug.DrawLine(octos[startid].position + new Vector3(-octos[startid].size*0.5f, octos[startid].size*0.5f, -octos[startid].size*0.5f), octos[startid].position + new Vector3(-octos[startid].size*0.5f, octos[startid].size*0.5f, octos[startid].size*0.5f), Color.green, 10f);
                Debug.DrawLine(octos[startid].position + new Vector3(-octos[startid].size*0.5f, octos[startid].size*0.5f, octos[startid].size*0.5f), octos[startid].position + new Vector3(octos[startid].size*0.5f, -octos[startid].size*0.5f, -octos[startid].size*0.5f), Color.green, 10f);
                Debug.DrawLine(octos[startid].position + new Vector3(octos[startid].size*0.5f, -octos[startid].size*0.5f, -octos[startid].size*0.5f), octos[startid].position + new Vector3(octos[startid].size*0.5f, -octos[startid].size*0.5f, octos[startid].size*0.5f), Color.green, 10f);
                Debug.DrawLine(octos[startid].position + new Vector3(octos[startid].size*0.5f, -octos[startid].size*0.5f, octos[startid].size*0.5f), octos[startid].position + new Vector3(octos[startid].size*0.5f, octos[startid].size*0.5f, -octos[startid].size*0.5f), Color.green, 10f);
                Debug.DrawLine(octos[startid].position + new Vector3(octos[startid].size*0.5f, octos[startid].size*0.5f, -octos[startid].size*0.5f), octos[startid].position + new Vector3(octos[startid].size*0.5f, octos[startid].size*0.5f, octos[startid].size*0.5f), Color.green, 10f);
                Debug.DrawLine(octos[startid].position + new Vector3(octos[startid].size*0.5f, octos[startid].size*0.5f, octos[startid].size*0.5f), octos[startid].position + new Vector3(-octos[startid].size*0.5f, -octos[startid].size*0.5f, -octos[startid].size*0.5f), Color.green, 10f);
        }
        // path.Add(octos[startid].position);
        // path.Add(octos[minid].position);

        if (minid != -1 && startid != -1) 
        {
            for (int k = 0; k < 10; ++k)
            {
                int mid = -1;
                min = 9999f;
                path.Add(octos[startid].position);
                for (int i = 0; i < octos.Count; ++i)
                {
                    if ((octos[i].position- to).sqrMagnitude < (octos[startid].position- to).sqrMagnitude && (octos[i].position- octos[startid].position).sqrMagnitude < min)
                    {
                        min = (octos[i].position - octos[startid].position).sqrMagnitude;
                        mid = i;
                    }
                }
                if (mid != -1) { startid = mid; } else { break; }
            }
        }
/*
        if (minid != -1&&startid!=-1)
        {
            for (int k = 0; k < 10; ++k)
            {
                int mid = -1;
                min = 9999f;
                path.Add(octos[startid].position);
                for (int i = 0; i < octos.Count; ++i) 
                    {
                        if (Vector3.Distance(octos[i].position, to) < Vector3.Distance(octos[startid].position, to) && Vector3.Distance(octos[i].position, octos[startid].position) < min) 
                        {
                            min = Vector3.Distance(octos[i].position, octos[startid].position);
                            mid = i;
                        }
                    }
                if (mid != -1) { startid = mid; } else { break; }
            }
        }
        else 
        {
            //throw new System.Exception("PathNotFoundException, лол");
        }*/
        return path;
    }
    private void OnDrawGizmos()
    {
        if (isDebug&&Application.isPlaying)
        {
            Gizmos.color = new Color(0.576f, 0.439f, 0.203f,0.3f);
            Vector3 one = new Vector3(0.125f, 0.125f, 0.125f);
            int x, y, z;/*
            for (x = 0; x < sizeXYZ; ++x)
                for (y = 0; y < sizeXYZ; ++y)
                    for (z = 0; z < sizeXYZ; ++z)
                        //if (space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w > isolevel)//&& space[x + y * sizeXYZ + z * sizeXYZ * sizeXYZ].w< isolevel+0.01f)
                        {
                            Gizmos.DrawCube(new Vector3((x * step + transform.position.x), (y * step + transform.position.y), (z * step + transform.position.z)), one);
                        }*/
            /*for (int i = 0; i < walkpoints.Length; ++i)
            {
                Gizmos.color = new Color(walkpoints[i].weight*0.01f,0,1- walkpoints[i].weight*0.01f);
                Gizmos.DrawCube(new Vector3((walkpoints[i].pos.x  + transform.position.x), (walkpoints[i].pos.y  + transform.position.y), (walkpoints[i].pos.z  + transform.position.z)), one);
               for (int ii = 0; ii < walkpointneighbors[i].Length; ++ii) if(walkpointneighbors[i][ii]!=-1)
                {
                    Debug.DrawLine(walkpoints[i].pos+transform.position,walkpoints[walkpointneighbors[i][ii]].pos + transform.position, Gizmos.color);
                }
            }*/
            
        }
        if (Application.isEditor)
        {
            Gizmos.color = new Color(0.6f, 0.1f, 0.1f, 0.3f);
            Gizmos.DrawCube(transform.position + new Vector3(5, 5, 5), new Vector3(10, 10, 10));
         //   if (updateconnectionslocal != updateconnections)
            {
           //     updateconnectionslocal = updateconnections;
           //     filter = GetComponent<MeshFilter>();
           //     collider = GetComponent<MeshCollider>();
            }
        }
        if (Application.isEditor&&isDebug)
        {
            for (int i = 0; i < octos.Count; ++i) 
            {
                //Gizmos.color = octos[i].isContain ? new Color(1,0,0,0.5f) : new Color(1, 1/(octos[i].generation*0.75f), 1, 0.25f);
                //Gizmos.DrawWireCube(octos[i].position, new Vector3(octos[i].size, octos[i].size, octos[i].size));
                Gizmos.color = octos[i].isContain ? new Color(1,0,0,0.5f) : new Color(1, 1/(octos[i].generation*0.75f), 1, 0.5f);
                if (octos[i].parent != null) 
                {
                    Gizmos.DrawLine(octos[i].position, octos[i].parent.position);
                }
            }
          //  bool isSelected = Selection.Contains(gameObject);
          //  Gizmos.color = isSelected ? new Color(0.168f, 0.5814968f, 0.93741f, 0.24f) : new Color(0.465f, 0.21978f, 0.1678f, 0.24f);
            /*if(cX)Debug.DrawRay(center,new Vector3(-10,0,0),Color.blue);
            if(cY)Debug.DrawRay(center,new Vector3(0,-10,0),Color.blue);
            if(cZ)Debug.DrawRay(center,new Vector3(0,0,-10),Color.blue);*/
            //Gizmos.DrawCube(transform.position + new Vector3(4.875f, 4.875f, 4.875f), new Vector3(9.75f, 9.75f, 9.75f));
        }
    }
   /* private void OnGUI()
    {
        if(isDebug)GUI.Box(new Rect(Screen.width - 200, Screen.height - 20, 200, 20), ""+walkpoints.Length);
    }*/
    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
    public struct Walkpoint
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 pos;
        public float weight;
        public int angle;
        public int iter;
    }
    public class OctoTree
    {
        public int generation;
        public Vector3 position;
        public float size;
        public OctoTree parent;
        public bool isContain;
        public bool isChecked;
        public bool haveChild;
        public int childCount;
        public List<OctoTree> childs;

        public OctoTree(int generation, Vector3 position, float size, bool isContain)
        {
            this.generation = generation;
            this.position = position;
            this.size = size;
            this.isContain = isContain;
            childs = new List<OctoTree>();
        }
        public OctoTree(int generation, Vector3 position, float size, bool isContain, OctoTree parent)
        {
            this.generation = generation;
            this.position = position;
            this.size = size;
            this.isContain = isContain;
            this.parent = parent;
            childs = new List<OctoTree>();
        }
        public List<OctoTree> Subdevide() 
        {
            haveChild = true;
            childs = new List<OctoTree>();
            float s = size * 0.25f;
            float c = size * 0.5f;
            childs.Add(new OctoTree(generation + 1, position + new Vector3(s, s, s), c, false,this));
            childs.Add(new OctoTree(generation + 1, position + new Vector3(-s, s, s), c, false, this));
            childs.Add(new OctoTree(generation + 1, position + new Vector3(s, s, -s), c, false, this));
            childs.Add(new OctoTree(generation + 1, position + new Vector3(-s, s, -s), c, false, this));

            childs.Add(new OctoTree(generation + 1, position + new Vector3(s, -s, s), c, false, this));
            childs.Add(new OctoTree(generation + 1, position + new Vector3(-s, -s, s), c, false, this));
            childs.Add(new OctoTree(generation + 1, position + new Vector3(s, -s, -s), c, false, this));
            childs.Add(new OctoTree(generation + 1, position + new Vector3(-s, -s, -s), c, false, this));
            for (int i = 0; i < childs.Count; ++i) 
            {
                childs[i].parent = this;
            }
            childCount = childs.Count;
            return childs;
        }
    }
    public static readonly Vector3[] neighborsTable = {
        new Vector3(-0.5f,-0.5f,-0.5f),
        new Vector3(-0.5f,-0.5f,0),
        new Vector3(-0.5f,-0.5f,0.5f),
        new Vector3(-0.5f,0,-0.5f),
        new Vector3(-0.5f,0,0),
        new Vector3(-0.5f,0,0.5f),
        new Vector3(-0.5f,0.5f,-0.5f),
        new Vector3(-0.5f,0.5f,0),
        new Vector3(-0.5f,0.5f,0.5f),
        new Vector3(0,-0.5f,-0.5f),
        new Vector3(0,-0.5f,0),
        new Vector3(0,-0.5f,0.5f),
        new Vector3(0,0,-0.5f),
        new Vector3(0,0,0.5f),
        new Vector3(0,0.5f,-0.5f),
        new Vector3(0,0.5f,0),
        new Vector3(0,0.5f,0.5f),
        new Vector3(0.5f,-0.5f,-0.5f),
        new Vector3(0.5f,-0.5f,0),
        new Vector3(0.5f,-0.5f,0.5f),
        new Vector3(0.5f,0,-0.5f),
        new Vector3(0.5f,0,0),
        new Vector3(0.5f,0,0.5f),
        new Vector3(0.5f,0.5f,-0.5f),
        new Vector3(0.5f,0.5f,0),
        new Vector3(0.5f,0.5f,0.5f)
    };
}
