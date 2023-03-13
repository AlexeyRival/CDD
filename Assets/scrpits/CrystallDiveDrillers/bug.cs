using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class bug : NetworkBehaviour
{
    public state State;
    [SyncVar]
    public bool isStartWalking;
    public float speed = 3.5f;
    public float agression = 1f;
    public float scale = 1f;
    public float upper = 1f;
    public bool isBoss;
    public bool isPreSpawn;
    public SyncListVector3 path = new SyncListVector3();
    [SyncVar]
    private int currentpoint;
    public Transform rotator;
    public bool isBonesDrop = true;
    public GameObject attacksphere,spray, hitsphere, donthitsphere,spawneffect;
    public Slider hpbar;
    public GameObject back;//спина
    public GameObject jaw_up, jaw_down;//челюсти
    public GameObject leg_fr, leg_fl, leg_br, leg_bl, leg_cr, leg_cl, hand_l, hand_r;//ляжки
    public GameObject foot_fr, foot_fl, foot_br, foot_bl, foot_cr, foot_cl, arm_l, arm_r;//голени
    public GameObject point_fr, point_fl, point_br, point_bl, point_cr, point_cl, point_ar, point_al;//точки сброса
    public GameObject defpoint_r, defpoint_l;//точки защиты
    public Vector3 v_bl, v_br, v_fl, v_fr, v_cr, v_cl, v_al, v_ar;
    public Vector3 t_bl, t_br, t_fl, t_fr, t_cr, t_cl, t_al, t_ar;
    public Vector3 s_bl, s_br, s_fl, s_fr, s_cr, s_cl, s_al, s_ar;
    private bool lock_bl, lock_br, lock_fl, lock_fr, lock_cl, lock_cr, lock_ar, lock_al;
    private float bfr;
    private RaycastHit hit;
    private float spd;
    private float dropHeight = 4, stepTrashold = 1f;
    public float visiontimer;
    [SyncVar]
    public float attacktimer;
    public int updatetimer;
    private bool isAttack;
    private GameObject target;
    private float dt;
    public int maxhp = 100;
    private int localhp = 100;
    private float spawnedtimer;
    private float lastdamage;
    private int soundtimer;
    private HashSet<int> infos;
    private int i, ii;
    [SyncVar]
    public int hp = 100;
    [ClientRpc]
    private void RpcChangeState(state State) 
    {
        this.State = State;
    }
    [Command]
    private void CmdAttack(int id) {
        if (id == 0)
        {
            GameObject ob = Instantiate(attacksphere, defpoint_l.transform.position, transform.rotation);
            ob.transform.Translate(1f, 0, 0);
            Destroy(ob, 0.1f);
            NetworkServer.Spawn(ob);
        }
        else if(id==1)
        {
            GameObject ob = Instantiate(spray, jaw_down.transform.position, transform.rotation);
            ob.transform.Translate(0, 0, 0.5f);
            Destroy(ob, attacktimer);
            NetworkServer.Spawn(ob);
        }
    }
    #region получение урона
    [Command]
    private void CmdDmg(int dmg) {
        hp -= dmg;
    }
    public void Dmg(int dmg, GameObject collider)
    {
        if (collider == arm_l || collider == arm_r)
        {
            dmg /= 2;
            Destroy(Instantiate(donthitsphere, collider.transform.position, transform.rotation), 1f);
        }
        else
        {
            Destroy(Instantiate(hitsphere, collider.transform.position, transform.rotation), 1f);
        }
        transform.Rotate(0.4f * Random.Range(-1f * dmg, 1f * dmg), 0.4f * Random.Range(-1f * dmg, 1f * dmg), 0.4f * Random.Range(-1f * dmg, 1f * dmg));
        transform.Translate(0.01f * Random.Range(-1f * dmg, 1f * dmg), 0, 0.01f * Random.Range(-1f * dmg, 1f * dmg));
    //    if(isServer)CmdDmg(dmg);
    }
    public void PublicDmg(int dmg, GameObject collider) {
        Dmg(dmg, collider);
    }
    #endregion
    #region ragdoll
    private void DropAll()
    {
        hpbar.gameObject.SetActive(false);
        GetComponent<NetworkTransform>().enabled = false;
        DropChild(transform.GetChild(0));
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        GetComponent<Rigidbody>().mass = 10f;
        GetComponent<Rigidbody>().useGravity = true;

        if(isBonesDrop)GetComponent<Rigidbody>().AddRelativeForce(0,50f,0,ForceMode.Impulse);
        if (Generator.only.currentquest == Generator.questtype.Бойня) { ++Generator.only.questprogress; }
        if (isBoss&& Generator.only.currentquest == Generator.questtype.Ликвидация) { ++Generator.only.questprogress; }
    }
        private void DropChild(Transform trans) {
        if (trans.GetComponent<CapsuleCollider>()) {
            trans.GetComponent<CapsuleCollider>().enabled = true;
            if (!trans.GetComponent<Rigidbody>())
            {
                trans.gameObject.AddComponent<Rigidbody>();
                trans.gameObject.GetComponent<Rigidbody>().mass = GetComponent<Rigidbody>().mass;
                if (isBonesDrop) trans.gameObject.GetComponent<Rigidbody>().AddForce(0, 5f, 0, ForceMode.Impulse);
                if (trans.parent && trans.parent.GetComponent<Rigidbody>()) {
                    trans.gameObject.AddComponent<CharacterJoint>();
                    trans.gameObject.GetComponent<CharacterJoint>().connectedBody = trans.parent.GetComponent<Rigidbody>();
                    trans.gameObject.GetComponent<CharacterJoint>().autoConfigureConnectedAnchor = true;
                }

            }
            else 
            {
                trans.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                trans.GetComponent<Rigidbody>().useGravity = true;
            }
        }
        if (trans.childCount == 0) { return; }
        for (int i = 0; i < trans.childCount; ++i) {
            DropChild(trans.GetChild(i));
        }
    }
    #endregion
    private void SetLeg(GameObject leg, Vector3 vec, Vector3 startpoint, float height)
    {
        return;
        bfr = Mathf.Sin(Vector3.Distance(startpoint, vec)*2f * Mathf.PI) * 1f + height;//*0.5f
        leg.transform.LookAt(new Vector3(vec.x, vec.y + bfr, vec.z));
        leg.transform.Rotate(-90, 90, 90);
    }
    private void SetFoot(GameObject foot, Vector3 vec)
    {
        return;
        foot.transform.LookAt(vec);
        foot.transform.Rotate(-90, 90, 90);
    }
    //TODO ВСЁ ВЕРНУТЬ
   private Vector3 SlerpLeg(Vector3 startpoint, Vector3 targetpoint, Vector3 vec, out bool locker, float speed)
    {
        locker = false;
        return startpoint;
        if (Vector3.Distance(startpoint, vec) > 0.001f)
        {
            locker = true;
            return Vector3.Slerp(vec, targetpoint, speed);
        }
        else
        {
            locker = false;
            return vec;
        }
    }
    public void SetPath(List<Vector3> path)
    {
        this.path.Clear();
        for (i = 0; i < path.Count; ++i)
        {
            this.path.Add(path[i]);
        }
        currentpoint = 0;
        if (path.Count > 0) isStartWalking = true;
    }
    
    private void Start()
    {
        hp = maxhp;
        localhp = maxhp;
        if (isServer) 
        {
            infos = new HashSet<int>();
        }
        if (!isPreSpawn)
        {
            spawnedtimer = Random.Range(3.5f, 4.5f);
            Destroy(Instantiate(spawneffect, transform.position, transform.rotation), spawnedtimer * 0.5f);
            transform.Translate(0, -spawnedtimer * 2, 0);
            transform.Rotate(-90, 0, 0);
            if (isServer)
            {
                SetPath(new List<Vector3>(new Vector3[] { transform.position + new Vector3(0, spawnedtimer * 2, 0), transform.position + new Vector3(Random.Range(-1f, 1f), spawnedtimer * 2, Random.Range(-1f, 1f)), transform.position + new Vector3(Random.Range(-1f, 1f), spawnedtimer * 2, Random.Range(-1f, 1f)), transform.position + new Vector3(0, spawnedtimer * 2, 0) }));
            }
        }
        soundtimer = 700 + Random.Range(1, 20) * 100;
    }
    void Update()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, rotator.rotation, 3f * Time.deltaTime);//2
        if (isStartWalking) {
            if (currentpoint == path.Count-1) { isStartWalking = false; return; }
            if (Physics.Raycast(transform.position, path[currentpoint]-transform.position, out hit,2f))
            {
                rotator.LookAt(hit.point);
                rotator.transform.Rotate(-20,0,0);
            }
            else 
            {
                rotator.LookAt(path[currentpoint]);
            }

            if (Quaternion.Angle(transform.rotation,rotator.rotation)<90)
            { 
                transform.Translate(0, 0, Time.deltaTime * speed);
                {
                    transform.Translate(0, Time.deltaTime * upper * 1.25f, 0);
                } 
            }

            if (Physics.Raycast(transform.position, -transform.up, out hit, 4f))
            {
                if (hit.distance > 0.8f * scale) { transform.Translate(0, -speed * Time.deltaTime, 0); }
                if (hit.distance < 0.6f * scale) { transform.Translate(0, speed * Time.deltaTime, 0); }
            }
        }
        if (soundtimer == 0) {
            PlayOneShot(!isBoss ? "event:/bugsound" : "event:/bigbugsound");
            soundtimer = 700 + Random.Range(1, 20) * 100;
        } else { --soundtimer; }
        if (isServer)
        {
            if (isStartWalking)
            {
                if (Vector3.Distance(transform.position, path[currentpoint]) < 1.35f*scale)
                {
                    ++currentpoint;
                }
                if (target)
                {
                    dt = Vector3.Distance(target.transform.position, transform.position);
                    if (dt < 3 * (scale * 0.75f))//|| (Vector3.Distance(target.transform.position,path[path.Count-1])>dt*0.5f&&dt>20))
                    {
                        isStartWalking = false;
                    } 
                }
            }
            else
            {
                if (attacktimer <= 0)
                {
                    if (!target)
                    {
                        if (visiontimer <= 0 && GameObject.FindGameObjectsWithTag("Smell").Length > 0)
                        {
                            for (i = 0; i < GameObject.FindGameObjectsWithTag("Smell").Length; ++i)
                            {
                                if (Random.Range(0, 3) == 0 && Vector3.Distance(transform.position, GameObject.FindGameObjectsWithTag("Smell")[i].transform.position) < 40f)
                                {
                                    target = GameObject.FindGameObjectsWithTag("Smell")[i];
                                    visiontimer = 30f;
                                    break;
                                }
                            }
                            if (!target) { visiontimer = Random.Range(0f, .125f); }
                            if (false)// (!target)
                            {
                                for (int i = 0; i < GameObject.FindGameObjectsWithTag("Player").Length; ++i)
                                {
                                    if (Vector3.Distance(transform.position, GameObject.FindGameObjectsWithTag("Player")[i].transform.position) < 10f)
                                    {
                                        target = GameObject.FindGameObjectsWithTag("Player")[i];
                                        visiontimer = 10f;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            State = state.none;
                            RpcChangeState(State);
                        }
                    }
                    else
                    {
                        if (Vector3.Distance(transform.position, target.transform.position) > 3 * (scale*0.5f))
                        {
                            if (updatetimer == 0)
                            {
                                try
                                {
                                    //SetPath(generator.GetPath(transform.position, target.transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f))));
                                    SetPath(Generator.only.GetOctoPath(transform.position, target.transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f))));
                                    //  SetPath(generator.GetFastPath(transform.position, target.transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f))));
                                    if (path.Count == 0) {
                                        //TODO ворует путь у другого жука
                                    }
                                    path.Add(path[path.Count - 1] + new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f)));
                                    for (i = 0; i < path.Count; ++i)
                                    {
                                        path[i] += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
                                    }
                                }
                                catch { updatetimer = 200 + Random.Range(-100, 100); }
                                if (Random.Range(0, 4) != 0)
                                {
                                    State = state.move;
                                    RpcChangeState(State);
                                }
                                else
                                {
                                    State = state.defense;
                                    RpcChangeState(State);
                                }
                            }
                            else
                            {
                                --updatetimer;
                            }
                        }
                        else
                        {
                            rotator.LookAt(target.transform.position - new Vector3(0, target.transform.localScale.y, 0));
                            if (!isBoss)
                            {
                                if (Random.Range(0, 2) != 0)
                                {
                                    State = state.bite;
                                    RpcChangeState(State);
                                }
                                else
                                {
                                    State = state.slash;
                                    RpcChangeState(State);
                                }
                                attacktimer = 2.4f - agression;
                            }
                            else
                            {
                                int r = 2;// Random.Range(0, 3);
                                switch (r) 
                                {
                                    case 0: 
                                        State = state.bite;
                                        attacktimer = 2.4f - agression;
                                        break;
                                    case 1: 
                                        State = state.slash;
                                        attacktimer = 2.4f - agression;
                                        break;
                                    case 2: 
                                            State = state.spray;
                                            attacktimer = 3.5f + agression;
                                        break;
                                    case 3: break;
                                }
                                RpcChangeState(State);
                            }
                            isAttack = false;
                        }
                    }
                }
                else
                {
                    if (target) rotator.LookAt(target.transform.position-new Vector3(0,target.transform.localScale.y,0));
                    attacktimer -= Time.deltaTime;
                    if (attacktimer <= 0)
                    {
                        //CmdAttack(State == state.bite ? 15 : 10);
                    }

                    visiontimer -= Time.deltaTime;
                    if (visiontimer <= 0) { target = null; }
                }
            }
            
            if(player.players!=null)for (i = 0; i < player.players.Count; ++i) 
            {
                for (ii = 0; ii < player.players[i].damageinfos.Count; ++ii) 
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
            player plr=null;
            if (GameObject.FindGameObjectWithTag("Player")) { plr = GameObject.FindGameObjectWithTag("Player").GetComponent<player>(); }

            if (plr)for (ii = 0; ii < plr.damageinfos.Count; ++ii)
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
            /**/
        }

        if (State==state.bite&&attacktimer < 0.6f) {
            if (attacktimer > 0.2f && attacktimer < 0.4f)
            {
                back.transform.Translate(0, Time.deltaTime * 3.6f, 0);
                jaw_up.transform.Rotate(Time.deltaTime * -125, 0, 0);
                jaw_down.transform.Rotate(Time.deltaTime * 125, 0, 0);
            }
            else
            {
                if (attacktimer < 0.2f)
                {
                    if (!isAttack) { isAttack = true; if (isServer) { CmdAttack(0); } }
                    back.transform.Translate(0, Time.deltaTime * -3.6f, 0);
                }
                else
                {
                    jaw_up.transform.Rotate(Time.deltaTime * 125, 0, 0);
                    jaw_down.transform.Rotate(Time.deltaTime * -125, 0, 0);
                }
            }
        }
        if (State==state.spray)
        {
            if (!isAttack) { isAttack = true; if (isServer) { CmdAttack(1); } }
            if (attacktimer > 0.2f && attacktimer < 0.4f)
            {
                jaw_up.transform.Rotate(Time.deltaTime * -125, 0, 0);
                jaw_down.transform.Rotate(Time.deltaTime * 125, 0, 0);
            }
            else
            {
                if (attacktimer < 0.2f)
                {
                    //jaw_up.transform.Rotate(Time.deltaTime * 100, 0, 0);
                    //jaw_down.transform.Rotate(Time.deltaTime * -100, 0, 0);
                }
                else if((3.5f + agression) - attacktimer < 0.2f)
                {
                    jaw_up.transform.Rotate(Time.deltaTime * 125, 0, 0);
                    jaw_down.transform.Rotate(Time.deltaTime * -125, 0, 0);
                }
            }
        }

        //middlepoint = new Vector3(7.768f, ((v_fr.y + v_cr.y + v_br.y) - (v_fl.y + v_cl.y + v_bl.y)) * -12f, -180);
        //back.transform.localRotation = Quaternion.Slerp(back.transform.localRotation, Quaternion.Euler(middlepoint), 3f * Time.deltaTime);

        SetLeg(leg_fr, v_fr, s_fr, 2f);
        SetLeg(leg_fl, v_fl, s_fl, 2f);
        SetLeg(leg_br, v_br, s_br, 2f);
        SetLeg(leg_bl, v_bl, s_bl, 2f);
        SetLeg(leg_cr, v_cr, s_cr, 2f);
        SetLeg(leg_cl, v_cl, s_cl, 2f);

        //if (State == state.move) 
        {
            SetLeg(hand_r, v_ar, s_ar, 2f);
            SetLeg(hand_l, v_al, s_al, 2f);
        }

        SetFoot(foot_fr, v_fr);
        SetFoot(foot_fl, v_fl);
        SetFoot(foot_br, v_br);
        SetFoot(foot_bl, v_bl);
        SetFoot(foot_cr, v_cr);
        SetFoot(foot_cl, v_cl);

        //if (State == state.move)
        {
            SetFoot(arm_r, v_ar);
            SetFoot(arm_l, v_al);
        }

        spd = Time.deltaTime * 13f * speed;//4
        v_fr = SlerpLeg(s_fr, t_fr, v_fr, out lock_fr, spd);
        v_fl = SlerpLeg(s_fl, t_fl, v_fl, out lock_fl, spd);
        v_bl = SlerpLeg(s_bl, t_bl, v_bl, out lock_bl, spd);
        v_br = SlerpLeg(s_br, t_br, v_br, out lock_br, spd);
        v_cl = SlerpLeg(s_cl, t_cl, v_cl, out lock_cl, spd);
        v_cr = SlerpLeg(s_cr, t_cr, v_cr, out lock_cr, spd);

        //if(State==state.move)
        {
            v_al = SlerpLeg(s_al, t_al, v_al, out lock_al, spd);
            v_ar = SlerpLeg(s_ar, t_ar, v_ar, out lock_ar, spd);
        }


        if (Physics.Raycast(point_bl.transform.position, -point_bl.transform.up, out hit, dropHeight))
        {
            if (Vector3.Distance(t_bl, hit.point) > stepTrashold && !(lock_bl || lock_br || lock_cl))
            {
                t_bl = hit.point;
                s_bl = hit.point;
                lock_bl = true;
            //    if (soundtimer % 5 == 0) PlayOneShot("event:/bugstep");
            }
        }
        if (Physics.Raycast(point_br.transform.position, -point_br.transform.up, out hit, dropHeight))
        {
            if (Vector3.Distance(t_br, hit.point) > stepTrashold && !(lock_bl || lock_br || lock_cr))
            {
                t_br = hit.point;
                s_br = hit.point;
                lock_br = true;
             //   if (soundtimer % 5 == 0) PlayOneShot("event:/bugstep");
            }
        }
        if (Physics.Raycast(point_fl.transform.position, -point_fl.transform.up, out hit, dropHeight))
        {
            if (Vector3.Distance(t_fl, hit.point) > stepTrashold && !(lock_fl || lock_fr || lock_cl))
            {
                t_fl = hit.point;
                s_fl = hit.point;
                lock_fl = true;
             //   if (soundtimer % 5 == 0) PlayOneShot("event:/bugstep");
            }
        }
        if (Physics.Raycast(point_fr.transform.position, -point_fr.transform.up, out hit, dropHeight))
        {
            if (Vector3.Distance(t_fr, hit.point) > stepTrashold && !(lock_fl || lock_fr || lock_cr))
            {
                t_fr = hit.point;
                s_fr = hit.point;
                lock_fr = true;
             //   if (soundtimer % 5 == 0) PlayOneShot("event:/bugstep");
            }
        }
        if (Physics.Raycast(point_cl.transform.position, -point_cl.transform.up, out hit, dropHeight))
        {
            if (Vector3.Distance(t_cl, hit.point) > stepTrashold && !(lock_cl || lock_cr || lock_bl || lock_fl))
            {
                t_cl = hit.point;
                s_cl = hit.point;
                lock_cl = true;
            }
        }
        if (Physics.Raycast(point_cr.transform.position, -point_cr.transform.up, out hit, dropHeight))
        {
            if (Vector3.Distance(t_cr, hit.point) > stepTrashold && !(lock_cl || lock_cr || lock_br || lock_fr))
            {
                t_cr = hit.point;
                s_cr = hit.point;
                lock_cr = true;
            }
        }
        if (State == state.move || (State == state.slash && attacktimer < 0.4f))
        {
            if (Physics.Raycast(point_al.transform.position, -point_al.transform.up, out hit, dropHeight))
            {
                if ((Vector3.Distance(t_al, hit.point) > stepTrashold)&& !(lock_al || lock_ar))
                {
                    t_al = hit.point;
                    s_al = hit.point;
                    lock_al = true;
                }
            }
            if (Physics.Raycast(point_ar.transform.position, -point_ar.transform.up, out hit, dropHeight))
            {
                if ((Vector3.Distance(t_ar, hit.point) > stepTrashold)&& !(lock_al || lock_ar))
                {
                    t_ar = hit.point;
                    s_ar = hit.point;
                    lock_ar = true;
                }
            }
        }
        else if (State == state.defense)
        {
            t_ar = defpoint_r.transform.position;
            s_ar = defpoint_r.transform.position;
            lock_ar = true;

            t_al = defpoint_l.transform.position;
            s_al = defpoint_l.transform.position;
            lock_al = true;
        }
        else if (State == state.slash) 
        {
             if (attacktimer == 1)
            {
                t_ar = defpoint_r.transform.position + new Vector3(0, 1f, 0);
                s_ar = defpoint_r.transform.position + new Vector3(0, 1f, 0);
                lock_ar = true;

                t_al = defpoint_l.transform.position + new Vector3(0, 1f, 0);
                s_al = defpoint_l.transform.position + new Vector3(0, 1f, 0);
                lock_al = true;
            }
           else 
            {
                if (!lock_ar && !lock_al)
                {
                    if(attacktimer < 0.45f||(attacktimer < 0.8f&& attacktimer > 0.7f))
                    {
                        t_ar = defpoint_r.transform.position;
                        s_ar = defpoint_r.transform.position;
                        lock_ar = true;

                        t_al = defpoint_l.transform.position;
                        s_al = defpoint_l.transform.position;
                        lock_al = true;
                    }
                    else if (attacktimer < 0.7f)
                    {
                        if (!isAttack) { isAttack = true;
                            if (isServer) { CmdAttack(10); }
                        }
                        if (target)
                        {
                            t_ar = target.transform.position;
                            s_ar = target.transform.position;
                        }
                        lock_ar = true;
                        if (target)
                        {
                            t_al = target.transform.position;
                            s_al = target.transform.position;
                        }
                        lock_al = true;
                    }
                }
            }
        }

        //hp
        if (localhp != hp) {
            localhp = hp;
            hpbar.gameObject.SetActive(true);
            //Destroy(Instantiate(hitsphere, transform.position, transform.rotation), 1f);
            hpbar.value = localhp * 1f / maxhp;
            lastdamage = 5f;
        }
        if (lastdamage > 0) { lastdamage -= Time.deltaTime; } else if(!isBoss){ hpbar.gameObject.SetActive(false); }
        if (hp <= 0) {
            enabled = false;
            Destroy(gameObject,8f);
            DropAll();
        }
    }
    private void PlayOneShot(string eventname)
    {
        FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventname);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        instance.start();
        instance.release();
    }
    private void OnDrawGizmos()
    {
        if (Application.isPlaying) {
            Gizmos.color = new Color(0.565f,0.018f,0.433f);
            for(i = 0; i < path.Count; ++i)
            {
                Gizmos.color = new Color(0.67f, Mathf.Sin(1f / path.Count * i), Mathf.Cos(1f / path.Count * i));
                Gizmos.DrawCube(path[i], new Vector3(0.2f, 0.2f, 0.2f));
            }
        }
    }
    private void OnDestroy()
    {
        hpbar.gameObject.SetActive(false);
        NetworkServer.Destroy(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (isServer)
        {
            if (collision.gameObject.CompareTag("Destroyer")) {
                Dmg(5,collision.other.gameObject);
                CmdDmg(5);
                if (hp - 5<0) { ++player.thisplayer.kills; }
            }
        }
        if (collision.gameObject.CompareTag("Destroyer"))
        {
            Destroy(Instantiate(hitsphere, collision.contacts[0].point, transform.rotation), 1f);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (isServer)
        {
            if (collision.gameObject.name=="Fire(Clone)")
            {
                Dmg(1, collision.gameObject);
                CmdDmg(1);
                if (hp - 1 < 0) { ++player.thisplayer.kills; }
            }
        }
        if (collision.gameObject.name == "Fire(Clone)")
        {
            //Destroy(Instantiate(hitsphere, collision.transform.position, transform.rotation), 1f);
        }
    }

    public enum state {
        none,
        move,
        defense,
        bite,
        slash,
        spray,
        split
    }
    
    public struct damageinfo {
        public NetworkInstanceId netid;
        public int unid;
        public float lifetime;
        public int dmgamout;
        public damageinfo(NetworkInstanceId netid,int unid, float lifetime, int dmgamout)
        {
            this.netid = netid;
            this.unid = unid;
            this.lifetime = lifetime;
            this.dmgamout = dmgamout;
        }
    }
    public class SyncListDamageInfo:SyncListStruct<damageinfo> { }
    public class SyncListVector3:SyncListStruct<Vector3> { }
    /**/
}
