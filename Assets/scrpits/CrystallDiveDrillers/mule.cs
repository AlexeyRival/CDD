using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class mule : NetworkBehaviour
{
    public bool isStartWalking;
    public float speed = 2f;
    public List<Vector3> path;
    private int currentpoint;
    private RaycastHit hit;
    public GameObject box;
    public GameObject leg_fr, leg_fl, leg_br, leg_bl;//ляжки
    public GameObject foot_fr, foot_fl, foot_br, foot_bl;//голени
    public GameObject point_fr, point_fl, point_br, point_bl;//точки сброса
    public Vector3 v_bl, v_br, v_fl, v_fr;
    public Vector3 t_bl, t_br, t_fl, t_fr;
    public Vector3 s_bl, s_br, s_fl, s_fr;
    private bool lock_bl, lock_br, lock_fl, lock_fr;
    private float bfr;
    private float spd;
    // Update is called once per frame
    public void SetPath(List<Vector3> path) {
        this.path = path;
        currentpoint = 0;
        if(path.Count>0)isStartWalking = true;
    }
    private void SetLeg(GameObject leg, Vector3 vec, Vector3 startpoint,float height) {

        bfr = Mathf.Sin(Vector3.Distance(startpoint, vec) * Mathf.PI) * 0.5f + height;
        leg.transform.LookAt(new Vector3(vec.x, vec.y + bfr, vec.z));
        leg.transform.Rotate(-90, 90, 90);
    }
    private void SetFoot(GameObject foot, Vector3 vec) {
        foot.transform.LookAt(vec);
        foot.transform.Rotate(-90, 90, 90);
    }
    private void SetFoot(GameObject foot, Vector3 vec, Vector3 rotator) {
        foot.transform.LookAt(vec);
        foot.transform.Rotate(rotator);
    }
    private Vector3 SlerpLeg(Vector3 startpoint, Vector3 targetpoint,Vector3 vec, out bool locker,float speed) {    
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
    void Update()
    {
        if (isServer) {
            if (isStartWalking)
            {
                if (currentpoint == path.Count) { isStartWalking = false;return; }
                transform.LookAt(path[currentpoint]);
                transform.Translate(0, 0, Time.deltaTime * speed);
                Debug.DrawRay(transform.position, -transform.up, Color.green);
                if (Physics.Raycast(transform.position, -transform.up, out hit, 4f))
                {
                    if (hit.distance > 0.7f) { transform.Translate(0, -speed * Time.deltaTime, 0); }
                    if (hit.distance < 0.5f) { transform.Translate(0, speed * Time.deltaTime, 0); }
                }
                else if (Physics.Raycast(transform.position- transform.up, transform.up, out hit, 2f)) 
                {
                  //  transform.Rotate(-180,0,0);
                }
                transform.Rotate(-90, 0, 0);
                transform.Rotate(0, 0, 45);
                if (Vector3.Distance(transform.position, path[currentpoint]) < 1f) {
                    ++currentpoint;
                }
            }
        }
     //   box.transform.localPosition = new Vector3(0, 0.685f, 0);
        for (int i = 0; i < cornertable.Length; ++i)
        {
            if (Physics.Raycast(transform.position, cornertable[i], out hit, 1f))
            {
       //         box.transform.position=(box.transform.position-cornertable[i]*hit.distance*0.5f);
            }
        }

        SetLeg(leg_fr,v_fr,s_fr,0.45f);
        SetLeg(leg_fl,v_fl,s_fl,0.45f);
        SetLeg(leg_br,v_br,s_br,0.45f);
        SetLeg(leg_bl,v_bl,s_bl,0.45f);

        SetFoot(foot_fr,v_fr);
        SetFoot(foot_fl, v_fl);
        SetFoot(foot_br,v_br);
        SetFoot(foot_bl,v_bl);

        spd = Time.deltaTime * 13f*speed;//4
        v_fr = SlerpLeg(s_fr,t_fr,v_fr,out lock_fr,spd);
        v_fl = SlerpLeg(s_fl,t_fl,v_fl,out lock_fl,spd);
        v_bl = SlerpLeg(s_bl,t_bl,v_bl,out lock_bl,spd);
        v_br = SlerpLeg(s_br,t_br,v_br,out lock_br,spd);

        if (Physics.Raycast(point_bl.transform.position, -point_bl.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_bl, hit.point) > 1f&&!(lock_bl||lock_br||lock_fl)) { 
                t_bl= hit.point;
                s_bl= hit.point;
                lock_bl = true;
            }
        }
        if (Physics.Raycast(point_br.transform.position, -point_br.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_br, hit.point) > 1f && !(lock_bl || lock_br || lock_fr)) { 
                t_br= hit.point;
                s_br= hit.point;
                lock_br = true;
            }
        }
        if (Physics.Raycast(point_fl.transform.position, -point_fl.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_fl, hit.point) > 1f && !(lock_fl || lock_fr || lock_bl)) { 
                t_fl= hit.point;
                s_fl= hit.point;
                lock_fl = true;
            }
        }
        if (Physics.Raycast(point_fr.transform.position, -point_fr.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_fr, hit.point) > 1f && !(lock_fl || lock_fr || lock_br)) { 
                t_fr= hit.point;
                s_fr= hit.point;
                lock_fr = true;
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.67f, 0, 0.54f, 0.67f);
        if (isStartWalking) {
            for (int i = 0; i < path.Count; ++i) {
                Gizmos.color = new Color(0.67f,Mathf.Sin(1f/path.Count*i),Mathf.Cos(1f/path.Count*i));
                Gizmos.DrawCube(path[i], new Vector3(0.4f, 0.4f, 0.4f));
            }
            
        }
    }
    private readonly Vector3[] cornertable = {
        new Vector3(1,0,0),
        new Vector3(-1,0,0),
        new Vector3(0,1,0),
        new Vector3(0,-1,0),
        new Vector3(0,0,1),
        new Vector3(0,0,-1),
    };
}
