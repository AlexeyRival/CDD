using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
public class hive : NetworkBehaviour
{
    [SyncVar]
    public int hp;
    public bool egg;
    private int maxhp;
    public Slider hpslider;
    public GameObject hitsphere,diesphere;
    private HashSet<int> infos;
    private float damagemultiplier=1f;

    [Command]
    private void CmdDmg(int dmg)
    {
        hp -= dmg;
    }
    public void Dmg(int dmg, GameObject collider)
    {
        hpslider.value = (hp - dmg) / (maxhp * 1f);
    }
    private void Start()
    {
        maxhp = hp;
        if (isServer)
        {
            infos = new HashSet<int>();
        }
    }
    private void Update()
    {
        damagemultiplier = 1f;
        if (player.players != null) for (int i = 0; i < player.players.Count; ++i)
            {
                for (int ii = 0; ii < player.players[i].damageinfos.Count; ++ii)
                {
                    if (player.players[i].damageinfos[ii].netid == netId)
                    {
                        if (!infos.Contains(player.players[i].damageinfos[ii].unid))
                        {
                            infos.Add(player.players[i].damageinfos[ii].unid);
                            CmdDmg(player.players[i].damageinfos[ii].dmgamout);
                        }
                    }
                }
            }
        player plr = null;
        if (GameObject.FindGameObjectWithTag("Player")) { plr = GameObject.FindGameObjectWithTag("Player").GetComponent<player>(); }

        if (plr) for (int ii = 0; ii < plr.damageinfos.Count; ++ii)
            {
                if (plr.damageinfos[ii].netid == netId)
                {
                    if (!infos.Contains(plr.damageinfos[ii].unid))
                    {
                        infos.Add(plr.damageinfos[ii].unid);
                        CmdDmg(plr.damageinfos[ii].dmgamout);
                    }
                }
            }
        if (isServer)
        {
            if (hp <= 0) 
            {
                NetworkServer.Destroy(gameObject);
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
        if (!egg)
        {
            GameObject.Find("AIDirector").GetComponent<AIDirector>().Scream();
        }
        else 
        {
            GameObject.Find("AIDirector").GetComponent<AIDirector>().SpawnBoss();
        }
        Destroy(Instantiate(diesphere,transform.position,transform.rotation),5f);   
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (isServer)
        {
            if (collision.gameObject.CompareTag("Destroyer"))
            {
                int dmg = (int)(15 * damagemultiplier);
                CmdDmg(dmg);
                if (hp - dmg < 0) { ++player.thisplayer.kills; }
            }
        }
        if (collision.gameObject.CompareTag("Destroyer"))
        {
            Dmg((int)(15 * damagemultiplier), collision.other.gameObject);
            Destroy(Instantiate(hitsphere, collision.contacts[0].point, transform.rotation), 1f);
        }
        damagemultiplier+=1.5f;
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (isServer)
        {
            if (collision.gameObject.name == "Fire(Clone)")
            {
                Dmg(1, collision.gameObject);
                CmdDmg(1);
                if (hp - 1 < 0) { ++player.thisplayer.kills; }
            }
        }
        if (collision.gameObject.name == "Fire(Clone)")
        {
            Destroy(Instantiate(hitsphere, collision.transform.position, transform.rotation), 1f);
        }
    }
    public void PlaySound(string eventname)
    {
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventname);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        instance.start();
        instance.release();
    }
}
