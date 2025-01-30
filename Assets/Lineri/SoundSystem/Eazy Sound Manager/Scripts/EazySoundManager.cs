using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using AudioType = Lineri.SoundSystem.Audio.AudioType;

namespace Lineri.SoundSystem
{
    /// <summary>
    /// Static class responsible for playing and managing audio and sounds.
    /// </summary>
    public class EazySoundManager : MonoBehaviour
    {
        /// <summary>
        /// The gameobject that the sound manager is attached to
        /// </summary>
        public static GameObject Gameobject
        {
            get => Instance.gameObject;
        }

        /// <summary>
        /// If set to true, when starting a new music Clip, all others will be stopped
        /// </summary>
        public static bool OnlyOnePlayableMusicClip = true;

        public static bool CanPlayInBackground = true;
        public static bool CanPlay => Application.isFocused || CanPlayInBackground;

        /// <summary>
        /// Global volume ranging from 0 to 1
        /// </summary>
        public static float GlobalVolume
        {
            get => _globalVolume;
            set => _globalVolume = Mathf.Clamp01(value);
        }
        private static float _globalVolume = 1f;

        /// <summary>
        /// Global music volume ranging from 0 to 1
        /// </summary>
        public static float GlobalMusicVolume
        {
            get => _globalMusicVolume;
            set => _globalMusicVolume = Mathf.Clamp01(value);
        }
        private static float _globalMusicVolume = 1f;

        /// <summary>
        /// Global sounds volume ranging from 0 to 1
        /// </summary>
        public static float GlobalSoundsVolume
        {
            get => _globalSoundsVolume;
            set => _globalSoundsVolume = Mathf.Clamp01(value);
        }
        private static float _globalSoundsVolume = 1f;

        /// <summary>
        /// Global UI sounds volume ranging from 0 to 1
        /// </summary>
        public static float GlobalUISoundsVolume
        {
            get => _globalUISoundsVolume;
            set => _globalUISoundsVolume = Mathf.Clamp01(value);
        }
        private static float _globalUISoundsVolume = 1f;

        #region Oprimize
        private static Queue<AudioSource> _cachedAudioSourceOnGameobject;
        private static Queue<Audio> _cachedAudio;
        #endregion

        private static ListAudio _musicAudio;
        private static ListAudio _soundsAudio;
        private static ListAudio _UISoundsAudio;
        
        private static bool _pausedByLostFocus = false;

        public static EazySoundManager Instance
        {
            get => _instance;
            private set => _instance = value;
        }
        private static EazySoundManager _instance = null;
        private static bool _isInitialized = false;

        private EazySoundManager()
        {
            if (_isInitialized)
            {
                CatchAndProcessingException(new System.Exception("More one singlton."));
            }

            _isInitialized = true;
        }

        static EazySoundManager()
        {
            // If the class is added to the GameObject manually, 
            // Unity throws an implicit content exception that does not indicate a specific problem.
            try
            {
                // If the class has already been added to the object at the time of calling new GameObject(), an exception will be received.
                Instance = new GameObject("EazySoundManager").AddComponent<EazySoundManager>();
            }
            catch (UnityException exception)
            {
                CatchAndProcessingException(exception);
            }

            Instance.Init();
        }

        /// <summary>
        /// Initialized the sound manager
        /// </summary>
        private void Init()
        {
            _musicAudio = new ListAudio(64);
            _soundsAudio = new ListAudio(64);
            _UISoundsAudio = new ListAudio(64);
            _cachedAudioSourceOnGameobject = new Queue<AudioSource>(64);
            _cachedAudio = new Queue<Audio>(64);

            DontDestroyOnLoad(this);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Event triggered when a new scene is loaded
        /// </summary>
        /// <param name="scene">The scene that is loaded</param>
        /// <param name="mode">The scene load mode</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            
            // Stop and remove all non-persistent audio
            RemoveNonPersistAudio(_musicAudio);
            RemoveNonPersistAudio(_soundsAudio);
            RemoveNonPersistAudio(_UISoundsAudio);
            //
            DeleteUnusedAudioSource();
        }

        private void Update()
        {
            if (CanPlay)
            {
                if (_pausedByLostFocus)
                {
                    _pausedByLostFocus = false;
                    UnPauseAll();
                }
                
                UpdateAllAudio(_musicAudio);
                UpdateAllAudio(_soundsAudio);
                UpdateAllAudio(_UISoundsAudio);
            }
            else
            {
                PauseAll();
                _pausedByLostFocus = true;
            }
        }
        
