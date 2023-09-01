using System;
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
                /// can't make it work :(
                /// due to the pitch change, the calculations of the duration of the clips become incorrect 
                /// and a new clip in the queue may trigger earlier or later than necessary
                /// 0.0.0.7 01
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
        /// audio queue won't play
        public bool PersistSoundOnSceneLoad = false;
        //when called, Play plays one clip instead of the entire chain. ignores LoopClips
        public bool PlayOneClip = false;
        // Play all clips at the start
        public bool PlaySoundOnAwake = false;
        /// <summary>
        /// if enabled, clips will be played every time object becomes active 
        /// </summary>
        /// DOES NOT WORK WITHOUT PlaySoundOnAwake
        //public bool PlayEveryTimeAnObjectBecomesActive = false;
        // Plays all the assigned clips at the same time. only works if IgnoreDuplicateMusic/Sound/UiSound != true
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
        private int _countClipsPlay = 0;
        private int _numberClipPlayedInList = 0;
        private bool _playWasCalled = false;
        #region to optimize GetAudioFromClips()
        private List<Audio> _audioFromClips;
        private List<AudioClip> _musicClipsLastOneUnchanged = new List<AudioClip>();
        private List<AudioClip> _soundClipsLastOneUnchanged = new List<AudioClip>();
        private List<AudioClip> _soundUiClipsLastOneUnchanged = new List<AudioClip>();
        #endregion
        //use it to force new clips to start, even if the clips are already being played
        private bool _ignorePlayingCurrenClips = false;
        private enum ClipType
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
            CheckAndApplyAudioFromClipsChanges();
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

        #region Get Audio list
        private List<Audio> GetAudioFromClips()
        {            
            if (_audioFromClips != null && _audioFromClips.Count != 0)
            {
                return _audioFromClips;
            }

            List<Audio> audios = new List<Audio>();

            ///it is relevant only for the current logic of calling clips,
            ///when, regardless of the type, the current one must end before calling a new one
            sbyte countClipTypeNotAdded = 0;
            for (int i = 0; i < Math.Max(MusicClips.Count, Math.Max(SoundClips.Count, SoundUiClips.Count)); i++)
            {
                if (countClipTypeNotAdded >= 3) break;

                _ = GetAudioByIndexAndAddToList(audios, i, ClipType.Music) ? countClipTypeNotAdded : countClipTypeNotAdded++;
                _ = GetAudioByIndexAndAddToList(audios, i, ClipType.Sound) ? countClipTypeNotAdded : countClipTypeNotAdded++;
                _ = GetAudioByIndexAndAddToList(audios, i, ClipType.SoundUi) ? countClipTypeNotAdded : countClipTypeNotAdded++;
            }

            //_audioFromClips = new List<Audio>((Audio[])audios.ToArray().Clone());
            _audioFromClips = new List<Audio>(audios);
            return audios;
        }

        //adds clips to the assigned list, also returns false if the clip was not added, true if added
        private bool GetAudioByIndexAndAddToList(List<Audio> audios, int i, ClipType clipType)
        {
            List<AudioClip> clips;

            switch (clipType)
            {
                case ClipType.Music:
                    {
                        clips = MusicClips;
                        break;
                    }
                case ClipType.Sound:
                    {
                        clips = SoundClips;
                        break;
                    }
                case ClipType.SoundUi:
                    {
                        clips = SoundUiClips;
                        break;
                    }
                default:
                    {
                        clips = new List<AudioClip>();
                        break;
                    }
            }

            if (!(i < clips.Count)) return false;

            Audio audio = EazySoundManager.GetAudio(clips[i]);

            if (audio == null) return false;

            audios.Add(audio);

            return true;
        }
        #endregion

        #region PlayClip
        private void PlayAudioFromList(bool callPlay)
        {
            if (!Application.isFocused) return;
            if (!PlayClips || !_playWasCalled) return;

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
            if ((_countClipsPlay != 0 && !LoopClips) && !callPlay) return;

            if (!PlaybackOfClipsIsComplete())
            {
                return;
            }

            PlayAllClipsInListAndReturnTime(MusicClips, ClipType.Music);
            PlayAllClipsInListAndReturnTime(SoundClips, ClipType.Sound);
            PlayAllClipsInListAndReturnTime(SoundUiClips, ClipType.SoundUi);

            _countClipsPlay += MusicClips.Count + SoundClips.Count + SoundUiClips.Count;
        }

        private void PlayAllClipsInListAndReturnTime(List<AudioClip> listClips, ClipType clipType)
        {
            foreach (AudioClip clip in listClips)
            {
                PlayClipInListTogether(clip, clipType);
            }
        }

        private int PlayClipInListTogether(AudioClip clip, ClipType clipType)
        {
            int id = PlayAudio(clip, clipType);
            _clipPitch = Pitch + UnityEngine.Random.Range(-RandomPitch, RandomPitch);
            EazySoundManager.GetAudio(id).Pitch = _clipPitch;

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
                PlayClipInList(MusicClips, ClipType.Music);
            }

            if (SoundClips.Count > _numberClipPlayedInList)
            {                
                PlayClipInList(SoundClips, ClipType.Sound);
            }

            if (SoundUiClips.Count > _numberClipPlayedInList)
            {
                PlayClipInList(SoundUiClips, ClipType.SoundUi);
            }

            _numberClipPlayedInList++;
        }

        private int PlayClipInList(List<AudioClip> clips, ClipType clipType)
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

             int id  = PlayAudio(clips[clipNumber], clipType);
             _clipPitch = Pitch + UnityEngine.Random.Range(-RandomPitch, RandomPitch);
             EazySoundManager.GetAudio(id).Pitch = _clipPitch;          
            _countClipsPlay++;

            return id;
        }
        #endregion

        private int PlayAudio(AudioClip clip, ClipType clipType)
        {
            switch (clipType)
            {
                case ClipType.Music:
                    int id = EazySoundManager.PlayMusic(clip, SoundVolume, false,
                        PersistSoundOnSceneLoad, FadeInSecond, FadeOutSecond, CurrentMusicFadeOutSeconds,
                        Transform3dAudio);
                    return id;
                case ClipType.Sound:
                    id = EazySoundManager.PlaySound(clip, SoundVolume, false, Transform3dAudio);
                    return id;
                case ClipType.SoundUi:
                    id = EazySoundManager.PlayUISound(clip, SoundVolume);
                    return id;
                default:
                    unchecked
                    {
                        return (int)Mathf.NegativeInfinity;
                    }                                               
            }
        }

        private bool PlaybackOfClipsIsComplete ()
        {
            if (_ignorePlayingCurrenClips)
            {
                _ignorePlayingCurrenClips = false;
                return true;
            }

            foreach(Audio audio in GetAudioFromClips())
            {
                if (audio.AudioSource != null)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        private void PlayOnAwakeningIfConditionIsMet()
        {
            if (!PlaySoundOnAwake) return;   
            
            Play();
        }

        //setting values at the start
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

        private void CheckAndApplyAudioFromClipsChanges()
        {
            if (_audioFromClips == null) return;

            bool listClipsChanged = ChekListClipsChanged();

            foreach (Audio audio in _audioFromClips)
            {
                if (!(audio == null || listClipsChanged)) 
                { 
                    continue; 
                }

                _audioFromClips.Clear();
                break;
            }           
        }

        private bool ChekListClipsChanged()
        {
            if (MusicClips == _musicClipsLastOneUnchanged ||
                SoundClips == _soundClipsLastOneUnchanged ||
                SoundUiClips == _soundUiClipsLastOneUnchanged)
            {
                _musicClipsLastOneUnchanged = new List<AudioClip>(MusicClips);
                _soundClipsLastOneUnchanged = new List<AudioClip>(SoundClips);
                _soundUiClipsLastOneUnchanged = new List<AudioClip>(SoundUiClips);
                return true;
            }
            else
            {
                return false;
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
