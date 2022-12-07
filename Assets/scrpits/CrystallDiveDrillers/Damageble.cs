using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Damageble : MonoBehaviour
{
    public Slider slider;
    public float maxhp, hp;
    private static Camera cam;
    void Update()
    {

        if (!cam) {
            cam = Camera.main;
            return;
        }
        if (slider.value !=hp/maxhp&& slider.value!=0f) {
            slider.value = Mathf.MoveTowards(slider.value, hp / maxhp, 1f*Time.deltaTime);
        }
        slider.transform.rotation=Quaternion.LookRotation(slider.transform.position- cam.transform.position);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Destroyer")) { hp -= 15;}
    }
}
