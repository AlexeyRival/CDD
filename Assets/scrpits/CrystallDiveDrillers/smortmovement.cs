using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class smortmovement : MonoBehaviour
{
    public GameObject target;
    public int maxrays = 6;
    private RaycastHit hit;

    private void TryFind()
    {
        if(Physics.Raycast(target.transform.position, transform.position-target.transform.position, out hit, 50))
        Debug.DrawLine(target.transform.position, hit.point, Color.red);
            //if (Physics.Raycast((hit.transform?:hit.point+target.transform.position)*0.5f, transform.position - target.transform.position, out hit, 50))
            Debug.DrawLine(target.transform.position, hit.point, Color.red);


        if (Physics.Raycast(target.transform.position, Quaternion.AngleAxis(90, Vector3.up) * (transform.position - target.transform.position), out hit, 50))
        Debug.DrawLine(target.transform.position, hit.point, Color.red);
        
        if (Physics.Raycast(target.transform.position, Quaternion.AngleAxis(-90, Vector3.up) * (transform.position - target.transform.position), out hit, 50))
        Debug.DrawLine(target.transform.position, hit.point, Color.red);
        
        if (Physics.Raycast(target.transform.position, target.transform.position- transform.position, out hit, 50))
        Debug.DrawLine(target.transform.position, hit.point, Color.red);
    }
    private void Update()
    {
        TryFind();
    }
    /*
    private List<data> datas;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private bool TryRay(Vector3 from, Vector3 angle,Transform target) 
    {
        if (Physics.Raycast(from, angle, out hit))//, Vector3.Distance(transform.position, target.transform.position))) 
        {
            if (hit.transform == target)
            {
                Debug.DrawRay(from, angle, Color.green);
                return true;
            }
            else
            {
                Debug.DrawRay(from, hit.point - from, Color.red);
                return false;
            }
        }
        return false;
    }
    private bool TryRay(Vector3 from, Vector3 angle,Vector3 target) 
    {
        if (Physics.Raycast(from, angle, out hit))//, Vector3.Distance(transform.position, target.transform.position))) 
        {
            if (hit.point == target)
            {
                Debug.DrawRay(from, angle, Color.green);
                return true;
            }
            else
            {
                Debug.DrawRay(from, hit.point - from, Color.red);
                return false;
            }
        }
        return false;
    }
    private const float step = 20;
    private bool TryRay(Vector3 from, Vector3 angle,int iterr,Transform target,Transform origin) 
    {
        int iter = iterr + 1;
        if (iter > maxrays) { return false; }
        bool outbool = false;
        if (Physics.Raycast(from, angle, out hit)) 
        {
            if (hit.transform == target)
            {
                Debug.DrawRay(from, angle, Color.green);
                outbool = true;
            }
            else
            {
                Vector3 htpt = hit.point;
                float hd = hit.distance;
                float dd = Vector3.Distance(target.position, origin.position);
                outbool =

                    hd > 1 && hd < dd && hd < 50 && (
                    TryRay(from, target.position - from, iter<4?4:iter, target,origin)||
                    //(target!=transform?TryRay(target.position, from-target.position, iter+1, origin,target):false)||
                    TryRay(from + (htpt - from) * 0.25f, angle + new Vector3(0, step, 0), iter, target,origin) ||
                    TryRay(from + (htpt - from) * 0.25f, angle + new Vector3(0, -step, 0), iter, target,origin)||
                    TryRay(from + (htpt - from) * 0.25f, Quaternion.AngleAxis(step, Vector3.up)*angle, iter, target,origin)||
                    TryRay(from + (htpt - from) * 0.25f, Quaternion.AngleAxis(-step, Vector3.up)*angle, iter, target,origin)
                    )
                    ;
                if (!outbool)
                {
                    Debug.DrawRay(from, htpt - from, Color.red);
                }
                else 
                {
                    Debug.DrawRay(from, (htpt - from)*0.5f, Color.green);
                }
            }
        }
        return outbool;
    }
    private bool TryNewRay(Vector3 from, Vector3 angle,int iterr,Transform target,Transform origin) 
    {
        int iter = iterr + 1;
        if (iter > maxrays) { return false; }
        bool outbool = false;
        if (Physics.Raycast(from, angle, out hit)) 
        {
            if (hit.transform == target)
            {
                Debug.DrawRay(from, angle, Color.green);
                outbool = true;
            }
            else
            {
                Vector3 htpt = hit.point;
                datas.Add(new data(from, target.position - from, iter < 4 ? 4 : iter, target, origin));
                datas.Add(new data(from + (htpt - from) * 0.25f, angle + new Vector3(0, step, 0), iter, target, origin));
                datas.Add(new data(from + (htpt - from) * 0.25f, angle + new Vector3(0, -step, 0), iter, target, origin));
                datas.Add(new data(from + (htpt - from) * 0.25f, Quaternion.AngleAxis(step, Vector3.up) * angle, iter, target, origin));
                datas.Add(new data(from + (htpt - from) * 0.25f, Quaternion.AngleAxis(-step, Vector3.up) * angle, iter, target, origin));
            }
            Debug.DrawRay(from, hit.point-from, new Color(1f,1f,1f,0.25f));
        }
        return outbool;
    }
    private bool TryNewRay(data data) 
    {
        return TryNewRay(data.from,data.angle,data.iterr,data.target,data.origin);
    }
    // Update is called once per frame
    void Update()
    {
        datas = new List<data>();
        // Debug.DrawRay(transform.position, target.transform.position-transform.position, Color.cyan);
        //if (!TryRay(transform.position, target.transform.position - transform.position, -1, target.transform,transform))
        //TryRay(transform.position, target.transform.position - transform.position, 1 - (int)(Vector3.Distance(transform.position, target.transform.position) / 10), target.transform, transform);
        if(
            !TryNewRay(transform.position, target.transform.position - transform.position, 0, target.transform, transform)&&
            !TryNewRay(target.transform.position, transform.position - target.transform.position, 0, transform, target.transform)
            )
        for (int i = 0; i < maxrays; ++i) 
        {
            for (int ii = 0; ii < datas.Count; ++ii) 
            if(datas[ii].iterr==i){
                        TryNewRay(datas[ii]);
                        datas.Remove(datas[ii]);
                    }
        }
       // if(!TryRay(transform.position,target.transform.position-transform.position,target.transform)){
        //    Vector3 htp = hit.point;
        //    boboom(transform.position+(hit.point-transform.position)*0.5f, target.transform.position - transform.position, target.transform);
            //TryRay(target.transform.position, transform.position - target.transform.position, 3 - (int)(Vector3.Distance(transform.position, target.transform.position) / 10), transform);
       // }
        
            //Debug.DrawRay(transform.position + (htpt - transform.position) * 0.5f, new Vector3(0.5f,0,0.5f), Color.cyan);
        
        //if (Physics.Raycast(transform.position, target.transform.position - transform.position, out hit, Vector3.Distance(transform.position, target.transform.position))) 
        
        /*
        transform.LookAt(target.transform.position);
        transform.Translate(0,0,Time.deltaTime);
    }*/
    private class data 
    {
        public Vector3 from;
        public Vector3 angle;
        public int iterr;
        public Transform target;
        public Transform origin;

        public data(Vector3 from, Vector3 angle, int iterr, Transform target, Transform origin)
        {
            this.from = from;
            this.angle = angle;
            this.iterr = iterr;
            this.target = target;
            this.origin = origin;
        }
    }
    
}
