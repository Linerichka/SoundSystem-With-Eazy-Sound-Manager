using UnityEngine;

namespace Lineri.SoundSystem
{
    /// <summary>
    /// The audio object
    /// </summary>
    public class Audio
    {
        /// <summary>
        /// The ID of the Audio
        /// </summary>
        public int AudioID { get; private set; }

        /// <summary>
        /// The type of the Audio
        /// </summary>
        public AudioType Type { get; private set; }

        /// <summary>
        /// Whether the audio is currently playing
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Whether the audio is paused
        /// </summary>
        public bool Paused { get; private set; }

        /// <summary>
        /// Whether the audio is stopping
        /// </summary>
        public bool Stopping { get; private set; }

        /// <summary>
        /// Whether the audio is created and updated at least once. 
        /// </summary>
        public bool Activated { get; private set; }

        public bool Deleted { get; private set; }

        /// <summary>
        /// The volume of the audio. Use SetVolume to change it.
        /// </summary>
        public float Volume
        {
            get => _volume;
            private set => _volume = Mathf.Clamp(value, 0f, 1f);      
        }

        /// <summary>
        /// The audio source that is responsible for this audio. Do not modify the audiosource directly as it could result to unpredictable behaviour. Use the Audio class instead.
        /// </summary>
        public AudioSource AudioSource { get; private set; }

        /// <summary>
        /// The source transform of the audio.
        /// </summary>
        public Transform SourceTransform { get; private set; }

        /// <summary>
        /// Audio clip to play/is playing
        /// </summary>
        public AudioClip Clip
        {
            get => _clip;
            private set
            {
                _clip = value;
                AudioSource.clip = _clip;
            }
        }

        /// <summary>
        /// Whether the audio will be lopped
        /// </summary>
        public bool Loop
        {
            get => _loop;
            set
            {
                _loop = value;
                AudioSource.loop = _loop;             
            }
        }

        /// <summary>
        /// Whether the audio is muted
        /// </summary>
        public bool Mute
        {
            get => _mute;
            set
            {
                _mute = value;
                AudioSource.mute = _mute;              
            }
        }

        /// <summary>
        /// Sets the priority of the audio
        /// </summary>
        public int Priority
        {
            get => _priority;
            set
            {
                _priority = Mathf.Clamp(value, 0, 256);
                AudioSource.priority = _priority;
            }
        }

