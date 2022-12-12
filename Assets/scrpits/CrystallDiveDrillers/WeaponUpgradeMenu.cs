using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponUpgradeMenu : MonoBehaviour
{

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }        
    }
}