        /// <summary>
        /// Updates the state of all audios of an audio dist
        /// </summary>
        /// <param name="listAudio">The audio dist to update</param>
        private static void UpdateAllAudio(ListAudio listAudio)
        {
            // Go through all audios and update them
            int count = listAudio.Count;
            for (int i = 0; i < count; i++)
            {
                Audio audio = listAudio[i];

                if (audio == null || audio.Paused || !audio.PlayedStart) continue;

                audio.Update();

                // Remove it if it is no longer active (playing)
                if (!audio.IsPlaying)
                {
                    DeleteAudio(audio, listAudio, ref i);
                }
            }
        }

        /// <summary>
        /// Retrieves the audio dist based on the audioType
        /// </summary>
        /// <param name="audioType">The audio type of the dist to return</param>
        /// <returns>An audio dist</returns>
        private static ListAudio GetAudioTypeList(AudioType audioType)
        {
            ListAudio listAudio;

            switch (audioType)
            {
                case AudioType.Music:
                    listAudio = _musicAudio;
                    break;
                case AudioType.Sound:
                    listAudio = _soundsAudio;
                    break;
                case AudioType.UISound:
                    listAudio = _UISoundsAudio;
                    break;
                default:
                    return null;
            }

            return listAudio;
        }
        
        #region Delete and remove Audio
        /// <summary>
        /// Remove all non-persistant audios from an audio dist
        /// </summary>
        /// <param name="listAudio">The audio dist whose non-persistant audios are getting removed</param>
        private static void RemoveNonPersistAudio(ListAudio listAudio)
        {
            // Go through all audios and remove them if they should not persist through scenes
            for (int i = 0; i < listAudio.Count; i++)
            {
                Audio audio = listAudio[i];

                if (audio.Persist && audio.Activated) continue;

                DeleteAudio(audio, listAudio, ref i);
            }
        }

        private static void DeleteAudio(Audio audio, ListAudio listAudio, ref int key)
        {
            if (audio.AudioSource.transform == Gameobject.transform)
            {
                audio.AudioSource.clip = null;
                _cachedAudioSourceOnGameobject.Enqueue(audio.AudioSource);
            }
            else
            {
                //If the AudioSource was not created but was passed as an argument, it will not be deleted
                if (!audio.DeleteAudioSource) return;

                Destroy(audio.AudioSource);
            }

            audio.Delete();
            listAudio[key] = null;
            _cachedAudio.Enqueue(audio);
        }

        /// <summary>
        /// Clear the AudioSource queue and delete the AudioSource if they do not contain a clip.
        /// </summary>
        private static void DeleteUnusedAudioSource()
        {
            while (_cachedAudioSourceOnGameobject.TryDequeue(out AudioSource audioSource))
            {
                if (audioSource.clip != null) continue;

                Destroy(audioSource);
            }
        }
        #endregion

