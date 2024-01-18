using UnityEngine;

namespace Lineri.SoundSystem
{
    public class SoundPocketSteamAudio : SoundPocket
    {
        [Header("SteamAudio settings")]
        public AudioSource AudioSource;

        public override void Play()
        {
            if (!CheckInstalledClipsAndDisableIfMoreOneGroup()) return;
            if (!CheckConditionsSourcesAndDisable()) return;

            base.Play();
        }

        protected override int PlayAudioMusic(AudioClip clip)
        {
            int id = EazySoundManager.PlayMusic(clip, SoundVolume, false, PersistSoundOnSceneLoad,
                FadeInSecond, FadeOutSecond, CurrentMusicFadeOut, Transform3dAudio, this.AudioSource);
            return id;
        }
        protected override int PlayAudioSound(AudioClip clip)
        {
            int id = EazySoundManager.PlaySound(clip, SoundVolume, false, Transform3dAudio, this.AudioSource);
            return id;
        }

        protected override void PlayAudioFromList(bool callPlay)
        {
            if (!PlayClips || !_playWasCalled) return;
            if (!Application.isFocused) return;
            
            PlayClipsOneAfterTheOther(callPlay);            
        }

        protected override void SetVariables()
        {
            base.SetVariables();
            PlayAllClipsTogether = false;
        }

        private bool CheckInstalledClipsAndDisableIfMoreOneGroup()
        {
            if (MusicClips.Count != 0 && (SoundClips.Count != 0 || SoundUiClips.Count != 0) ||
                SoundClips.Count != 0 && (MusicClips.Count != 0 || SoundUiClips.Count != 0) ||
                SoundUiClips.Count != 0 && (SoundClips.Count != 0 || MusicClips.Count != 0))
            {
                Debug.LogError("More than one clip group has been installed. Use no more than one clip group per class.", gameObject);
                gameObject.SetActive(false);
                return false;
            }
            else
            {
                gameObject.SetActive(true);
                return true;
            }
        }

        private bool CheckConditionsSourcesAndDisable()
        {
#if !STEAMAUDIO_ENABLED
            Debug.LogError("Steam Audio has not been imported into the project.", gameObject);
            gameObject.SetActive(false);
            return false;
#else
            if (this.AudioSource == null)
            {
                Debug.LogError("AudioSource was not installed.", gameObject);
                gameObject.SetActive(false);
                return false;
            }

            gameObject.SetActive(true);
            return true;
#endif
        }
    }
}

