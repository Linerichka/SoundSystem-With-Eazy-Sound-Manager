using System.Collections.Generic;
using UnityEngine;

namespace Lineri.SoundSystem
{
    public class SoundPocket : MonoBehaviour
    {
        //here Sound - all sounds \ audio clips
        [SerializeField] private float _soundVolume = 1f;
        public float SoundVolume
        {
            get { return _soundVolume; }
            set 
            { 
                _soundVolume = Mathf.Clamp(value, 0f, Mathf.Infinity);
                UpdateParametersAllPlayingClips();
            }
        }

        [SerializeField] private float _pitch = 1f;
        public float Pitch
        {
            get { return _pitch; }
            set 
            { 
                _pitch = Mathf.Clamp(value, 0f, 3);
                UpdateParametersAllPlayingClips();
            }
        }

        [SerializeField] private float _randomPitch = 0f;
        public float RandomPitch
        {
            get { return _randomPitch; }
            set { _randomPitch = Mathf.Clamp(value, -3f, 3f); }
        }

        //Use the plugin documentation for these 3 variables
        public float FadeInSecond = 2f;
        public float FadeOutSecond = 2f;
        public float CurrentMusicFadeOutSeconds = -1f;
        //
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
        // Use the plugin documentation for 3d audio AND set null to not use 3D sound
        public Transform Transform3dAudio = null;
        // After the end of the current clip, the new ones will no longer be played
        public bool PlayClips = true;
        public List<AudioClip> MusicClips = new List<AudioClip>();
        public List<AudioClip> SoundClips = new List<AudioClip>();
        public List<AudioClip> SoundUiClips = new List<AudioClip>();

        #region private variables
        //if Start() was called earlier - true, if not - false
        private bool _startInvoked = false; 
        private float _clipPitch = 1f;
        private bool _clipsTogetherPlayCalled; //true if the clips were called to play together
        private int _numberClipPlayedInList = 0;
        private bool _playWasCalled = false;
        #region to optimize GetAudioFromClips()
        private List<Audio> _audioFromClips;
        private bool _listsClipsChanged = false;
        #endregion
        //use it to force new clips to start, even if the clips are already being played
        private bool _ignorePlayingCurrenClips = false;
        private enum _clipType
        {
            Music,
            Sound,
            SoundUi
        }
        #endregion

        private void Start()
        {
            SetVariables();
            PlayOnAwakeningIfConditionIsMet();
        }

        private void Update()
        {
            PlayAudioFromList(false);           
        }

        #region Public void
        public void Play()
        {
            if (!ChekInstalledClipsAndDisableIfNotInstalled()) return;

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
            _ignorePlayingCurrenClips = true;
        }
        #endregion

        //update the parameters of the clips that are currently playing (pitch, volume ...)
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
        private void PlayAudioFromList(bool callPlay)
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
            if (!PlaybackOfClipsIsComplete())
            {
                return;
            }

            PlayAllClipsInList(MusicClips, _clipType.Music);
            PlayAllClipsInList(SoundClips, _clipType.Sound);
            PlayAllClipsInList(SoundUiClips, _clipType.SoundUi);

            _clipsTogetherPlayCalled = true;
        }

        private void PlayAllClipsInList(List<AudioClip> listClips, _clipType _clipType)
        {
            foreach (AudioClip clip in listClips)
            {
                PlayClipInListTogether(clip, _clipType);
            }
        }

        private int PlayClipInListTogether(AudioClip clip, _clipType _clipType)
        {
            int id = PlayAudio(clip, _clipType);
            _clipPitch = Pitch + UnityEngine.Random.Range(-RandomPitch, RandomPitch);
            EazySoundManager.GetAudio(id, (Audio.AudioType)_clipType).Pitch = _clipPitch;

            return id;
        }
        #endregion

        #region Play One After The Other
        private void PlayClipsOneAfterTheOther(bool callPlay)
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
                PlayClipInList(MusicClips, _clipType.Music);
            }

            if (SoundClips.Count > _numberClipPlayedInList)
            {                
                PlayClipInList(SoundClips, _clipType.Sound);
            }

            if (SoundUiClips.Count > _numberClipPlayedInList)
            {
                PlayClipInList(SoundUiClips, _clipType.SoundUi);
            }

            _numberClipPlayedInList++;
        }

        private int PlayClipInList(List<AudioClip> clips, _clipType _clipType)
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

             int id  = PlayAudio(clips[clipNumber], _clipType);
             _clipPitch = Pitch + UnityEngine.Random.Range(-RandomPitch, RandomPitch);
             EazySoundManager.GetAudio(id, (Audio.AudioType)_clipType).Pitch = _clipPitch;

            return id;
        }
        #endregion

        private int PlayAudio(AudioClip clip, _clipType _clipType)
        {
            switch (_clipType)
            {
                case _clipType.Music:
                    int id = EazySoundManager.PlayMusic(clip, SoundVolume, false,
                        PersistSoundOnSceneLoad, FadeInSecond, FadeOutSecond, CurrentMusicFadeOutSeconds,
                        Transform3dAudio);
                    return id;
                case _clipType.Sound:
                    id = EazySoundManager.PlaySound(clip, SoundVolume, false, Transform3dAudio);
                    return id;
                case _clipType.SoundUi:
                    id = EazySoundManager.PlayUISound(clip, SoundVolume);
                    return id;
                default:
                    unchecked
                    {
                        return (int)Mathf.NegativeInfinity;
                    }                                               
            }
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
                GetAudioByIndexAndAddToList(audios, i, _clipType.Music);
                GetAudioByIndexAndAddToList(audios, i, _clipType.Sound);
                GetAudioByIndexAndAddToList(audios, i, _clipType.SoundUi);
            }

            _audioFromClips = audios;

            return audios;
        }

        //adds clips to the assigned list, also returns false if the clip was not added, true if added
        private bool GetAudioByIndexAndAddToList(List<Audio> audios, int i, _clipType _clipType)
        {
            List<AudioClip> clips;

            switch (_clipType)
            {
                case _clipType.Music:
                    {
                        clips = MusicClips;
                        break;
                    }
                case _clipType.Sound:
                    {
                        clips = SoundClips;
                        break;
                    }
                case _clipType.SoundUi:
                    {
                        clips = SoundUiClips;
                        break;
                    }
                default:
                    {
                        clips = null;
                        break;
                    }
            }

            if (!(i < clips.Count)) return false;

            Audio audio = EazySoundManager.GetAudio(clips[i], (Audio.AudioType)_clipType);

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

            if (_ignorePlayingCurrenClips)
            {
                _ignorePlayingCurrenClips = false;
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

        //set values at the start
        private void SetVariables()
        {
            _startInvoked = true;
        }

        private bool ChekInstalledClipsAndDisableIfNotInstalled()
        {
            // returns true if clips are installed, false otherwise
            if (MusicClips.Count == 0 &&
                SoundClips.Count == 0 &&
                SoundUiClips.Count == 0)
            {
                Debug.LogWarning("Clips not set ", gameObject);
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
        // setting values of public variables from the inspector
        void OnValidate()
        {          
            SoundVolume = _soundVolume;
            Pitch = _pitch;
            RandomPitch = _randomPitch;
        }
        #endregion
    }
}
