using System.Collections.Generic;
using UnityEngine;
using Hellmade.Sound;

public class ActionSoundPocketManager : MonoBehaviour
{
    [SerializeField] private List <GameObject> _soundPockets = new List<GameObject>();

    private void OnEnable()
    {
        //using such a record to subscribe to events, this allows you to ignore the arguments that are passed to Action.
        //_primer += _ => ActionSoundPocketManager.ActionHandler();
    }
    private void OnDisable()
    {
        //_primer -= _ => ActionSoundPocketManager.ActionHandler();
    }

    #region Handlers
    public void ActionPlayHandler()
    {
        SoundPocketInvokePlay();
    }

    //  use this if you need to reset the queue of clips in SoundPockets  
    public void ActionResetQueueHandler()
    {
        SoundPocketResetQueue();
    }

    // use this to stop playing the current clips in your pocket
    public void ActionStopClipsHandler()
    {
        SoundPocketStop();
    }

    // use this to pause the playback of clips in your pocket, with the subsequent possibility to resume
    public void ActionPauseClipsHandler()
    {
        SoundPocketPause();
    }

    // use this to resume playback of the installed clips
    public void ActionUnPauseClipsHandler() 
    {
        SoundPocketUnPause();
    }
    #endregion

    #region wtf cor
    private void SoundPocketInvokePlay()
    {
        foreach (GameObject pocket in _soundPockets)
        {
            if (pocket == null) return;

            SoundPocket soundPocket = pocket.GetComponent<SoundPocket>();
            soundPocket.Play();
        }
    }

    private void SoundPocketResetQueue()
    {
        foreach (GameObject pocket in _soundPockets)
        {
            if (pocket == null) return;

            SoundPocket soundPocket = pocket.GetComponent<SoundPocket>();
            soundPocket.ResetClipQueue();
        }
    }

    private void SoundPocketStop()
    {
        foreach (GameObject pocket in _soundPockets)
        {
            if (pocket == null) return;

            SoundPocket soundPocket = pocket.GetComponent<SoundPocket>();
            soundPocket.StopClipsPlayning();
        }
    }

    private void SoundPocketPause()
    {
        foreach (GameObject pocket in _soundPockets)
        {
            if (pocket == null) return;

            SoundPocket soundPocket = pocket.GetComponent<SoundPocket>();
            soundPocket.PauseClipsPlayning();
        }
    }

    private void SoundPocketUnPause()
    {
        foreach (GameObject pocket in _soundPockets)
        {
            if (pocket == null) return;

            SoundPocket soundPocket = pocket.GetComponent<SoundPocket>();
            soundPocket.UnPauseClipsPlayning();
        }
    }
    #endregion
}
