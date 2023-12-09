using System.Collections.Generic;
using UnityEngine;
using AudioType = Lineri.SoundSystem.Audio.AudioType;

namespace Lineri.SoundSystem
{
    public class SoundPocket : MonoBehaviour
    {
        [Header("Clips setting")]
        //here Sound - all sounds \ audio clips
        [SerializeField] private float _soundVolume = 1f;
        public float SoundVolume
        {
            get => _soundVolume;
            set
            {
                _soundVolume = Mathf.Clamp(value, 0f, 1);
                UpdateParametersAllPlayingClips();
            }
        }

        [SerializeField] private float _pitch = 1f;
        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = Mathf.Clamp(value, 0f, 3);
                UpdateParametersAllPlayingClips();
            }
        }

        [SerializeField] private float _randomPitch = 0f;
        public float RandomPitch
        {
            get => _randomPitch;
            set => _randomPitch = Mathf.Clamp(value, -3f, 3f);
        }

        //Use the plugin documentation for these 3 variables
        public float FadeInSecond = 2f;
        public float FadeOutSecond = 2f;
        public float CurrentMusicFadeOutSeconds = -1f;
        //
        // Use the plugin documentation for 3d audio AND set null to not use 3D sound
        public Transform Transform3dAudio = null;

        [Header("SoundPocket settings")]
        public bool LoopClips = false;
        // If true, plays clips in the lists in random order. ONLY FOR CLIPS ONE AFTER THE OTHER
        public bool RandomPlayClip = false;
        /// <summary>
        /// Keep sound on scene change
        /// </summary>
        /// continues the sound that is played at the moment of the scene change,
        /// audio queue won't play if the current class is not in DontDestroyOnLoad
        public bool PersistSoundOnSceneLoad = false;
        //when called, Play plays one clip instead of the entire chain. ignores LoopClips
        public bool PlayOneClip = false;
        // Play all clips at the start
        public bool PlaySoundOnAwake = false;
        // Plays all the assigned clips at the same time. This may not work with EazySoundManager.IgnoreDuplicateMusic/Sound/UiSound == true
        public bool PlayAllClipsTogether = false;
        // After the end of the current clip, the new ones will no longer be played
        public bool PlayClips = true;

        [Header("Clips")]
        public List<AudioClip> MusicClips = new List<AudioClip>();
        public List<AudioClip> SoundClips = new List<AudioClip>();
        public List<AudioClip> SoundUiClips = new List<AudioClip>();

        #region private variables
        //if Start() was called earlier - true, if not - false
        private bool _startInvoked = false; 
        //true if the clips were called to play together
        private bool _clipsTogetherPlayCalled; 
        private int _numberClipPlayedInList = 0;
        protected bool _playWasCalled = false;
        #region to optimize GetAudioFromClips()
        private List<Audio> _audioFromClips;
        private bool _listsClipsChanged = false;
        #endregion
        //use it to force new clips to start, even if the clips are already being played
        private bool _ignorePlayingCurrentClips = false;
        #endregion

        protected virtual void Start()
        {
            SetVariables();
            PlayOnAwakeningIfConditionIsMet();
        }

        private void Update()
        {
            PlayAudioFromList(false);           
        }

        #region Public void
        public virtual void Play()
        {
            if (!CheñkInstalledClipsAndDisableIfNotInstalled()) return;

            _playWasCalled = true;
            PlayClips = true;

            PlayAudioFromList(true);
        }

        public void ResetClipQueue()
        {
            _numberClipPlayedInList = 0;
            _clipsTogetherPlayCalled = false;
        }

        public void StopClipsPlayning()
        {
            foreach (Audio audio in GetAudioFromClips())
            {
                audio.Stop();
            }
            
            PlayClips = false;

            ResetClipQueue();
        }

        public void PauseClipsPlayning()
        {
            foreach (Audio audio in GetAudioFromClips())
            {
                audio.Pause();
            }
        }

        public void UnPauseClipsPlayning()
        {
            foreach (Audio audio in GetAudioFromClips())
            {
                audio.UnPause();
            }
        }

        public void ResetTimeClipsPlayed()
        {
            _ignorePlayingCurrentClips = true;
        }
        #endregion

        /// <summary>
        /// Update the parameters of the clips that are currently playing (pitch, volume ...)
        /// </summary>
        private void UpdateParametersAllPlayingClips()
        {
            if (!(Application.isPlaying && _startInvoked)) return;
            if (EazySoundManager.Gameobject == null) return;

            foreach (Audio audio in GetAudioFromClips())
            {
                audio.SetVolume(SoundVolume);
                audio.Pitch = Pitch;
            }
        }
       
        #region PlayClip
        protected virtual void PlayAudioFromList(bool callPlay)
        {
            if (!PlayClips || !_playWasCalled) return;
            if (!Application.isFocused) return;

            if (PlayAllClipsTogether)
            {
                PlayAllClipsInListTogether(callPlay);
            }
            else
            {
                PlayClipsOneAfterTheOther(callPlay);
            }
        }

        #region Play Together
        private void PlayAllClipsInListTogether(bool callPlay)
        {
            if (!((_clipsTogetherPlayCalled && LoopClips) || callPlay)) return;
            if (!PlaybackOfClipsIsComplete()) return;           

            PlayAllClipsInList(MusicClips, AudioType.Music);
            PlayAllClipsInList(SoundClips, AudioType.Sound);
            PlayAllClipsInList(SoundUiClips, AudioType.UISound);

            _clipsTogetherPlayCalled = true;
        }

        private void PlayAllClipsInList(List<AudioClip> listClips, AudioType AudioType)
        {
            foreach (AudioClip clip in listClips)
            {
                PlayClipInListTogether(clip, AudioType);
            }
        }

        private int PlayClipInListTogether(AudioClip clip, AudioType AudioType)
        {
            int id = PlayAudio(clip, AudioType);
            float clipPitch = Pitch + UnityEngine.Random.Range(-RandomPitch, RandomPitch);
            EazySoundManager.GetAudio(id, AudioType).Pitch = clipPitch;

            return id;
        }
        #endregion

        #region Play One After The Other
        protected void PlayClipsOneAfterTheOther(bool callPlay)
        {
            if (!(MusicClips.Count > _numberClipPlayedInList || 
                SoundClips.Count > _numberClipPlayedInList || 
                SoundUiClips.Count > _numberClipPlayedInList))
            {
                if (LoopClips || callPlay)
                {
                    ResetClipQueue();
                }
            }

            if (!PlaybackOfClipsIsComplete() || (!callPlay && PlayOneClip))
            {
                return;
            }
           
            if (MusicClips.Count > _numberClipPlayedInList)
            {
                PlayClipInList(MusicClips, AudioType.Music);
            }

            if (SoundClips.Count > _numberClipPlayedInList)
            {                
                PlayClipInList(SoundClips, AudioType.Sound);
            }

            if (SoundUiClips.Count > _numberClipPlayedInList)
            {
                PlayClipInList(SoundUiClips, AudioType.UISound);
            }

            _numberClipPlayedInList++;
        }

        private int PlayClipInList(List<AudioClip> clips, AudioType AudioType)
        {
            int clipNumber;

            if (RandomPlayClip)
            {
                clipNumber = UnityEngine.Random.Range(0, clips.Count);
            }
            else
            {
                clipNumber = _numberClipPlayedInList;
            }

             int id  = PlayAudio(clips[clipNumber], AudioType);
             float clipPitch = Pitch + UnityEngine.Random.Range(-RandomPitch, RandomPitch);
             EazySoundManager.GetAudio(id, AudioType).Pitch = clipPitch;

            return id;
        }
        #endregion

        private int PlayAudio(AudioClip clip, in AudioType AudioType)
        {
            switch (AudioType)
            {
                case AudioType.Music:
                    return PlayAudioMusic(clip);
                case AudioType.Sound:
                    return PlayAudioSound(clip);
                case AudioType.UISound:
                    return PlayAudioUISound(clip);
                default:
                    unchecked
                    {
                        return (int)Mathf.NegativeInfinity;
                    }                                               
            }
        }

        protected virtual int PlayAudioMusic(AudioClip clip)
        {
            int id = EazySoundManager.PlayMusic(clip, SoundVolume, false, PersistSoundOnSceneLoad, 
                FadeInSecond, FadeOutSecond, CurrentMusicFadeOutSeconds, Transform3dAudio);
            return id;
        }

        protected virtual int PlayAudioSound(AudioClip clip)
        {
            int id = EazySoundManager.PlaySound(clip, SoundVolume, false, Transform3dAudio);
            return id;
        }

        protected virtual int PlayAudioUISound(AudioClip clip)
        {
            int id = EazySoundManager.PlayUISound(clip, SoundVolume);
            return id;
        }
        #endregion

        #region Get Audio list
        private List<Audio> GetAudioFromClips()
        {
            ///if _audioFromClips corresponds to what can be obtained using GetAudioFromClips(),
            ///then you can immediately return it without performing unnecessary calculations
            if (CheckAudioFromClipsChangesAndForRelevance())
            {
                return _audioFromClips;
            }

            List<Audio> audios = new List<Audio>();


            for (int i = 0; i < Mathf.Max(MusicClips.Count, SoundClips.Count, SoundUiClips.Count); i++)
            {
                GetAudioByIndexAndAddToList(audios, i, AudioType.Music);
                GetAudioByIndexAndAddToList(audios, i, AudioType.Sound);
                GetAudioByIndexAndAddToList(audios, i, AudioType.UISound);
            }

            _audioFromClips = audios;

            return audios;
        }

        //adds clips to the assigned list, also returns false if the clip was not added, true if added
        private bool GetAudioByIndexAndAddToList(List<Audio> audios, in int i, in AudioType AudioType)
        {
            List<AudioClip> clips;

            switch (AudioType)
            {
                case AudioType.Music:
                    {
                        clips = MusicClips;
                        break;
                    }
                case AudioType.Sound:
                    {
                        clips = SoundClips;
                        break;
                    }
                case AudioType.UISound:
                    {
                        clips = SoundUiClips;
                        break;
                    }
                default:
                    {
                        return false;
                    }
            }

            if (!(i < clips.Count)) return false;

            Audio audio = EazySoundManager.GetAudio(clips[i], AudioType);

            if (audio == null) return false;

            audios.Add(audio);

            return true;
        }

        #region 
        //returns true if the list stores all current clips
        private bool CheckAudioFromClipsChangesAndForRelevance()
        {
            if (_audioFromClips == null) return false;
            if (_audioFromClips.Count == 0) return false;
            if (!ChekListClipsChanged()) return false;

            foreach (Audio audio in _audioFromClips)
            {
                if (audio == null) return false;
                if (audio.Deleted) return false;
            }

            return true;
        }


        //returns true if the clips have not been modified
        private bool ChekListClipsChanged()
        {
            if (_listsClipsChanged)
            {
                _listsClipsChanged = false;
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
        #endregion

        //if there are no more active clips returns true
        private bool PlaybackOfClipsIsComplete ()
        {
            List<Audio> audios = GetAudioFromClips();

            if (_ignorePlayingCurrentClips)
            {
                _ignorePlayingCurrentClips = false;
                return true;
            }

            foreach (Audio audio in audios)
            {
                if (audio != null && !audio.Deleted)
                {
                    return false;
                }
            }

            return true;
        }

        private void PlayOnAwakeningIfConditionIsMet()
        {
            if (!PlaySoundOnAwake) return;   
            
            Play();
        }

        /// <summary>
        /// Set values at the start
        /// </summary>
        protected virtual void SetVariables()
        {
            _startInvoked = true;
        }

        private bool CheñkInstalledClipsAndDisableIfNotInstalled()
        {
            // returns true if clips are installed, false otherwise
            if (MusicClips.Count == 0 &&
                SoundClips.Count == 0 &&
                SoundUiClips.Count == 0)
            {
                Debug.LogError("Clips not set ", gameObject);
                gameObject.SetActive(false);
                return false;
            }
            else
            {
                gameObject.SetActive(true);
                return true;
            }
        }

        #region editor only
        /// <summary>
        /// Setting values of public variables from the inspector
        /// </summary>
        protected virtual void OnValidate()
        {          
            SoundVolume = _soundVolume;
            Pitch = _pitch;
            RandomPitch = _randomPitch;
            _listsClipsChanged = true;
        }
        #endregion
    }
}