        #region GetAudio Functions
        /// <summary>
        /// Returns the Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the Audio to be retrieved</param>
        /// <returns>Audio that has as its id the audioID, null if no such Audio is found</returns>
        public static Audio GetAudio(AudioType audioType, int audioID)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    return GetMusicAudio(audioID);
                case AudioType.Sound:
                    return GetSoundAudio(audioID);
                case AudioType.UISound:
                    return GetUISoundAudio(audioID);
                default: return null;
            }
        }

        /// <summary>
        /// Returns the all occurrence of Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the Audio to be retrieved</param>
        /// <returns>All occurrence of Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static List<Audio> GetAudio(AudioType audioType, AudioClip audioClip)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    return GetMusicAudio(audioClip);
                case AudioType.Sound:
                    return GetSoundAudio(audioClip);
                case AudioType.UISound:
                    return GetUISoundAudio(audioClip);
                default: return null;
            }
        }

        /// <summary>
        /// Returns the music Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the music Audio to be returned</param>
        /// <returns>Music Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public static Audio GetMusicAudio(int audioID)
        {
            return GetAudioP(AudioType.Music, ref audioID);
        }

        /// <summary>
        /// Returns the all occurrence of music Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the music Audio to be retrieved</param>
        /// <returns>All occurrence of music Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static List<Audio> GetMusicAudio(AudioClip audioClip)
        {
            return GetAudioP(AudioType.Music, audioClip);
        }

        /// <summary>
        /// Returns the sound fx Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the sound fx Audio to be returned</param>
        /// <returns>Sound fx Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public static Audio GetSoundAudio(int audioID)
        {
            return GetAudioP(AudioType.Sound, ref audioID);
        }

        /// <summary>
        /// Returns the all occurrence of sound Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the sound Audio to be retrieved</param>
        /// <returns>All occurrence of sound Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static List<Audio> GetSoundAudio(AudioClip audioClip)
        {
            return GetAudioP(AudioType.Sound, audioClip);
        }

        /// <summary>
        /// Returns the UI sound fx Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the UI sound fx Audio to be returned</param>
        /// <returns>UI sound fx Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public static Audio GetUISoundAudio(int audioID)
        {
            return GetAudioP(AudioType.UISound, ref audioID);
        }

        /// <summary>
        /// Returns the all occurrence of UI sound Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the UI sound Audio to be retrieved</param>
        /// <returns>All occurrence of UI sound Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static List<Audio> GetUISoundAudio(AudioClip audioClip)
        {
            return GetAudioP(AudioType.UISound, audioClip);
        }

        private static Audio GetAudioP(AudioType audioType, ref int audioID)
        {
            ListAudio listAudio = GetAudioTypeList(audioType);

            if (!listAudio.Contains(audioID)) return null;

            return listAudio[audioID];
        }

        private static List<Audio> GetAudioP(AudioType audioType, AudioClip audioClip)
        {
            ListAudio listAudio = GetAudioTypeList(audioType);
            List<Audio> result = new List<Audio>();
            
            int count = listAudio.Count;
            for (int i = 0; i < count; i++)
            {
                Audio audio = listAudio[i];
                
                if (audio != null && audio.Clip == audioClip) result.Add(audio);
            }
            
            return result;
        }
        #endregion

        #region Prepare Function
        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name="persist"> Whether the audio persists in between scene changes</param>
        /// <param name="fadeInValue">How many seconds it needs for the audio to fade in/ reach target volume (if higher than current)</param>
        /// <param name="fadeOutValue"> How many seconds it needs for the audio to fade out/ reach target volume (if lower than current)</param>
        /// <param name="sourceTransform">The transform that is the source of the music (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <param name="audioSource">Specify the AudioSource to play the clips on it. Takes over some settings of the specified AudioSource</param>
        /// <returns>An Audio class that allows you to control the sound being played.</returns>
        public static Audio PrepareMusic(AudioClip clip, float volume = 1f, bool loop = false, bool persist = false,
            float fadeInSeconds = 0f, float fadeOutSeconds = 0f, Transform sourceTransform = null, AudioSource audioSource = null)
        {
            return PrepareAudio(AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, sourceTransform, audioSource);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <param name="sourceTransform">The transform that is the source of the sound (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <param name="audioSource">Specify the AudioSource to play the clips on it. Takes over some settings of the specified AudioSource</param>
        /// <returns>An Audio class that allows you to control the sound being played.</returns>
        public static Audio PrepareSound(AudioClip clip, float volume = 1f, bool loop = false, bool persist = false,
            float fadeInSeconds = 0f, float fadeOutSeconds = 0f, Transform sourceTransform = null, AudioSource audioSource = null)
        {
            return PrepareAudio(AudioType.Sound, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, sourceTransform, audioSource);
        }
        
        /// <summary>
        /// Prepares and initializes a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>An Audio class that allows you to control the sound being played.</returns>
        public static Audio PrepareUISound(AudioClip clip, float volume = 1f)
        {
            return PrepareAudio(AudioType.UISound, clip, volume, false, false, 0f, 0f, null);
        }
        
        private static Audio PrepareAudio(AudioType audioType, AudioClip clip, float volume, bool loop, bool persist,
            float fadeInSeconds, float fadeOutSeconds, Transform sourceTransform)
        {
#if DEBUG
            if (clip == null)
            {
                throw new System.Exception("[Eazy Sound Manager] Audio clip is null");
            }
#endif
            
            bool sourceNull = true;
            ListAudio listAudio = GetAudioTypeList(audioType);
            int id = listAudio.GetFreeIndex();
            sourceTransform = sourceTransform == null ? Gameobject.transform : sourceTransform;
            
            Audio audio = GetAudioClass();
            audio.Init(
                ref id, ref audioType, clip, ref loop, ref persist, ref volume, ref fadeInSeconds, ref fadeOutSeconds,
                sourceTransform, GetAudioSource(sourceTransform, ref sourceNull),
                ref sourceNull);

            // Add it to list
            listAudio[id] = (audio);
            
            return audio;
        }

        private static Audio PrepareAudio(AudioType audioType, AudioClip clip, float volume, bool loop, bool persist,
            float fadeInSeconds, float fadeOutSeconds, Transform sourceTransform, AudioSource audioSource)
        {
            #if DEBUG
            if (clip == null)
            {
                throw new System.Exception("[Eazy Sound Manager] Audio clip is null");
            }
            #endif

            // Left to avoid backward compatibility errors
            bool sourceNull = audioSource == null;
            ListAudio listAudio = GetAudioTypeList(audioType);
            int id = listAudio.GetFreeIndex();
            sourceTransform = sourceTransform == null ? Gameobject.transform : sourceTransform;
            
            Audio audio = GetAudioClass();
            audio.Init(
                ref id, ref audioType, clip, ref loop, ref persist, ref volume, ref fadeInSeconds, ref fadeOutSeconds,
                sourceTransform, sourceNull ? GetAudioSource(sourceTransform, ref sourceNull) : audioSource,
                ref sourceNull);

            // Add it to list
            listAudio[id] = (audio);
            
            return audio;
        }

        private static Audio GetAudioClass()
        {
            Audio audio;
            
            if (!_cachedAudio.TryDequeue(out audio))
            {
                audio = new Audio();
            }

            return audio;
        }

        /// <summary>
        /// If the Transform for playing clips is not set or it is Gameobject.transform, 
        /// then check if the created AudioSource is available and return it, otherwise create a new one and return it.
        /// </summary>
        private static AudioSource GetAudioSource(Transform sourceTransform, ref bool sourceNull)
        {
            if (sourceNull)
            {
                AudioSource audioSource;

                if (!_cachedAudioSourceOnGameobject.TryDequeue(out audioSource))
                {
                    audioSource = Gameobject.AddComponent<AudioSource>();
                }

                return audioSource;
            }
            else
            {
                return sourceTransform.gameObject.AddComponent<AudioSource>();
            }
        }
        #endregion

        #region Play Functions
        /// <summary>
        /// Play background music
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name="persist"> Whether the audio persists in between scene changes</param>
        /// <param name="fadeInSeconds">How many seconds it needs for the audio to fade in/ reach target volume (if higher than current)</param>
        /// <param name="fadeOutSeconds"> How many seconds it needs for the audio to fade out/ reach target volume (if lower than current)</param>
        /// <param name="currentMusicfadeOutSeconds"> How many seconds it needs for current music audio to fade out. It will override its own fade out seconds. If -1 is passed, current music will keep its own fade out seconds</param>
        /// <param name="sourceTransform">The transform that is the source of the music (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <param name="audioSource">Specify the AudioSource to play the clips on it. Takes over some settings of the specified AudioSource</param>
        /// <returns>An Audio class that allows you to control the sound being played.</returns>
        public static Audio PlayMusic(AudioClip clip, float volume = 1f, bool loop = false, bool persist = false,
            float fadeInSeconds = 0f, float fadeOutSeconds = 0f, float currentMusicfadeOutSeconds = 0f, Transform sourceTransform = null, AudioSource audioSource = null)
        {
            return PlayAudio(AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds,
                currentMusicfadeOutSeconds, sourceTransform, audioSource);
        }

        /// <summary>
        /// Play a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <param name="sourceTransform">The transform that is the source of the sound (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <param name="audioSource">Specify the AudioSource to play the clips on it. Takes over some settings of the specified AudioSource</param>
        /// <returns>An Audio class that allows you to control the sound being played.</returns>
        public static Audio PlaySound(AudioClip clip, float volume = 1f, bool loop = false, bool persist = false,
            float fadeInSeconds = 0f, float fadeOutSeconds = 0f, Transform sourceTransform = null, AudioSource audioSource = null)
        {
            return PlayAudio(AudioType.Sound, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, -1f, sourceTransform, audioSource);
        }

        /// <summary>
        /// Play a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>An Audio class that allows you to control the sound being played.</returns>
        public static Audio PlayUISound(AudioClip clip, float volume = 1f)
        {
            return PlayAudio(AudioType.UISound, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        private static Audio PlayAudio(AudioType audioType, AudioClip clip, float volume, bool loop, bool persist,
            float fadeInSeconds, float fadeOutSeconds, float currentMusicfadeOutSeconds, Transform sourceTransform, AudioSource audioSource = null)
        {
            // Stop all current music playing
            if (OnlyOnePlayableMusicClip && audioType == AudioType.Music)
            {
                StopAllMusic(currentMusicfadeOutSeconds);
            }

            Audio audio = PrepareAudio(audioType, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, sourceTransform, audioSource);
            audio.Play();

            return audio;
        }

        #endregion

        #region Stop Functions

        /// <summary>
        /// Stop all audio playing
        /// </summary>
        public static void StopAll()
        {
            StopAll(-1f);
        }

        /// <summary>
        /// Stop all audio playing
        /// </summary>
        /// <param name="musicFadeOutSeconds"> How many seconds it needs for all music audio to fade out. It will override  their own fade out seconds. If -1 is passed, all music will keep their own fade out seconds</param>
        public static void StopAll(float musicFadeOutSeconds)
        {
            StopAllMusic(musicFadeOutSeconds);
            StopAllSounds();
            StopAllUISounds();
        }

        /// <summary>
        /// Stop all music playing
        /// </summary>
        public static void StopAllMusic()
        {
            StopAllAudio(AudioType.Music, -1f);
        }

        /// <summary>
        /// Stop all music playing
        /// </summary>
        /// <param name="fadeOutSeconds"> How many seconds it needs for all music audio to fade out. It will override  their own fade out seconds. If -1 is passed, all music will keep their own fade out seconds</param>
        public static void StopAllMusic(float fadeOutSeconds)
        {
            StopAllAudio(AudioType.Music, fadeOutSeconds);
        }

        /// <summary>
        /// Stop all sound fx playing
        /// </summary>
        public static void StopAllSounds()
        {
            StopAllAudio(AudioType.Sound, -1f);
        }

        /// <summary>
        /// Stop all UI sound fx playing
        /// </summary>
        public static void StopAllUISounds()
        {
            StopAllAudio(AudioType.UISound, -1f);
        }

        private static void StopAllAudio(AudioType audioType, float fadeOutSeconds)
        {
            ListAudio listAudio = GetAudioTypeList(audioType);

            foreach (Audio audio in listAudio)
            {
                if (fadeOutSeconds >= 0) audio.FadeOutSeconds = fadeOutSeconds;

                audio.Stop();
            }
        }

        #endregion

        #region Pause Functions

        /// <summary>
        /// Pause all audio playing
        /// </summary>
        public static void PauseAll()
        {
            PauseAllMusic();
            PauseAllSounds();
            PauseAllUISounds();
        }

        /// <summary>
        /// Pause all music playing
        /// </summary>
        public static void PauseAllMusic()
        {
            PauseAllAudio(AudioType.Music);
        }

        /// <summary>
        /// Pause all sound fx playing
        /// </summary>
        public static void PauseAllSounds()
        {
            PauseAllAudio(AudioType.Sound);
        }

        /// <summary>
        /// Pause all UI sound fx playing
        /// </summary>
        public static void PauseAllUISounds()
        {
            PauseAllAudio(AudioType.UISound);
        }

        private static void PauseAllAudio(AudioType audioType)
        {
            ListAudio listAudio = GetAudioTypeList(audioType);

            foreach (Audio audio in listAudio)
            {
                audio.Pause();
            }
        }

        #endregion

        #region Resume Functions

        /// <summary>
        /// Resume all audio playing
        /// </summary>
        public static void UnPauseAll()
        {
            UnPauseAllMusic();
            UnPauseAllSounds();
            UnPauseAllUISounds();
        }

        /// <summary>
        /// Resume all music playing
        /// </summary>
        public static void UnPauseAllMusic()
        {
            UnPauseAllAudio(AudioType.Music);
        }

        /// <summary>
        /// Resume all sound fx playing
        /// </summary>
        public static void UnPauseAllSounds()
        {
            UnPauseAllAudio(AudioType.Sound);
        }

        /// <summary>
        /// Resume all UI sound fx playing
        /// </summary>
        public static void UnPauseAllUISounds()
        {
            UnPauseAllAudio(AudioType.UISound);
        }

        private static void UnPauseAllAudio(AudioType audioType)
        {
            ListAudio listAudio = GetAudioTypeList(audioType);

            foreach (Audio audio in listAudio) audio.UnPause();
        }
        #endregion

        private static void CatchAndProcessingException(System.Exception exception)
        {
            if (exception.Message.Contains("Internal_CreateGameObject is not allowed"))
            {
                throw new System.Exception("Do not try to assign EazySoundManager to GameObject. " +
                    "Delete all instances of EazySoundManager that are created in such a way as to fix the exception.\n" +
                exception.Message);
            }
            else if (exception.Message.Contains("More one singlton."))
            {
                throw new System.Exception("Do not try to manually create new instances of the " +
                    "Eazy Sound System by adding to the GameObject at runtime. " +
                    "Make sure that no additional instances are created anywhere.\n" +
                    exception.Message);
            }
            else throw exception;
        }
    }
}
