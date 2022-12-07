using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class missionActivator : MonoBehaviour
{
    public int id=-1;
    public void Activate() {
        MissionMenuController controller = GameObject.Find("MissionMenuController").GetComponent<MissionMenuController>();
        controller.SelectMission(id);
    }
}
