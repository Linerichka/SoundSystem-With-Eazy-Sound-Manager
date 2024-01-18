using UnityEngine;

namespace Lineri.SoundSystem
{
    public abstract class SoundPocketManager : MonoBehaviour
    {
        [SerializeField] private GameObject _soundPocket;
        protected GameObject SoundPocket
        {
            get => _soundPocket;
            set
            {
                if (value != null) _soundPocketExists = true;
                else _soundPocketExists = false;

                _soundPocketPrefab = value;
            }
        }

        [SerializeField] private GameObject _soundPocketPrefab;
        protected GameObject SoundPocketPrefab 
        { 
           get => _soundPocketPrefab;
           set => _soundPocketPrefab = value;
        }

        protected bool _soundPocketMethodsCanExecuted = true;
        //object SoundPocket has been created and is ready for use
        protected bool _soundPocketExists = false;

        #region Handlers
        /// <summary>
        /// Use to start playback
        /// </summary>
        protected virtual void PlayHandler()
        {
            SortThroughSoundPocketAndChek(_methods.Play);
        }

        /// <summary>
        /// Use this if you need to reset the queue of clips in SoundPockets.
        /// </summary>
        protected virtual void ResetQueueHandler()
        {
            SortThroughSoundPocketAndChek(_methods.ResetQueue);
        }

        /// <summary>
        /// Use this to stop playing the current clips in your pocket.
        /// </summary>
        protected virtual void StopHandler()
        {
            SortThroughSoundPocketAndChek(_methods.Stop);
        }

        /// <summary>
        /// Use this to pause the playback of clips in your pocket, with the subsequent possibility to resume.
        /// </summary>
        protected virtual void PauseHandler()
        {
            SortThroughSoundPocketAndChek(_methods.Pause);
        }

        /// <summary>
        /// Use this to resume playback of the installed clips.
        /// </summary>
        protected virtual void UnPauseHandler()
        {
            SortThroughSoundPocketAndChek(_methods.UnPause);
        }

        /// <summary>
        /// Use this to reset the time in order to force Play() to start ahead of time.
        /// </summary>
        protected virtual void ResetTimePlayedHandler()
        {
            SortThroughSoundPocketAndChek(_methods.ResetTimePlayed);
        }

        /// <summary> 
        /// Use this to disable the execution of Update functions in SoundPocket,
        /// and also ignore calls to NameMethodHandler methods for SoundPocket,
        /// also sets PlayClips = false for all SoundPocket
        /// This can be used to save CPU time.
        /// <param name="disableSoundPocket">
        /// Set the argument to false to only ignore function calls, without disabling SoundPocket.
        /// </param>
        /// </summary>
        protected virtual void OffSoundPocketHandler(in bool disableSoundPocket = true)
        {
            _soundPocketMethodsCanExecuted = false;

            if (!disableSoundPocket) return;

            SortThroughSoundPocketAndChek(_methods.PlayClipsSetOff, true);

            if (_soundPocketExists) _soundPocket.SetActive(false);          
        }


        protected virtual void OnSoundPocketHandler()
        {
            _soundPocketMethodsCanExecuted = true;
            SortThroughSoundPocketAndChek(_methods.PlayClipsSetOn, true);

            if (_soundPocketExists) _soundPocket.SetActive(true);        
        }

        /// <summary>
        /// Creates a prefab and changes the bool variable.
        /// <c>_soundPocketExists = true;</c>.
        /// </summary>
        /// <returns>Instantiate().gameObject</returns>
        protected virtual GameObject CreateSoundPocketHandler()
        {
            _soundPocketExists = true;
            return _soundPocket = Instantiate(_soundPocketPrefab, transform);
        }

        /// <summary>
        /// Destroy a prefab and changes the bool variable.
        /// <c>_soundPocketExists = false;</c>
        /// </summary>
        protected virtual void DestroySoundPocketHandler()
        {
            _soundPocketExists = false;
            Destroy(_soundPocket);
        }
        #endregion

        protected virtual void Awake()
        {
            SetVariables();
        }

        #region Logic
        protected enum _methods
        {
            Play,
            ResetQueue,
            Stop,
            Pause,
            UnPause,
            ResetTimePlayed,
            PlayClipsSetOn,
            PlayClipsSetOff
        };

        /// <summary>
        /// If the optional argument is true, then the action will be performed even if the methods cannot be called.
        /// </summary>
        protected virtual void SortThroughSoundPocketAndChek(in _methods method, in bool ignoreSoundPocketMethodsCanExecuted = false)
        {
            if (!_soundPocketMethodsCanExecuted && !ignoreSoundPocketMethodsCanExecuted) return;

            foreach (SoundPocket soundPocket in GetSoundPockets())
            {
                if (soundPocket == null) continue;

                CallMethodsInSoundPocket(soundPocket, method);
            }
        }

        /// <summary>
        /// Gets and returns an array of SoundPocket from the created prefab for calling methods
        /// </summary>
        protected virtual SoundPocket[] GetSoundPockets()
        {
            return _soundPocket.GetComponents<SoundPocket>();
        }

        protected virtual void CallMethodsInSoundPocket(SoundPocket soundPocket, _methods method)
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
                case _methods.PlayClipsSetOn:
                    soundPocket.PlayClips = true;
                    break;
                case _methods.PlayClipsSetOff:
                    soundPocket.PlayClips = false;
                    break;
            }
        }

        protected virtual void SetVariables()
        {
            if (_soundPocket != null)
            {
                _soundPocketExists = true;
            }
        }
        #endregion

        protected virtual void OnValidate()
        {
            SoundPocket = _soundPocket;
            SoundPocketPrefab = _soundPocketPrefab;
        }
    }
}