        /// <summary>
        /// The pitch of the audio
        /// </summary>
        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = Mathf.Clamp(value, -3, 3);
                AudioSource.pitch = _pitch;              
            }
        }

        /// <summary>
        /// Pans a playing sound in a stereo way (left or right). This only applies to sounds that are Mono or Stereo.
        /// </summary>
        public float StereoPan
        {
            get => _stereoPan;
            set
            {
                _stereoPan = Mathf.Clamp(value, -1, 1);
                AudioSource.panStereo = _stereoPan;              
            }
        }

        /// <summary>
        /// Sets how much this AudioSource is affected by 3D spatialisation calculations (attenuation, doppler etc). 0.0 makes the sound full 2D, 1.0 makes it full 3D.
        /// </summary>
        public float SpatialBlend
        {
            get => _spatialBlend;
            set
            {
                _spatialBlend = Mathf.Clamp01(value);
                AudioSource.spatialBlend = _spatialBlend;
            }
        }

        /// <summary>
        /// The amount by which the signal from the AudioSource will be mixed into the global reverb associated with the Reverb Zones.
        /// </summary>
        public float ReverbZoneMix
        {
            get => _reverbZoneMix;
            set
            {
                _reverbZoneMix = Mathf.Clamp(value, 0, 1.1f);
                AudioSource.reverbZoneMix = _reverbZoneMix;              
            }
        }

        /// <summary>
        /// The doppler scale of the audio
        /// </summary>
        public float DopplerLevel
        {
            get => _dopplerLevel;
            set
            {
                _dopplerLevel = Mathf.Clamp(value, 0, 5);
                AudioSource.dopplerLevel = _dopplerLevel;              
            }
        }

        /// <summary>
        /// The spread angle (in degrees) of a 3d stereo or multichannel sound in speaker space.
        /// </summary>
        public float Spread
        {
            get => _spread;
            set
            {
                _spread = Mathf.Clamp(value, 0, 360);
                AudioSource.spread = _spread;                
            }
        }

        /// <summary>
        /// How the audio attenuates over distance
        /// </summary>
        public AudioRolloffMode RolloffMode
        {
            get => _rolloffMode;
            set
            {
                _rolloffMode = value;
                AudioSource.rolloffMode = _rolloffMode;               
            }
        }

        /// <summary>
        /// (Logarithmic rolloff) MaxDistance is the distance a sound stops attenuating at.
        /// </summary>
        public float Max3DDistance
        {
            get => _max3DDistance;
            private set
            {
                AudioSource.maxDistance = value;           
                _max3DDistance = AudioSource.maxDistance;
            }
        }

        /// <summary>
        /// Within the Min distance the audio will cease to grow louder in volume.
        /// </summary>
        public float Min3DDistance
        {
            get => _min3DDistance;
            private set
            {
                AudioSource.minDistance = value;
                _min3DDistance = AudioSource.minDistance;
            }
        }

        /// <summary>
        /// Whether the audio persists in between scene changes
        /// </summary>
        public bool Persist;

        /// <summary>
        /// How many seconds it needs for the audio to fade in/ reach target volume (if higher than current)
        /// </summary>
        public float FadeInSeconds;

        /// <summary>
        /// How many seconds it needs for the audio to fade out/ reach target volume (if lower than current)
        /// </summary>
        public float FadeOutSeconds;

        public bool DeleteAudioSource = true;

        /// <summary>
        /// Enum representing the type of audio
        /// </summary>
        public enum AudioType
        {
            Music,
            Sound,
            UISound
        }

        private static int _audioCounter = 0;

        private AudioClip _clip;
        private float _volume;
        private bool _loop;
        private bool _mute;
        private int _priority;
        private float _pitch;
        private float _stereoPan;
        private float _spatialBlend;
        private float _reverbZoneMix;
        private float _dopplerLevel;
        private float _spread;
        private AudioRolloffMode _rolloffMode;
        private float _max3DDistance;
        private float _min3DDistance;

        private float _targetVolume;
        private float _initTargetVolume;
        private float _tempFadeSeconds = -1f;
        private float _fadeInterpolater = 0f;
        private float _onFadeStartVolume;

        public Audio(in AudioType audioType, AudioClip clip, in bool loop, in bool persist, in float volume, in float fadeInValue, 
            in float fadeOutValue, Transform sourceTransform, AudioSource audioSource, in bool overrideAudioSourceSettings = true)
        {
            // Set unique audio ID
            AudioID = _audioCounter;
            _audioCounter++;

            /// Initialize values
            /// Use private fields for setting to prevent parameters from being applied to the AudioSource
            this.AudioSource = audioSource;
            this.Type = audioType;
            this.Clip = clip;
            this.SourceTransform = sourceTransform;
            this._loop = loop;
            this.Persist = persist;
            this.FadeInSeconds = fadeInValue;
            this.FadeOutSeconds = fadeOutValue;
            this.DeleteAudioSource = overrideAudioSourceSettings;
            this._targetVolume = volume;
            this._initTargetVolume = volume;

            // Set audiosource default values
            _mute = false;
            _priority = 128;
            _pitch = 1;
            _stereoPan = 0;

            if (sourceTransform != null && sourceTransform != EazySoundManager.Gameobject.transform) _spatialBlend = 1;            

            _reverbZoneMix = 1;
            _dopplerLevel = 1;
            _spread = 0;
            _rolloffMode = AudioRolloffMode.Logarithmic;
            _min3DDistance = 1;
            _max3DDistance = 500;

            // Initliaze states
            IsPlaying = false;
            Paused = false;
            Activated = false;
            Deleted = false;

            SetValueAudioSource(overrideAudioSourceSettings);
        }

        /// <summary>
        /// Initializes the audiosource component with the appropriate values
        /// </summary>
        private void SetValueAudioSource(in bool overrideAudioSourceSettings)
        {
            if (overrideAudioSourceSettings)
            {
                AudioSource.clip = Clip;
                AudioSource.loop = Loop;
                AudioSource.mute = Mute;
                AudioSource.volume = Volume;
                AudioSource.priority = Priority;
                AudioSource.pitch = Pitch;
                AudioSource.panStereo = StereoPan;
                AudioSource.spatialBlend = SpatialBlend;
                AudioSource.reverbZoneMix = ReverbZoneMix;
                AudioSource.dopplerLevel = DopplerLevel;
                AudioSource.spread = Spread;
                AudioSource.rolloffMode = RolloffMode;
                AudioSource.maxDistance = Max3DDistance;
                AudioSource.minDistance = Min3DDistance;
            }
            //uses the current audio source settings, except for some of them
            else
            {
                AudioSource.clip = Clip;
                AudioSource.loop = Loop;
                AudioSource.volume = Volume;
                AudioSource.pitch = Pitch;
                Mute = AudioSource.mute;
                AudioSource.priority = Priority;
                StereoPan = AudioSource.panStereo;
                SpatialBlend = AudioSource.spatialBlend;
                ReverbZoneMix = AudioSource.reverbZoneMix;
                DopplerLevel = AudioSource.dopplerLevel;
                Spread = AudioSource.spread;
                RolloffMode = AudioSource.rolloffMode;
                Max3DDistance = AudioSource.maxDistance;
                Min3DDistance = AudioSource.minDistance;
            }
        }

        /// <summary>
        /// Update loop of the Audio. This is automatically called from the sound manager itself. Do not use this function anywhere else, as it may lead to unwanted behaviour.
        /// </summary>
        public void Update()
        {
            if (!Activated)
            {
                Activated = true;
                _fadeInterpolater = -Time.unscaledDeltaTime;
            }

            // Increase/decrease volume to reach the current target
            if (!Mathf.Approximately(Volume, _targetVolume))
            {
                float fadeValue;
                _fadeInterpolater += Time.unscaledDeltaTime;

                if (Volume > _targetVolume)
                {
                    fadeValue = Mathf.Approximately(_tempFadeSeconds, -1f) ? FadeOutSeconds: _tempFadeSeconds;
                }
                else
                {
                    fadeValue = Mathf.Approximately(_tempFadeSeconds, -1f) ? FadeInSeconds : _tempFadeSeconds;
                }

                Volume = Mathf.Lerp(_onFadeStartVolume, _targetVolume, _fadeInterpolater / fadeValue);
            }
            else if (!Mathf.Approximately(_tempFadeSeconds, -1))
            {
                _tempFadeSeconds = -1;
            }

            // Set the volume, taking into account the global volumes as well.
            switch (Type)
            {
                case AudioType.Music:
                    {
                        AudioSource.volume = Volume * EazySoundManager.GlobalMusicVolume * EazySoundManager.GlobalVolume;
                        break;
                    }
                case AudioType.Sound:
                    {
                        AudioSource.volume = Volume * EazySoundManager.GlobalSoundsVolume * EazySoundManager.GlobalVolume;
                        break;
                    }
                case AudioType.UISound:
                    {
                        AudioSource.volume = Volume * EazySoundManager.GlobalUISoundsVolume * EazySoundManager.GlobalVolume;
                        break;
                    }
            }

            // Completely stop audio if it finished the process of stopping
            if (Mathf.Approximately(Volume, 0f) && Stopping)
            {
                AudioSource.Stop();
                Stopping = false;
                IsPlaying = false;
                Paused = false;
            }

            // Update playing status
            if (AudioSource.isPlaying != IsPlaying && Application.isFocused)
            {
                IsPlaying = AudioSource.isPlaying;
            }
        }

        /// <summary>
        /// Start playing audio clip from the beginning
        /// </summary>
        public void Play()
        {
            Play(_initTargetVolume);
        }

        /// <summary>
        /// Start playing audio clip from the beggining
        /// </summary>
        /// <param name="volume">The target volume</param>
        public void Play(float volume)
        {
            IsPlaying = true;
            AudioSource.Play();
            SetVolume(volume);
        }

        /// <summary>
        /// Stop playing audio clip
        /// </summary>
        public void Stop()
        {
            if (Stopping) return;

            Stopping = true;
            SetVolume(0f);
        }

        /// <summary>
        /// Pause playing audio clip
        /// </summary>
        public void Pause()
        {
            Paused = true;
            AudioSource.Pause();
        }

        /// <summary>
        /// Resume playing audio clip
        /// </summary>
        public void UnPause()
        {
            AudioSource.UnPause();
            Paused = false;
        }

        /// <summary>
        /// Sets the audio volume
        /// </summary>
        /// <param name="volume">The target volume</param>
        public void SetVolume(float volume)
        {
            if (volume > _targetVolume)
            {
                SetVolume(volume, FadeOutSeconds);
            }
            else
            {
                SetVolume(volume, FadeInSeconds);
            }
        }

        /// <summary>
        /// Sets the audio volume
        /// </summary>
        /// <param name="volume">The target volume</param>
        /// <param name="fadeSeconds">How many seconds it needs for the audio to fade in/out to reach target volume. If passed, it will override the Audio's fade in/out seconds, but only for this transition</param>
        public void SetVolume(float volume, float fadeSeconds)
        {
            SetVolume(volume, fadeSeconds, this.Volume);
        }

        /// <summary>
        /// Sets the audio volume
        /// </summary>
        /// <param name="volume">The target volume</param>
        /// <param name="fadeSeconds">How many seconds it needs for the audio to fade in/out to reach target volume. If passed, it will override the Audio's fade in/out seconds, but only for this transition</param>
        /// <param name="startVolume">Immediately set the volume to this value before beginning the fade. If not passed, the Audio will start fading from the current volume towards the target volume</param>
        public void SetVolume(float volume, float fadeSeconds, float startVolume)
        {
            _targetVolume = Mathf.Clamp01(volume);
            _fadeInterpolater = 0f;
            _onFadeStartVolume = startVolume;
            _tempFadeSeconds = fadeSeconds;
        }

        /// <summary>
        /// Sets the Audio 3D distances
        /// </summary>
        /// <param name="min">the min distance</param>
        /// <param name="max">the max distance</param>
        public void Set3DDistances(float min, float max)
        { 
            Min3DDistance = min;
            Max3DDistance = max;
            Max3DDistance = max;
            Min3DDistance = min;
        }

        public void Delete()
        {
            AudioSource.Stop();
            Stopping = false;
            IsPlaying = false;
            Paused = false;
            AudioSource = null;
            Deleted = true;
        }
    }
}