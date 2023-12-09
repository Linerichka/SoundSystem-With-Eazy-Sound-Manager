using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

        /// <summary>
        /// When set to true, new music audios that have the same audio clip as any other music audios, will be ignored
        /// </summary>
        public static bool IgnoreDuplicateMusic = false;

        /// <summary>
        /// When set to true, new sound audios that have the same audio clip as any other sound audios, will be ignored
        /// </summary>
        public static bool IgnoreDuplicateSounds = false;

        /// <summary>
        /// When set to true, new UI sound audios that have the same audio clip as any other UI sound audios, will be ignored
        /// </summary>
        public static bool IgnoreDuplicateUISounds = false;

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

        private static EazySoundManager instance = null;

        private static Dictionary<int, Audio> _musicAudio;
        private static Dictionary<int, Audio> _soundsAudio;
        private static Dictionary<int, Audio> _UISoundsAudio;

        private static Queue<AudioSource> _cachedAudioSourceOnGameobject;

        private static bool initialized = false;

        private static EazySoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (EazySoundManager)FindObjectOfType(typeof(EazySoundManager));
                    if (instance == null)
                    {
                        // Create gameObject and add component
                        instance = new GameObject("EazySoundManager").AddComponent<EazySoundManager>();
                    }
                }
                return instance;
            }
        }

        static EazySoundManager()
        {
            Instance.Init();
        }

        /// <summary>
        /// Initialized the sound manager
        /// </summary>
        private void Init()
        {
            if (initialized) return;

            _musicAudio = new Dictionary<int, Audio>();
            _soundsAudio = new Dictionary<int, Audio>();
            _UISoundsAudio = new Dictionary<int, Audio>();
            _cachedAudioSourceOnGameobject = new Queue<AudioSource>();

            initialized = true;
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
            // Stop and remove all non-persistent audio
            RemoveNonPersistAudio(_musicAudio);
            RemoveNonPersistAudio(_soundsAudio);
            RemoveNonPersistAudio(_UISoundsAudio);
            //
            DeleteUnusedAudioSource();
        }

        private void Update()
        {
            UpdateAllAudio(_musicAudio);
            UpdateAllAudio(_soundsAudio);
            UpdateAllAudio(_UISoundsAudio);
        }

        /// <summary>
        /// Retrieves the audio dictionary based on the audioType
        /// </summary>
        /// <param name="audioType">The audio type of the dictionary to return</param>
        /// <returns>An audio dictionary</returns>
        private static Dictionary<int, Audio> GetAudioTypeDictionary(in Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict;

            switch (audioType)
            {
                case Audio.AudioType.Music:
                    audioDict = _musicAudio;
                    break;
                case Audio.AudioType.Sound:
                    audioDict = _soundsAudio;
                    break;
                case Audio.AudioType.UISound:
                    audioDict = _UISoundsAudio;
                    break;
                default:
                    return null;
            }

            return audioDict;
        }

        /// <summary>
        /// Retrieves the IgnoreDuplicates setting of audios of a specified audio type
        /// </summary>
        /// <param name="audioType">The audio type that the returned IgnoreDuplicates setting affects</param>
        /// <returns>An IgnoreDuplicates setting (bool)</returns>
        private static bool GetAudioTypeIgnoreDuplicateSetting(in Audio.AudioType audioType)
        {
            switch (audioType)
            {
                case Audio.AudioType.Music:
                    return IgnoreDuplicateMusic;
                case Audio.AudioType.Sound:
                    return IgnoreDuplicateSounds;
                case Audio.AudioType.UISound:
                    return IgnoreDuplicateUISounds;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Updates the state of all audios of an audio dictionary
        /// </summary>
        /// <param name="audioDict">The audio dictionary to update</param>
        private static void UpdateAllAudio(Dictionary<int, Audio> audioDict)
        {
            if (!Application.isFocused) return;

            // Go through all audios and update them
            int[] keys = new int[audioDict.Keys.Count];
            audioDict.Keys.CopyTo(keys, 0);

            foreach (int key in keys)
            {
                Audio audio = audioDict[key];

                if (audio.Paused) continue;

                audio.Update();

                // Remove it if it is no longer active (playing)
                if (audio.IsPlaying || audio.Paused) continue;

                DeleteAudio(audio, audioDict, key);
            }
        }

        /// <summary>
        /// Remove all non-persistant audios from an audio dictionary
        /// </summary>
        /// <param name="audioDict">The audio dictionary whose non-persistant audios are getting removed</param>
        private static void RemoveNonPersistAudio(Dictionary<int, Audio> audioDict)
        {
            // Go through all audios and remove them if they should not persist through scenes
            int[] keys = new int[audioDict.Keys.Count];
            audioDict.Keys.CopyTo(keys, 0);

            foreach (int key in keys)
            {
                Audio audio = audioDict[key];

                if (audio.Persist && !audio.Activated) continue;

                DeleteAudio(audio, audioDict, key);
            }
        }

        private static void DeleteAudio(Audio audio, Dictionary<int, Audio> audioDict, in int key)
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
            audioDict.Remove(key);
        }

        private static void DeleteUnusedAudioSource()
        {
            AudioSource[] audioSources = Gameobject.GetComponents<AudioSource>();

            foreach (AudioSource audioSource in audioSources)
            {
                if (audioSource.clip != null) continue;

                Destroy(audioSource);
            }

            _cachedAudioSourceOnGameobject.Clear();
        }
        #region GetAudio Functions

        /// <summary>
        /// Returns the Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the Audio to be retrieved</param>
        /// <returns>Audio that has as its id the audioID, null if no such Audio is found</returns>
        public static Audio GetAudio(in int audioID, in Audio.AudioType audioType)
        {
            switch (audioType)
            {
                case Audio.AudioType.Music:
                    return GetMusicAudio(audioID);
                case Audio.AudioType.Sound:
                    return GetSoundAudio(audioID);
                case Audio.AudioType.UISound:
                    return GetUISoundAudio(audioID);
                default: return null;
            }
        }

        /// <summary>
        /// Returns the first occurrence of Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the Audio to be retrieved</param>
        /// <returns>First occurrence of Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static Audio GetAudio(AudioClip audioClip, in Audio.AudioType audioType)
        {
            switch (audioType)
            {
                case Audio.AudioType.Music:
                    return GetMusicAudio(audioClip);
                case Audio.AudioType.Sound:
                    return GetSoundAudio(audioClip);
                case Audio.AudioType.UISound:
                    return GetUISoundAudio(audioClip);
                    default: return null;
            }
        }

        /// <summary>
        /// Returns the music Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the music Audio to be returned</param>
        /// <returns>Music Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public static Audio GetMusicAudio(in int audioID)
        {
            return GetAudio(Audio.AudioType.Music, audioID);
        }

        /// <summary>
        /// Returns the first occurrence of music Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the music Audio to be retrieved</param>
        /// <returns>First occurrence of music Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static Audio GetMusicAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.Music, audioClip);
        }

        /// <summary>
        /// Returns the sound fx Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the sound fx Audio to be returned</param>
        /// <returns>Sound fx Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public static Audio GetSoundAudio(in int audioID)
        {
            return GetAudio(Audio.AudioType.Sound, audioID);
        }

        /// <summary>
        /// Returns the first occurrence of sound Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the sound Audio to be retrieved</param>
        /// <returns>First occurrence of sound Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static Audio GetSoundAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.Sound, audioClip);
        }

        /// <summary>
        /// Returns the UI sound fx Audio that has as its id the audioID if one is found, returns null if no such Audio is found
        /// </summary>
        /// <param name="audioID">The id of the UI sound fx Audio to be returned</param>
        /// <returns>UI sound fx Audio that has as its id the audioID if one is found, null if no such Audio is found</returns>
        public static Audio GetUISoundAudio(in int audioID)
        {
            return GetAudio(Audio.AudioType.UISound, audioID);
        }

        /// <summary>
        /// Returns the first occurrence of UI sound Audio that plays the given audioClip. Returns null if no such Audio is found
        /// </summary>
        /// <param name="audioClip">The audio clip of the UI sound Audio to be retrieved</param>
        /// <returns>First occurrence of UI sound Audio that has as plays the audioClip, null if no such Audio is found</returns>
        public static Audio GetUISoundAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.UISound, audioClip);
        }

        private static Audio GetAudio(in Audio.AudioType audioType, in int audioID)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            if (!audioDict.ContainsKey(audioID)) return null;

            return audioDict[audioID];
        }

        private static Audio GetAudio(in Audio.AudioType audioType, AudioClip audioClip)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            foreach (Audio audio in audioDict.Values)
            {
				if (audio == null) continue;			

				if (audio.Clip == audioClip) return audio;            
            }

            return null;
        }

        #endregion

        #region Prepare Function

        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareMusic(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, 1f, false, false, 1f, 1f, null);
        }

        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareMusic(AudioClip clip, in float volume)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, false, false, 1f, 1f, null);
        }

        /// <summary>
        /// Prepares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name = "persist" > Whether the audio persists in between scene changes</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareMusic(AudioClip clip, in float volume, in bool loop, in bool persist)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, 1f, 1f, null);
        }

        /// <summary>
        /// Prerpares and initializes background music
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name="persist"> Whether the audio persists in between scene changes</param>
        /// <param name="fadeInValue">How many seconds it needs for the audio to fade in/ reach target volume (if higher than current)</param>
        /// <param name="fadeOutValue"> How many seconds it needs for the audio to fade out/ reach target volume (if lower than current)</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareMusic(AudioClip clip, in float volume, in bool loop, in bool persist,
            in float fadeInSeconds, in float fadeOutSeconds)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, null);
        }

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
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareMusic(AudioClip clip, in float volume, in bool loop, in bool persist, 
            in float fadeInSeconds, in float fadeOutSeconds, Transform sourceTransform)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, sourceTransform);
        }

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
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareMusic(AudioClip clip, in float volume, in bool loop, bool persist, 
            in float fadeInSeconds, in float fadeOutSeconds, Transform sourceTransform, AudioSource audioSource)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, sourceTransform, audioSource);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareSound(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.Sound, clip, 1f, false, false, 0f, 0f, null);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareSound(AudioClip clip, in float volume)
        {
            return PrepareAudio(Audio.AudioType.Sound, clip, volume, false, false, 0f, 0f, null);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareSound(AudioClip clip, in bool loop)
        {
            return PrepareAudio(Audio.AudioType.Sound, clip, 1f, loop, false, 0f, 0f, null);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <param name="sourceTransform">The transform that is the source of the sound (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareSound(AudioClip clip, in float volume, in bool loop, Transform sourceTransform)
        {
            return PrepareAudio(Audio.AudioType.Sound, clip, volume, loop, false, 0f, 0f, sourceTransform);
        }

        /// <summary>
        /// Prepares and initializes a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <param name="sourceTransform">The transform that is the source of the sound (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <param name="audioSource">Specify the AudioSource to play the clips on it. Takes over some settings of the specified AudioSource</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareSound(AudioClip clip, in float volume, in bool loop, Transform sourceTransform, AudioSource audioSource)
        {
            return PrepareAudio(Audio.AudioType.Sound, clip, volume, loop, false, 0f, 0f, sourceTransform, audioSource);
        }

        /// <summary>
        /// Prepares and initializes a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareUISound(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.UISound, clip, 1f, false, false, 0f, 0f, null);
        }

        /// <summary>
        /// Prepares and initializes a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to prepare</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PrepareUISound(AudioClip clip, in float volume)
        {
            return PrepareAudio(Audio.AudioType.UISound, clip, volume, false, false, 0f, 0f, null);
        }

        private static int PrepareAudio(in Audio.AudioType audioType, AudioClip clip, in float volume, in bool loop, in bool persist, 
            in float fadeInSeconds, in float fadeOutSeconds, Transform sourceTransform, AudioSource audioSource = null)
        {
            if (clip == null)
            {
                throw new System.Exception("[Eazy Sound Manager] Audio clip is null");
            }

            if (GetAudioTypeIgnoreDuplicateSetting(audioType))
            {
                Audio duplicateAudio = GetAudio(audioType, clip);

                if (duplicateAudio != null) return duplicateAudio.AudioID;                
            }

            bool sourceNull = audioSource == null;
            // Create the audioSource
            Audio audio = new Audio(
                audioType:audioType, clip, loop, persist, volume, fadeInSeconds, fadeOutSeconds, 
                sourceTransform == null ? Gameobject.transform : sourceTransform,
                sourceNull ? GetAudioSource(sourceTransform) : audioSource,
                sourceNull
                );

            // Add it to dictionary
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);
            audioDict.Add(audio.AudioID, audio);

            return audio.AudioID;
        }

        private static AudioSource GetAudioSource(Transform sourceTransform)
        {
            if (sourceTransform == null || sourceTransform == Gameobject.transform)
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
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayMusic(AudioClip clip)
        {
            return PlayAudio(Audio.AudioType.Music, clip, 1f, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// Play background music
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayMusic(AudioClip clip, in float volume)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// Play background music
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name = "persist" > Whether the audio persists in between scene changes</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayMusic(AudioClip clip, in float volume, in bool loop, in bool persist)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, loop, persist, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// Play background music
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the music is looped</param>
        /// <param name="persist"> Whether the audio persists in between scene changes</param>
        /// <param name="fadeInSeconds">How many seconds it needs for the audio to fade in/ reach target volume (if higher than current)</param>
        /// <param name="fadeOutSeconds"> How many seconds it needs for the audio to fade out/ reach target volume (if lower than current)</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayMusic(AudioClip clip, in float volume, in bool loop, in bool persist, in float fadeInSeconds, in float fadeOutSeconds)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, -1f, null);
        }

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
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayMusic(AudioClip clip, in float volume, in bool loop, in bool persist, 
            in float fadeInSeconds, in float fadeOutSeconds, in float currentMusicfadeOutSeconds, Transform sourceTransform)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform);
        }

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
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayMusic(AudioClip clip, in float volume, in bool loop, in bool persist,
            in float fadeInSeconds, in float fadeOutSeconds, in float currentMusicfadeOutSeconds, Transform sourceTransform, AudioSource audioSource)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, 
                currentMusicfadeOutSeconds, sourceTransform, audioSource);
        }

        /// <summary>
        /// Play a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlaySound(AudioClip clip)
        {
            return PlayAudio(Audio.AudioType.Sound, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Play a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlaySound(AudioClip clip, in float volume)
        {
            return PlayAudio(Audio.AudioType.Sound, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Play a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlaySound(AudioClip clip, in bool loop)
        {
            return PlayAudio(Audio.AudioType.Sound, clip, 1f, loop, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Play a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <param name="sourceTransform">The transform that is the source of the sound (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlaySound(AudioClip clip, in float volume, in bool loop, Transform sourceTransform)
        {
            return PlayAudio(Audio.AudioType.Sound, clip, volume, loop, false, 0f, 0f, -1f, sourceTransform);
        }

        /// <summary>
        /// Play a sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <param name="loop">Wether the sound is looped</param>
        /// <param name="sourceTransform">The transform that is the source of the sound (will become 3D audio). If 3D audio is not wanted, use null</param>
        /// <param name="audioSource">Specify the AudioSource to play the clips on it. Takes over some settings of the specified AudioSource</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlaySound(AudioClip clip, in float volume, in bool loop, Transform sourceTransform, AudioSource audioSource)
        {
            return PlayAudio(Audio.AudioType.Sound, clip, volume, loop, false, 0f, 0f, -1f, sourceTransform, audioSource);
        }

        /// <summary>
        /// Play a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayUISound(AudioClip clip)
        {
            return PlayAudio(Audio.AudioType.UISound, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// Play a UI sound fx
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume"> The volume the music will have</param>
        /// <returns>The ID of the created Audio object</returns>
        public static int PlayUISound(AudioClip clip, in float volume)
        {
            return PlayAudio(Audio.AudioType.UISound, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        private static int PlayAudio(Audio.AudioType audioType, AudioClip clip, in float volume, in bool loop, in bool persist, 
            in float fadeInSeconds, in float fadeOutSeconds, in float currentMusicfadeOutSeconds, Transform sourceTransform, AudioSource audioSource = null)
        {
            // Stop all current music playing
            if (audioType == Audio.AudioType.Music && OnlyOnePlayableMusicClip)
            {
                StopAllMusic(currentMusicfadeOutSeconds);
            }

            int audioID = PrepareAudio(audioType, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, sourceTransform, audioSource);
            GetAudio(audioType, audioID).Play();

            return audioID;
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
        public static void StopAll(in float musicFadeOutSeconds)
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
            StopAllAudio(Audio.AudioType.Music, -1f);
        }

        /// <summary>
        /// Stop all music playing
        /// </summary>
        /// <param name="fadeOutSeconds"> How many seconds it needs for all music audio to fade out. It will override  their own fade out seconds. If -1 is passed, all music will keep their own fade out seconds</param>
        public static void StopAllMusic(in float fadeOutSeconds)
        {
            StopAllAudio(Audio.AudioType.Music, fadeOutSeconds);
        }

        /// <summary>
        /// Stop all sound fx playing
        /// </summary>
        public static void StopAllSounds()
        {
            StopAllAudio(Audio.AudioType.Sound, -1f);
        }

        /// <summary>
        /// Stop all UI sound fx playing
        /// </summary>
        public static void StopAllUISounds()
        {
            StopAllAudio(Audio.AudioType.UISound, -1f);
        }

        private static void StopAllAudio(in Audio.AudioType audioType, in float fadeOutSeconds)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            foreach (Audio audio in audioDict.Values)
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
            PauseAllAudio(Audio.AudioType.Music);
        }

        /// <summary>
        /// Pause all sound fx playing
        /// </summary>
        public static void PauseAllSounds()
        {
            PauseAllAudio(Audio.AudioType.Sound);
        }

        /// <summary>
        /// Pause all UI sound fx playing
        /// </summary>
        public static void PauseAllUISounds()
        {
            PauseAllAudio(Audio.AudioType.UISound);
        }

        private static void PauseAllAudio(in Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            foreach (Audio audio in audioDict.Values)
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
            UnPauseAllAudio(Audio.AudioType.Music);
        }

        /// <summary>
        /// Resume all sound fx playing
        /// </summary>
        public static void UnPauseAllSounds()
        {
            UnPauseAllAudio(Audio.AudioType.Sound);
        }

        /// <summary>
        /// Resume all UI sound fx playing
        /// </summary>
        public static void UnPauseAllUISounds()
        {
            UnPauseAllAudio(Audio.AudioType.UISound);
        }

        private static void UnPauseAllAudio(in Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            foreach (Audio audio in audioDict.Values)
            {
                audio.UnPause();
            }
        }
        #endregion
    }
}
