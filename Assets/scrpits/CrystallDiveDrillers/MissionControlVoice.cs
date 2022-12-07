using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionControlVoice : MonoBehaviour
{
    private FMOD.Studio.EventInstance instance;
    public static MissionControlVoice only;
    FMOD.Studio.PLAYBACK_STATE state;
    public GameObject icon;
    private void Start()
    {

        instance = FMODUnity.RuntimeManager.CreateInstance("event:/MC");
        instance.setParameterByName("r",0);
        only = this;
    }
    public void Update()
    {
        instance.getPlaybackState(out state);
        if (state != FMOD.Studio.PLAYBACK_STATE.STOPPED)
        {
            icon.SetActive(true);
        }
        else 
        {
            icon.SetActive(false);
        }
    }
    public void PlayReplica(int r) 
    {
        
        instance.getPlaybackState(out state);
        if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
        {
            instance.setParameterByName("r", r);
            instance.start();
        }
        else 
        {
            print(state);
        }
    }
}
