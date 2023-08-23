using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lineri.SoundSystem
{
    public class SoundPocket : MonoBehaviour
    {
        //here Sound - all sounds \ audio clips
        public float SoundVolume = 1f;
        public float Pitch = 1f;   
        public float RandomPitch = 0f; 
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
        #region time to calculate clip playback
        // it is calculated by the longest length of clips
        // NOT INCREASED FROM Time.timeScale
        private float _timeMaxClipPlayed = 0f; // use it to play together AND max lenght clip in clips lists
        private float _timeClipPlayed = 0f; // use it to play one after the other
        private float _timeClipStartPlayed = 0f; // use it to play one after the other
        private float _timeClipsStartPlayed = 0f; // use it to play together
        //use the bottom two to find the pause time
        private float _timeClipsStartPause = 0f; // time when clips start pausing
        private float _timeClipsStopPause = 0f; // the time of the end of the pause of clips
        #endregion
        private float _clipPitch = 1f;
        private int _countClipsPlay = 0;
        private int _numberClipPlayedInList = 0;
        private bool _playWasCalled = false;
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
        }

        #region Public void
        public void Play()
        {
            if (MusicClips.Count == 0 &&
                SoundClips.Count == 0 &&
                SoundUiClips.Count == 0)
            {
                Debug.Log("Clips not set " + gameObject);
                return;
            }

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

            _timeClipsStartPause = Time.realtimeSinceStartup;
            _timeClipsStopPause = Mathf.Infinity;
        }

        public void UnPauseClipsPlayning()
        {
            foreach (Audio audio in GetAudioFromClips())
            {
                audio.UnPause();
            }

            _timeClipsStopPause = Time.realtimeSinceStartup;
        }
        #endregion

        private List<Audio> GetAudioFromClips()
        {
            List<Audio> audios = new List<Audio>();

            for (int i = 0; i < Math.Max(MusicClips.Count, Math.Max(SoundClips.Count, SoundUiClips.Count)); i++)
            {
                if (i < MusicClips.Count)
                {
                    Audio audio = EazySoundManager.GetAudio(MusicClips[i]);
                    audios.Add(audio);
                }

                if (i < SoundClips.Count)
                {
                    Audio audio = EazySoundManager.GetAudio(SoundClips[i]);
                    audios.Add(audio);
                }

                if (i < SoundUiClips.Count)
                {
                    Audio audio = EazySoundManager.GetAudio(SoundUiClips[i]);
                    audios.Add(audio);
                }

            }
            
            return audios;
        }

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

            if ((Time.realtimeSinceStartup - _timeClipsStartPlayed
                - (_timeClipsStopPause - _timeClipsStartPause)) < _timeMaxClipPlayed)
            {
                return;
            }

            _timeMaxClipPlayed = 0;

            _timeMaxClipPlayed = Math.Max(_timeMaxClipPlayed, PlayAllClipsInListAndReturnTime(MusicClips, ClipType.Music));
            _timeMaxClipPlayed = Math.Max(_timeMaxClipPlayed, PlayAllClipsInListAndReturnTime(SoundClips, ClipType.Sound));
            _timeMaxClipPlayed = Math.Max(_timeMaxClipPlayed, PlayAllClipsInListAndReturnTime(SoundUiClips, ClipType.SoundUi));

            _countClipsPlay += MusicClips.Count + SoundClips.Count + SoundUiClips.Count;
            _timeClipsStartPlayed = Time.realtimeSinceStartup;
        }

        private float PlayAllClipsInListAndReturnTime(List<AudioClip> listClips, ClipType clipType)
        {
            float timeMaxClipPlayed = 0f;

            foreach (AudioClip clip in listClips)
            {
                timeMaxClipPlayed = Math.Max(timeMaxClipPlayed, PlayClipInListTogether(clip, clipType));
            }

            return timeMaxClipPlayed;
        }

        private float PlayClipInListTogether(AudioClip clip, ClipType clipType)
        {
            int id = PlayAudio(clip, clipType);
            _clipPitch = Pitch + UnityEngine.Random.Range(0, RandomPitch);
            EazySoundManager.GetAudio(id).Pitch = _clipPitch;

            return (clip.length / _clipPitch);
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
                    _numberClipPlayedInList = 0;
                }
            }

            if (((Time.realtimeSinceStartup - _timeClipStartPlayed 
                - (_timeClipsStopPause - _timeClipsStartPause)) <
                _timeClipPlayed) || (!callPlay && PlayOneClip))
            {               
                return;
            }

            _timeClipPlayed = 0;
            float timeClipPlayed = 0f;

            if (MusicClips.Count > _numberClipPlayedInList)
            {
                timeClipPlayed = PlayClipInList(MusicClips, ClipType.Music);
            }

            _timeClipPlayed = Math.Max(_timeClipPlayed, timeClipPlayed);

            if (SoundClips.Count > _numberClipPlayedInList)
            {                
                timeClipPlayed = PlayClipInList(SoundClips, ClipType.Sound);
            }

            _timeClipPlayed = Math.Max(_timeClipPlayed, timeClipPlayed);

            if (SoundUiClips.Count > _numberClipPlayedInList)
            {
                timeClipPlayed = PlayClipInList(SoundUiClips, ClipType.SoundUi);
            }

            _timeClipPlayed = Math.Max(_timeClipPlayed, timeClipPlayed);
            _timeClipStartPlayed = Time.realtimeSinceStartup;
            _numberClipPlayedInList++;
        }

        private float PlayClipInList(List<AudioClip> clips, ClipType clipType)
        {
            int clipNumber = 0;

            if (RandomPlayClip)
            {
                clipNumber = UnityEngine.Random.Range(0, clips.Count);
            }
            else
            {
                clipNumber = _numberClipPlayedInList;
            }

             int id  = PlayAudio(clips[clipNumber], clipType);
             _clipPitch = Pitch + UnityEngine.Random.Range(0, RandomPitch);
             EazySoundManager.GetAudio(id).Pitch = _clipPitch;          
            _countClipsPlay++;

            return clips[clipNumber].length / _clipPitch;
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
        #endregion

        private void PlayOnAwakeningIfConditionIsMet()
        {
            if (!PlaySoundOnAwake) return;   
            
            Play();
        }

        private void SetVariables()
        {
            _timeClipStartPlayed = Time.realtimeSinceStartup;
            _timeClipsStartPlayed = Time.realtimeSinceStartup;
        }
    }
}
