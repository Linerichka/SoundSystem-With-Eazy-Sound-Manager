using UnityEngine;

namespace Lineri.SoundSystem
{
    public class ActionSoundPocketManager : MonoBehaviour
    {
        public delegate void HandlerMethod();
        public HandlerMethod Play;
        public HandlerMethod ResetQueue;
        public HandlerMethod Stop;
        public HandlerMethod Pause;
        public HandlerMethod UnPause;
        public HandlerMethod ResetTime;

        private void OnEnable()
        {
            //use such a construction to bind a method of an instance of a class to an event, inside your class
            //action += actionSoundPocketManager.ActionPlayHandler;
            SetVariables();
        }

        private void OnDisable()
        {
            UnSetVariables();
        }

        #region Handlers
        public void ActionPlayHandler()
        {
            SortThroughSoundPocketAndChek(_methods.Play);
        }

        //  use this if you need to reset the queue of clips in SoundPockets  
        public void ActionResetQueueHandler()
        {
            SortThroughSoundPocketAndChek(_methods.ResetQueue);
        }

        // use this to stop playing the current clips in your pocket
        public void ActionStopClipsHandler()
        {
            SortThroughSoundPocketAndChek(_methods.Stop);
        }

        // use this to pause the playback of clips in your pocket, with the subsequent possibility to resume
        public void ActionPauseClipsHandler()
        {
            SortThroughSoundPocketAndChek(_methods.Pause);
        }

        // use this to resume playback of the installed clips
        public void ActionUnPauseClipsHandler()
        {
            SortThroughSoundPocketAndChek(_methods.UnPause);
        }

        // use this to reset the time in order to force Play() to start ahead of time.
        public void ActionResetTimeClipsPlayed()
        {
            SortThroughSoundPocketAndChek(_methods.ResetTimePlayed);
        }

        public void Initialization()
        {
            SetVariables();
        }
        #endregion

        private void SetVariables()
        {
            Play = ActionPlayHandler;
            ResetQueue = ActionResetQueueHandler;
            Stop = ActionStopClipsHandler;
            Pause = ActionPauseClipsHandler;
            UnPause = ActionUnPauseClipsHandler;
            ResetTime = ActionResetTimeClipsPlayed;
        }

        private void UnSetVariables()
        {
            Play = null;
            ResetQueue = null;
            Stop = null;
            Pause = null;
            UnPause = null;
            ResetTime = null;
        }

        #region logic
        private enum _methods
        {
            Play,
            ResetQueue,
            Stop,
            Pause,
            UnPause,
            ResetTimePlayed
        };

        private void SortThroughSoundPocketAndChek(_methods method)
        {
            foreach (SoundPocket soundPocket in gameObject.GetComponents<SoundPocket>())
            {
                if (soundPocket == null) continue;

                CallMethodsInSoundPocket(soundPocket, method);
            }
        }

        private void CallMethodsInSoundPocket(SoundPocket soundPocket, _methods method)
        {
            switch (method)
            {
                case _methods.Play:
                    soundPocket.Play();
                    break;
                case _methods.ResetQueue:
                    soundPocket.ResetClipQueue();
                    break;
                case _methods.Stop:
                    soundPocket.StopClipsPlayning();
                    break;
                case _methods.Pause:
                    soundPocket.PauseClipsPlayning();
                    break;
                case _methods.UnPause:
                    soundPocket.UnPauseClipsPlayning();
                    break;
                case _methods.ResetTimePlayed:
                    soundPocket.ResetTimeClipsPlayed();
                    break;
            }
        }
        #endregion
    }
}
