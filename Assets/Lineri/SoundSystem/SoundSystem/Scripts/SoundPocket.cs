using System;
using System.Collections.Generic;
using UnityEngine;
using AudioType = Lineri.SoundSystem.Audio.AudioType;

namespace Lineri.SoundSystem
{
    public class SoundPocket : MonoBehaviour
    {
        [Header("Clips setting")]
        // Here Sound - all sounds \ audio clips
        [SerializeField, Range(0, 1)] private float _soundVolume = 1f;
        public float SoundVolume
        {
            get => _soundVolume;
            set
            {
                _soundVolume = Mathf.Clamp(value, 0f, 1);
                UpdateParametersAllPlayingClips(_typeUpdate.Volume);
            }
        }

        [SerializeField, Range(-3, 3)] private float _pitch = 1f;
        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = Mathf.Clamp(value, -3f, 3f);
                UpdateParametersAllPlayingClips(_typeUpdate.Pitch);
            }
        }

        [SerializeField, Range(0, 3)] private float _randomPitch = 0f;
        public float RandomPitch
        {
            get => _randomPitch;
            set => _randomPitch = Mathf.Clamp(value, 0, 3f);
        }

        // Use the plugin documentation for these 3 variables
        [SerializeField] private float _fadeInSecond = 0f;
        public float FadeInSecond
        {
            get => _fadeInSecond;
            set
            {
                if (value < 0f) _fadeInSecond = 0f;
                else _fadeInSecond = value;
            }
        }
        [SerializeField] private float _fadeOutSecond = 0f;
        public float FadeOutSecond
        {
            get => _fadeOutSecond;
            set
            {
                if (value < 0f) _fadeOutSecond = 0f;
                else _fadeOutSecond = value;
            }
        }

        [System.Obsolete("Use 'CurrentMusicFadeOut' instead.")]
        public float CurrentMusicFadeOutSeconds = -1f;
        public float CurrentMusicFadeOut
        {
#pragma warning disable CS0618
            get => CurrentMusicFadeOutSeconds;
            set
            {
                if (Mathf.Approximately(-1f, value)) CurrentMusicFadeOutSeconds = -1f;
                else if (value < 0f) CurrentMusicFadeOutSeconds = 0f;
                else CurrentMusicFadeOutSeconds = value;
            }
#pragma warning restore CS0618
        }
        //
        // If true, then fade in applies only to the first clip in the queue
        public bool FadeInFirstClipInQueue = false;
        /// If true, fade in applies only to the clip that is played after calling the Play method from outside. 
        /// Also, if Loop is set to True, then fade in will not be applied to the first clip when the queue is repeated.
        public bool FadeInClipAfterCallPlay = false;
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
        // When called, Play plays one clip instead of the entire chain. ignores LoopClips
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

        #region CallBacks
        public event Action OnClipsQueuePlayedStart;
        // Called only if PlayAllClipsTogether = false
        public event Action OnClipPlayedStart;
        // It can be called repeatedly
        public event Action ClipsQueueEnded;
        public event Action ClipsQueueReset;
        #endregion
        #region Private variables
        //if Start() was called earlier - true, if not - false
        private bool _startInvoked = false;
        //true if the clips were called to play together
        private bool _clipsTogetherPlayCalled;
        private int _numberClipPlayedInList = 0;
        protected bool _playWasCalled = false;
        #region To optimize GetAudioFromClips()
        private List<Audio> _audioFromClips = new();
        private List<Audio> _audiosResultGetAudio = new();
        private bool _listsClipsChanged = false;
        #endregion
        //use it to force new clips to start, even if the clips are already being played
        private bool _ignorePlayingCurrentClips = false;
        private bool _callPlay = true;
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
            ClipsQueueReset?.Invoke();
            _numberClipPlayedInList = 0;
            _clipsTogetherPlayCalled = false;
        }

        public void StopClipsPlayning()
        {
            foreach (Audio audio in GetAudioFromClips()) audio.Stop();

            PlayClips = false;

            ResetClipQueue();
        }

        public void PauseClipsPlayning()
        {
            foreach (Audio audio in GetAudioFromClips()) audio.Pause();
        }

        public void UnPauseClipsPlayning()
        {
            foreach (Audio audio in GetAudioFromClips()) audio.UnPause();
        }

        public void ResetTimeClipsPlayed()
        {
            _ignorePlayingCurrentClips = true;
        }

        /// <summary>
        /// Sets the volume instantly, ignoring the Fade In and Fade Out values.
        /// </summary>
        public void SetVolumeInstantly()
        {
            foreach (Audio audio in GetAudioFromClips()) audio.SetVolume(SoundVolume, 0f);
        }
        #endregion

        private enum _typeUpdate
        {
            All,
            Volume,
            Pitch
        }

        /// <summary>
        /// Update the parameters of the clips that are currently playing (pitch, volume).
        /// </summary>
        private void UpdateParametersAllPlayingClips(in _typeUpdate typeUpdate)
        {
            if (!(Application.isPlaying && _startInvoked)) return;
            if (EazySoundManager.Gameobject == null) return;

            foreach (Audio audio in GetAudioFromClips())
            {
                if (typeUpdate == _typeUpdate.All)
                {
                    audio.SetVolume(SoundVolume);
                    audio.Pitch = Pitch;
                    continue;
                }
                else if (typeUpdate == _typeUpdate.Volume) audio.SetVolume(SoundVolume);
                else if (typeUpdate == _typeUpdate.Pitch) audio.Pitch = Pitch;
            }
        }

        #region PlayClip
        protected virtual void PlayAudioFromList(in bool callPlay)
        {
            if (!_playWasCalled || !PlayClips || !Application.isFocused) return;

            if (PlayAllClipsTogether) PlayAllClipsInListTogether(callPlay);        
            else PlayClipsOneAfterTheOther(callPlay);          
        }

        #region Play Together
        private void PlayAllClipsInListTogether(in bool callPlay)
        {
            if (!((_clipsTogetherPlayCalled && LoopClips) || callPlay)) return;
            if (!PlaybackOfClipsIsComplete()) return;
            else if (callPlay) OnClipsQueuePlayedStart?.Invoke();

            _callPlay = callPlay;

            PlayAllClipsInList(MusicClips, AudioType.Music);
            PlayAllClipsInList(SoundClips, AudioType.Sound);
            PlayAllClipsInList(SoundUiClips, AudioType.UISound);

            _clipsTogetherPlayCalled = true;
        }

        private void PlayAllClipsInList(List<AudioClip> listClips, in AudioType AudioType)
        {
            foreach (AudioClip clip in listClips)
            {
                PlayClipInListTogether(clip, AudioType);
            }
        }

        private int PlayClipInListTogether(AudioClip clip, in AudioType AudioType)
        {
            int id = PlayAudio(clip, AudioType);
            float clipPitch = Pitch + UnityEngine.Random.Range(-RandomPitch, RandomPitch);
            EazySoundManager.GetAudio(id, AudioType).Pitch = clipPitch;

            return id;
        }
        #endregion

        #region Play One After The Other
        protected void PlayClipsOneAfterTheOther(in bool callPlay)
        {
            if (!(MusicClips.Count > _numberClipPlayedInList ||
                SoundClips.Count > _numberClipPlayedInList ||
                SoundUiClips.Count > _numberClipPlayedInList))
            {
                if (LoopClips || callPlay) ResetClipQueue();
            }

            if (!PlaybackOfClipsIsComplete() || (!callPlay && PlayOneClip))
            {
                return;
            }
            else if (callPlay) OnClipsQueuePlayedStart?.Invoke();

            OnClipPlayedStart?.Invoke();
            _callPlay = callPlay;

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

        private int PlayClipInList(List<AudioClip> clips, in AudioType AudioType)
        {
            int clipNumber;

            if (RandomPlayClip) clipNumber = UnityEngine.Random.Range(0, clips.Count);         
            else clipNumber = _numberClipPlayedInList;           

            int id = PlayAudio(clips[clipNumber], AudioType);
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

        protected virtual int PlayAudioMusic(AudioClip clip, AudioSource audioSource = null)
        {
            int id = EazySoundManager.PlayMusic(clip, SoundVolume, false, PersistSoundOnSceneLoad,
                FadeInCanSet() ? FadeInSecond : 0f,
                FadeOutSecond, CurrentMusicFadeOut, Transform3dAudio, audioSource);
            return id;
        }

        protected virtual int PlayAudioSound(AudioClip clip, AudioSource audioSource = null)
        {
            int id = EazySoundManager.PlaySound(clip, SoundVolume, false, PersistSoundOnSceneLoad,
                FadeInCanSet() ? FadeInSecond : 0f, 
                FadeOutSecond, Transform3dAudio, audioSource);
            return id;
        }

        /// <summary>
        /// If there are no conditions prohibiting setting Fade In for the clip, then returns True.
        /// </summary>
        private bool FadeInCanSet()
        {
            // Ñheck whether we can apply fade in to the clip in normal mode if there are no forbidding conditions
            bool canApplyFadeInToClip = (!FadeInFirstClipInQueue && !FadeInClipAfterCallPlay);
            // Check whether we can apply fade in to the clip if it is the first in the queue and the conditions allow
            bool canApplyFadeInToFirstClip = (FadeInFirstClipInQueue && _numberClipPlayedInList == 0 && !PlayAllClipsTogether);
            // Check whether we can apply fade in to the clip if it is playing after calling Play.
            bool canApplyFadeInBecausePlayningClipAfterCallPlay = (FadeInClipAfterCallPlay && _callPlay);
            bool fadeInSet = (canApplyFadeInToFirstClip || canApplyFadeInBecausePlayningClipAfterCallPlay || canApplyFadeInToClip);
            return fadeInSet;
        }

        protected virtual int PlayAudioUISound(AudioClip clip)
        {
            int id = EazySoundManager.PlayUISound(clip, SoundVolume);
            return id;
        }
        #endregion

        #region Get Audio list
        /// <summary>
        /// Returns Audio of the current clips.
        /// </summary>
        private List<Audio> GetAudioFromClips()
        {
            ///if _audioFromClips corresponds to what can be obtained using GetAudioFromClips(),
            ///then you can immediately return it without performing unnecessary calculations
            if (CheckAudioFromClipsChangesAndForRelevance())
            {
                return _audioFromClips;
            }

            _audiosResultGetAudio.Clear();

            for (int i = 0; i < Mathf.Max(MusicClips.Count, Mathf.Max(SoundClips.Count, SoundUiClips.Count)); i++)
            {
                GetAudioByIndexAndAddToList(_audiosResultGetAudio, i, AudioType.Music);
                GetAudioByIndexAndAddToList(_audiosResultGetAudio, i, AudioType.Sound);
                GetAudioByIndexAndAddToList(_audiosResultGetAudio, i, AudioType.UISound);
            }

            _audioFromClips = _audiosResultGetAudio;

            return _audiosResultGetAudio;
        }

        /// <summary>
        /// Adds clips to the assigned list, also returns false if the clip was not added, true if added
        /// </summary>
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

        #region Cheking changes
        /// <summary>
        /// Returns true if the list stores all current clips
        /// </summary>
        private bool CheckAudioFromClipsChangesAndForRelevance()
        {
            int audioCount = _audioFromClips.Count;

            if (audioCount == 0 || !ChekListClipsChanged()) return false;

            for (int i = 0; i < audioCount; i++)
            {
                Audio audio = _audioFromClips[i];

                if (audio == null || audio.Deleted) return false;
            }

            return true;
        }


        /// <summary>
        /// Returns true if the clips have not been modified
        /// </summary>
        private bool ChekListClipsChanged()
        {
            if (_listsClipsChanged)
            {
                _listsClipsChanged = false;
                return false;
            }
            else return true;
        }
        #endregion
        #endregion

        /// <summary>
        /// If the clips are no longer playing, returns true.
        /// If there is at least one clip that is being played, it returns false.
        /// </summary>
        private bool PlaybackOfClipsIsComplete()
        {
            if (_ignorePlayingCurrentClips)
            {
                _ignorePlayingCurrentClips = false;
                ClipsQueueEnded?.Invoke();
                return true;
            }

            List<Audio> audios = GetAudioFromClips();
            int audioCount = audios.Count;

            for (int i = 0; i < audioCount; i++)
            {
                Audio audio = audios[i];
                if (audio != null && !audio.Deleted) return false;
            }


            ClipsQueueEnded?.Invoke();
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

        /// <summary>
        /// Returns true if clips are installed.
        /// If the clips are not set, it calls gameObject.SetActive(false) and returns false.
        /// </summary>
        private bool CheñkInstalledClipsAndDisableIfNotInstalled()
        {
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

        #region Editor only
        /// <summary>
        /// Setting values of public variables from the inspector
        /// </summary>
        protected virtual void OnValidate()
        {
            SoundVolume = _soundVolume;
            Pitch = _pitch;
            RandomPitch = _randomPitch;
            FadeInSecond = _fadeInSecond;
            FadeOutSecond = _fadeOutSecond;
#pragma warning disable CS0618
            CurrentMusicFadeOut = CurrentMusicFadeOutSeconds;
#pragma warning restore CS0618
            _listsClipsChanged = true;
        }
        #endregion
    }
}
