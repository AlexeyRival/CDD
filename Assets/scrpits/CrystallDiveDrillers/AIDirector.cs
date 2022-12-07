using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AIDirector : NetworkBehaviour
{
    public GameObject bug, mimicbug, opressor, ironOpressor, boss;
    public Generator generator;
    public MissionMenuController missioncontroller;
    public difficulty currentdifficulty;
    public Slider bossbar;
    public bool moved, debugMode;
    private const string version = "Дионис";
    public float time, swarmmeter;
    private int phase = 0;
    private bool isBossSpawn;
    private GameObject thisboss;
    [SyncVar]
    private float bosshp=0f;
    public void SpawnSmallBugs()
    {
        RaycastHit hit;
        GameObject ob;
        bool isMimicBug;
        for (int i = 0; i < 5; ++i)
        {
            isMimicBug = Random.Range(0, 100) < 15;
            Physics.Raycast(generator.bugspawnpoints[Random.Range(0, generator.bugspawnpoints.Count)], Vector3.down, out hit);
            ob = Instantiate(isMimicBug ? mimicbug : bug, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
            if (i == 0) {
                ob.GetComponent<bug>().agression *= 1.1f;
                ob.GetComponent<bug>().speed *= 1.1f;
                ob.GetComponent<bug>().hp = (int)(ob.GetComponent<bug>().hp * 1.1f);
            }
            NetworkServer.Spawn(ob);
        }
    }
    public void SpawnOpressor()
    {
        RaycastHit hit;
        bool isIronOpressor = Random.Range(0, 100) < 15;
        Physics.Raycast(generator.bugspawnpoints[Random.Range(0, generator.bugspawnpoints.Count)], Vector3.down, out hit);
        GameObject ob = Instantiate(isIronOpressor ? ironOpressor : opressor, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
        if (isIronOpressor)
        {
            ob.GetComponent<bug>().agression *= 1.1f;
            ob.GetComponent<bug>().speed *= 1.1f;
            ob.GetComponent<bug>().hp = (int)(ob.GetComponent<bug>().hp * 1.1f);
        }
        NetworkServer.Spawn(ob);
    }
    public void SpawnBoss() 
    {
        RaycastHit hit;
        Physics.Raycast(generator.bugspawnpoints[Random.Range(0, generator.bugspawnpoints.Count)], Vector3.down, out hit);
        NetworkServer.Spawn(Instantiate(boss, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0)));
    }
    public void SpawnMidBugs() 
    {
        for (int i = 0; i < Random.Range(2, 4); ++i) {
            SpawnSmallBugs();
        }
    }
    public void SpawnBigBugs() 
    {
        for (int i = 0; i < Random.Range(4, 8); ++i) {
            SpawnSmallBugs();
        }
    }
    public void Scream() 
    {
        time = 101f;
        swarmmeter += 15f*currentdifficulty.SwarmmeterMultiplier;
    }
    public void ResetAll() {
        phase = 0;
        time = 0;
        swarmmeter = 0;
    }
    void Update()
    {
        if (isServer)
        {
            if (!moved)
            {
                if (generator.isGeneratingCompleted)
                {
                    SpawnSmallBugs();
                    currentdifficulty = missioncontroller.difficulties[generator.questdifficulty];
                    moved = true;
                }
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.RightControl)) { debugMode = true; }
            if (debugMode)
            {
                if (Input.GetKeyDown(KeyCode.F1)) { SpawnSmallBugs(); }
                if (Input.GetKeyDown(KeyCode.F2)) { SpawnMidBugs(); }
                if (Input.GetKeyDown(KeyCode.F3)) { SpawnBigBugs(); }
                if (Input.GetKeyDown(KeyCode.F4)) { SpawnOpressor(); }
                if (Input.GetKeyDown(KeyCode.F5)) { SpawnBoss(); }
                if (Input.GetKeyDown(KeyCode.F10)) { ResetAll(); }
                if (Input.GetKeyDown(KeyCode.F11)) { gameObject.SetActive(false); }
                if (Input.GetKeyDown(KeyCode.F12)) { debugMode = false; }
            }
            if (moved)
            {
                time += Time.deltaTime * currentdifficulty.TimerMultiplier * (generator.currentquest == Generator.questtype.Бойня ? 2 : 1);
                swarmmeter += Time.deltaTime * 0.05f*currentdifficulty.SwarmmeterMultiplier;
                if (time >= 50&&phase==0) { 
                    if (Random.Range(0, 100) < 30) { SpawnSmallBugs(); time = 0; } else { phase = 1; }
                }
                if (time >= 95&&time<=96) { MissionControlVoice.only.PlayReplica(17); }
                if (time >= 100) { 
                    phase = 0;time = 0;
                    if (currentdifficulty.OpressorSpawnChance!=0&&Random.Range(0, 100) < currentdifficulty.OpressorSpawnChance + swarmmeter * 0.1f&&Random.Range(0,2)!=0) { 
                        SpawnOpressor();
                        if (currentdifficulty.OpressorSpawnChance + swarmmeter * 0.1f > 110 && Random.Range(0, 100) < 10) { phase = 1;time = 101; SpawnMidBugs();}else
                        if (currentdifficulty.OpressorSpawnChance + swarmmeter * 0.1f > 90 && Random.Range(0, 100) < 10) { phase = 1;time = 101; }else
                        if (currentdifficulty.OpressorSpawnChance + swarmmeter * 0.1f > 80) { SpawnMidBugs(); }else
                        if (currentdifficulty.OpressorSpawnChance + swarmmeter * 0.1f > 50) { SpawnSmallBugs(); }

                    }else
                    if (Random.Range(0, 100) < 80+swarmmeter) {
                        if (swarmmeter < 20)
                        {
                            SpawnMidBugs();
                        }
                        else 
                        {
                            SpawnBigBugs();
                        }
                    } 
                    
                }
                if (!generator.isGeneratingCompleted) { moved = false;ResetAll(); }
            }
        }
        else 
        {
            if (isBossSpawn) { isBossSpawn = false; bossbar.gameObject.SetActive(false); }
        }
    }
    private void OnGUI()
    {
        if (debugMode) {
            GUI.Box(new Rect(Screen.width * 0.5f - 100, 40, 200, 25), "DEBUG MODE - "+version);
            GUI.Box(new Rect(0, 0, Screen.width / 12 * 1, 40), "F1 - SpawnSmallBugs");
            GUI.Box(new Rect(Screen.width / 12 * 1, 0, Screen.width / 12 * 1, 40), "F2 - SpawnMidBugs");
            GUI.Box(new Rect(Screen.width / 12 * 2, 0, Screen.width / 12 * 1, 40), "F3 - SpawnBigBugs");
            GUI.Box(new Rect(Screen.width / 12 * 3, 0, Screen.width / 12 * 1, 40), "F4 - SpawnOpressor");
            GUI.Box(new Rect(Screen.width / 12 * 4, 0, Screen.width / 12 * 1, 40), "F5 - SpawnBoss");
            GUI.Box(new Rect(Screen.width / 12 * 9, 0, Screen.width / 12 * 1, 40), "F10 - Сбросить всё");
            GUI.Box(new Rect(Screen.width / 12 * 10, 0, Screen.width / 12 * 1, 40), "F11 - Отключить "+version);
            GUI.Box(new Rect(Screen.width / 12 * 11, 0, Screen.width / 12 * 1, 40), "F12 - Отключить Debug Mode");
            GUI.Box(new Rect(Screen.width - 200, Screen.height * 0.4f, 200, 30), "Время: " + time);
            GUI.Box(new Rect(Screen.width - 200, Screen.height * 0.4f + 30, 200, 30), "Счётчик Роя: " + swarmmeter);
            GUI.Box(new Rect(Screen.width - 200, Screen.height * 0.4f + 60, 200, 30), "Фаза: " + phase);
        }
    }
}
