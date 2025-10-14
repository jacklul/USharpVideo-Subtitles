/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 * 
 * Based on code by Haï~ (https://github.com/hai-vr) - https://gist.github.com/hai-vr/b340f9a46952640f81efe7f02da6bdf6
 */

using JetBrains.Annotations;
using System;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Video.Components.Base;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace UdonSharp.Video.Subtitles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [AddComponentMenu("Udon Sharp/Video/Subtitles/Subtitle Manager")]
    public class SubtitleManager : UdonSharpBehaviour
    {
        private const char ARROW_UP = '▲'; // ▲ ⇪ ↑
        private const char ARROW_DOWN = '▼'; // ▼ ⇩ ↓
        private const string LOG_PREFIX = "[<color=#7ecad6>USharpVideoSubtitles</color>]";

        #region Config

        [Header("References")]

#if USHARPVIDEO_FOUND
        [
            SerializeField,
            InspectorName("USharpVideo Player"),
            Tooltip("This will take priority over BaseVRCVideoPlayer(s)")
        ]
        private USharpVideoPlayer uSharpVideoPlayer;
#endif

        [
            SerializeField,
            InspectorName("Base VRC Video Player(s)"),
            Tooltip("Most video players are using multiple video player components so you will have to assign all of them here")
        ]
        private BaseVRCVideoPlayer[] baseVRCVideoPlayers;

        [
            SerializeField,
            InspectorName("Overlay Handler"),
            Tooltip("Overlay Handler component instance to display subtitles on")
        ]
        private SubtitleOverlayHandler subtitleOverlayHandler;

        [Header("Settings")]

        [
            Range(0, 60),
            PublicAPI,
            Tooltip("How many frames to wait before the next subtitle update - higher values decrease time accuracy of the subtitles but could slightly increase game performance\nThe default is fine for most cases\nSetting this to zero updates every frame")
        ]
        public int updateRate = 10;

        [
            SerializeField,
            InspectorName("Enabled by default"),
            Tooltip("Are subtitles shown by default?")
        ]
        private bool defaultEnabled = true;

        [
            SerializeField,
            InspectorName("Locked by default"),
            Tooltip("Are the controls locked by default?\nThis setting does nothing when using USharpVideo as the lock state is inherited from it")
        ]
        private bool defaultLocked = false;

        [
            SerializeField,
            InspectorName("Local by default"),
            Tooltip("Is local mode enabled by default?")
        ]
        private bool defaultLocal = false;

        [PublicAPI, InspectorName("Default Subtitles URL"), Tooltip("URL to load subtitles from on start")]
        public VRCUrl subtitlesURL;

        [SerializeField, InspectorName("Strings Translation File"), Tooltip("JSON file containing translations for the hardcoded strings\nSee LoadDefaultStrings() function for all string keys")]
        private TextAsset stringsFile;

        [Header("Parser")]

        [
            Range(4.16f, 33.3f),
            PublicAPI,
            InspectorName("Time Limit (ms)"),
            Tooltip("Approximate maximum processing time for the parser to take in miliseconds\nRecommended to keep this under 16.6 as otherwise it will reduce FPS below 60 during parsing (see https://fpstoms.com for more info)")
        ]
        public float parserTimeLimit = 11.1f;

        [
            PublicAPI,
            InspectorName("Filter Subtitle Text"),
            Tooltip("Remove unsupported tags from subtitles\nKeep it enabled unless you will not allow users to use their subtitles and serve pre-filtered subtitles")
        ]
        public bool filterSubtitles = true;

        [Header("Synchronization")]

        [
            SerializeField,
            InspectorName("Enabled"),
            Tooltip("Is synchronization enabled?\nWhen disabled then only local mode will be available")
        ]
        private bool syncEnabled = true;

        [
            SerializeField,
            InspectorName("Chunk size (chars)"),
            Range(5000, 50000),
            Tooltip("Maximum size of a single data chunk when synchronizing the subtitles to others - big chunk sizes can make synchronization fail\nValues above 10000 were not tested")
        ]
        private int syncChunkSize = 10000;

        [
            PublicAPI,
            InspectorName("Resync Cooldown (secs)"),
            Range(1, 60),
            Tooltip("Delay between sync requests made by non-privileged users in seconds")
        ]
        public int syncCooldown = 10;

        [
            PublicAPI,
            InspectorName("Allow URL sync"),
            Tooltip("Sync entered URL to everyone for them to individually fetch the data themselves\nWhen disabled then the URL is fetched by the person who entered it and then the text is synchronized to everyone\nYou should keep it enabled for better VRC networking performance")
        ]
        public bool allowUrlSync = true;

        [Header("Security")]

        [PublicAPI, Tooltip("When checked then the instance creator can always control the subtitles regardless of if they are the master or not")]
        public bool allowInstanceCreatorControl = true;

        [SerializeField, Tooltip("When checked then additional checks are made before transfering ownership of this object\nEnable this to prevent people from taking ownership when they shouldn't be able to")]
        private bool verifyOwnershipRequest = true;

#if USHARPVIDEO_FOUND
        [Header("USharpVideo Integration")]

        [PublicAPI, Tooltip("Clear loaded subtitles when a new video starts?")]
        public bool clearOnNewVideo = false;

        [SerializeField, Tooltip("Force owner of this object to be whoever owns the video player when owner changes?")]
        private bool setToVideoPlayerOwner = false;
#endif

        [Header("Logging")]

        [PublicAPI, Range(0, 4), Tooltip("Logging verbosity\n0 = none, 1 = errors, 2 = warnings, 3 = info (default), 4 = debug")]
        public int logLevel = 3; // 0 = none, 1 = errors, 2 = warnings, 3 = info, 4 = debug

        [PublicAPI, Tooltip("Reference to a custom logger script\nMust implement provided Logger class")]
        public UdonSharp.Video.Subtitles.Logger customLogger;

        #endregion
        #region Variables

        // Synced
        [UdonSynced]
        private int _syncId; // Unique sync ID

        [UdonSynced]
        private double _syncTime; // Last sync time

        [UdonSynced]
        private int _chunkSync; // Current chunk being synced/received

        [UdonSynced]
        private int _chunkCount; // Total number of chunks to sync

        [UdonSynced]
        private string _syncedChunk; // Chunk of data currently being synced

        [UdonSynced]
        private VRCUrl _URLSync = VRCUrl.Empty; // Stores URL for use with synchronization

        [UdonSynced]
        private bool _isLocked = true; // Lock state, unused when USharpVideo is used
        private bool _lastLocked; // Remember last lock state, unused when USharpVideo is used

        //[UdonSynced]
        private float _timeOffset = 0.0f; // Video time offset
        //private float _lastTimeOffset = 0.0f; // Remember last video time offset

        // Variables for storing user input temporarily
        private string _dataTmp = ""; // This stores user's text input, it gets copied to _dataSynced/_dataLocal once the data is parsed and verified to be valid
        private VRCUrl _URLTmp = VRCUrl.Empty; // This stores user's URL input, it gets copied to _URLSync once the data is fetched and verified to be valid

        // Data variables, separated for synced/local modes
        private string _dataSynced = ""; // Stores complete text data for use with synchronization, data received by the clients is concatenated to this variable then parsed
        private string _dataLocal = ""; // Stores subtitles text data when using local mode

        // Parsed data - used to display the subtitles
        private string[] _dataText = new string[0]; // This contains subtitle text
        private Vector2[] _dataTime = new Vector2[0]; // This contains subtitle start and end time
        private int _dataCount = 0; // For use in Update() instead of _dataText.Length

        // Parser related
        private string[] _parserArray = new string[0]; // Stores split text string (each line is one array element)
        private int _parserLine = 0; // Currently processed line (_parserArray index)
        private int _parserIndex = 0; // Current subtitle group index (_dataText)
        private bool _isParsing = false; // If this true and _isParserDone is false then parser is currently working
        private bool _isParserDone = true; // Unfortunately we have to use one extra variable for this because of error handling in _ParserWork to prevent race condition with _ProcessInputWaitForParser

        // Settings
        private bool _isEnabled = true; // Are subtitles shown?
        private bool _isLocal = false; // Is local mode enabled?

        // Translations
        private DataDictionary defaultTranslation;
        private DataDictionary translation;

        // References
#if USHARPVIDEO_FOUND
        private VideoPlayerManager _videoManager = null;
#endif
        private SubtitleControlHandler[] _registeredControlHandlers = new SubtitleControlHandler[0];
        private UdonSharpBehaviour[] _registeredCallbackReceivers = new UdonSharpBehaviour[0];

        // States
        private int _lastSyncId;
        private int _localChunkSync;
        private int _lastUpdateFrame = 0;
        private int _currentDataIndex = 0;
        private float _lastVideoTime = 0;
        private VRCUrl _lastVideoURL = VRCUrl.Empty;
        private VRCPlayerApi _currentOwner;
        private VRCPlayerApi _previousOwner;

        #endregion
        #region Initialization

#if USHARPVIDEO_FOUND
        private void OnEnable()
        {
            if (uSharpVideoPlayer) uSharpVideoPlayer.RegisterCallbackReceiver(this);
        }
#endif

        private void Start()
        {
#if USHARPVIDEO_FOUND
            if (!uSharpVideoPlayer && baseVRCVideoPlayers.Length == 0)
                LogWarning("No video player reference assigned!");
#else
            if (baseVRCVideoPlayers.Length == 0)
                LogWarning("No video player reference assigned!");
#endif

            LoadDefaultStrings();

            if (stringsFile)
                LoadStringsFile(stringsFile);

            _isEnabled = defaultEnabled;
            _isLocked = _lastLocked = defaultLocked;
            _isLocal = defaultLocal;

            if (!syncEnabled)
                _isLocal = true;

            _previousOwner = Networking.GetOwner(gameObject);
            _currentOwner = _previousOwner;

#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
            {
                _videoManager = uSharpVideoPlayer.GetVideoManager();

                if (Networking.IsOwner(gameObject))
                    // This event will be initially triggered only for non-master players, this fixes the issue with wrong lock state on master player
                    SendCustomEventDelayedFrames(nameof(OnUSharpVideoLockChange), 1);
            }
            else
            {
#endif
                if (Networking.IsOwner(gameObject))
                    SendCustomEventDelayedFrames(nameof(_QueueSerialize), 1); // Send lock state to everyone as initial state

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.UpdateLockState();
#if USHARPVIDEO_FOUND
            }
#endif

            ResetSubtitleTrackingState();

            Log("Initialized");

            if (subtitlesURL.ToString() != "" && Networking.IsMaster)
            {
                Log($"Loading subtitles from configured URL: {subtitlesURL}");
                FetchFromURL(subtitlesURL);
            }
        }

#if USHARPVIDEO_FOUND
        private void OnDisable()
        {
            if (uSharpVideoPlayer) uSharpVideoPlayer.UnregisterCallbackReceiver(this);
        }
#endif

        [PublicAPI]
        public void RegisterControlHandler(SubtitleControlHandler newControlHandler)
        {
            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new SubtitleControlHandler[0];

            foreach (SubtitleControlHandler controlHandler in _registeredControlHandlers)
            {
                if (newControlHandler == controlHandler)
                    return;
            }

            SubtitleControlHandler[] newControlHandlers = new SubtitleControlHandler[_registeredControlHandlers.Length + 1];
            _registeredControlHandlers.CopyTo(newControlHandlers, 0);
            _registeredControlHandlers = newControlHandlers;

            _registeredControlHandlers[_registeredControlHandlers.Length - 1] = newControlHandler;

            newControlHandler.SetStatusText(_dataCount > 0 ? GetString("LOADED") : GetString("NOT_LOADED"));
        }

        [PublicAPI]
        public void UnregisterControlHandler(SubtitleControlHandler controlHandler)
        {
            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new SubtitleControlHandler[0];

            int controlHandlerCount = _registeredControlHandlers.Length;
            for (int i = 0; i < controlHandlerCount; ++i)
            {
                SubtitleControlHandler handler = _registeredControlHandlers[i];

                if (controlHandler == handler)
                {
                    SubtitleControlHandler[] newControlHandlers = new SubtitleControlHandler[controlHandlerCount - 1];

                    for (int j = 0; j < i; ++j)
                        newControlHandlers[j] = _registeredControlHandlers[j];

                    for (int j = i + 1; j < controlHandlerCount; ++j)
                        newControlHandlers[j - 1] = _registeredControlHandlers[j];

                    _registeredControlHandlers = newControlHandlers;

                    return;
                }
            }
        }

        #endregion
        #region Logging

        public void LogError(string message, UnityEngine.Object context = null)
        {
            if (logLevel < 1)
                return;

            if (context == null)
                context = this;

            if (customLogger)
                customLogger.LogError(LOG_PREFIX + " " + message, context);
            else
                Debug.LogError(LOG_PREFIX + " " + message, context);
        }

        public void LogWarning(string message, UnityEngine.Object context = null)
        {
            if (logLevel < 2)
                return;

            if (context == null)
                context = this;

            if (customLogger)
                customLogger.LogWarning(LOG_PREFIX + " " + message, context);
            else
                Debug.LogWarning(LOG_PREFIX + " " + message, context);
        }

        public void Log(string message, UnityEngine.Object context = null)
        {
            if (logLevel < 3)
                return;

            if (context == null)
                context = this;

            if (customLogger)
                customLogger.Log(LOG_PREFIX + " " + message, context);
            else
                Debug.Log(LOG_PREFIX + " " + message, context);
        }

        public void LogDebug(string message, UnityEngine.Object context = null)
        {
            if (logLevel < 4)
                return;

            if (context == null)
                context = this;

            if (customLogger)
                customLogger.Log(LOG_PREFIX + " " + message, context);
            else
                Debug.Log(LOG_PREFIX + " " + message, context);
        }

        #endregion
        #region Translation

        private void LoadDefaultStrings()
        {
            defaultTranslation = new DataDictionary();
            defaultTranslation.SetValue("LOADED", "Subtitles loaded");
            defaultTranslation.SetValue("NOT_LOADED", "No subtitles loaded");
            defaultTranslation.SetValue("CLEARED", "Subtitles cleared");
            defaultTranslation.SetValue("FETCHING", "Fetching from URL...");
            defaultTranslation.SetValue("FETCH_FAILED", "Failed to fetch from URL");
            defaultTranslation.SetValue("PARSING", "Parsing... {0}%");
            defaultTranslation.SetValue("PARSE_FAILED", "Failed to parse subtitles");
            defaultTranslation.SetValue("SYNCING", "Sync {0} / {1} {2}");
            //defaultTranslation.SetValue("SYNC_RUNNING", "Wait for sync to finish");
            //defaultTranslation.SetValue("SYNC_COOLDOWN", "Wait for sync cooldown");
            //defaultTranslation.SetValue("PLACEHOLDER_PASTE", "Paste SRT subtitles...");
            //defaultTranslation.SetValue("PLACEHOLDER_URL", "Paste URL to SRT subtitles...");
            //defaultTranslation.SetValue("ONLY_MASTER_CAN_ADD", "Only master {0} can add subtitles");
            //defaultTranslation.SetValue("ONLY_OWNER_CAN_SYNC", "Only {0} can sync subtitles");
            defaultTranslation.SetValue("INDICATOR_ANYONE", "(anyone)");
            defaultTranslation.SetValue("INDICATOR_MASTER", "(master)");
            defaultTranslation.SetValue("INDICATOR_LOCAL", "(local)");
            //defaultTranslation.SetValue("ALIGNMENT_BOTTOM", "Bottom");
            //defaultTranslation.SetValue("ALIGNMENT_TOP", "Top");
        }

        [PublicAPI]
        public void LoadStringsFile(TextAsset file = null)
        {
            translation = null;

            if (file != null)
            {
                if (VRCJson.TryDeserializeFromJson(file.text, out DataToken result))
                {
                    if (result.TokenType == TokenType.DataDictionary)
                    {
                        translation = result.DataDictionary;
                        LogDebug("Translation file loaded");
                    }
                    else
                        LogError("Translation file is not a valid dictionary object");
                }
                else
                    LogError("Failed to parse translation file");
            }
            else
                LogDebug("No translation file provided, switching to built-in messages");
        }

        public string GetString(string key, params object[] args)
        {
            DataToken value = new DataToken();

            if (translation != null && translation.Count > 0 && translation.ContainsKey(key))
                translation.TryGetValue(key, out value);

            if (value.IsNull && defaultTranslation.ContainsKey(key))
                defaultTranslation.TryGetValue(key, out value);

            if (value.TokenType == TokenType.String)
                return args.Length > 0 ? string.Format(value.ToString(), args) : value.ToString();

            LogDebug($"Translation key '{key}' not found");
            return key;
        }

        #endregion
        #region Ownership

        [PublicAPI]
        public bool IsPrivilegedUser(VRCPlayerApi player)
        {
#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
                return uSharpVideoPlayer.IsPrivilegedUser(player);
#endif

            return player.isMaster || (allowInstanceCreatorControl && player.isInstanceOwner);
        }

        [PublicAPI]
        public bool CanControlSubtitles()
        {
#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
                return uSharpVideoPlayer.CanControlVideoPlayer();
#endif

            return !_isLocked || IsPrivilegedUser(Networking.LocalPlayer);
        }

        private void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject))
                return;

            if (CanControlSubtitles())
            {
                LogDebug("Taking ownership");

                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        #endregion
        #region Subtitles handling

        public void Update()
        {
            if (!_isEnabled || _dataCount == 0) // Do nothing when hidden or no data is loaded
                return;

            if (updateRate > 0)
            {
                if (_lastUpdateFrame < updateRate)
                {
                    _lastUpdateFrame++;
                    return;
                }

                _lastUpdateFrame = 0;
            }

            if (IsVideoPlayerPlaying()) // Don't update subtitles if the video is not playing
            {
                float time = GetVideoTime();

                if (time == _lastVideoTime)
                    return;

                if (time < _lastVideoTime)
                    ResetSubtitleTrackingState();

                _lastVideoTime = time;
                string text = "";

                for (int i = _currentDataIndex; i < _dataCount; i++)
                {
                    if (time >= _dataTime[i].x && time <= _dataTime[i].y) // Subtitle display time matches current time
                    {
                        if (text != "")
                            text += "\n" + _dataText[i]; // Support overlapping subtitles
                        else
                            text = _dataText[i];
                    }
                    else if (time > _dataTime[_currentDataIndex].y) // Currently tracked subtitle is no longer to be shown
                        _currentDataIndex++;
                    else
                        break;
                }

                if (subtitleOverlayHandler) subtitleOverlayHandler.DisplaySubtitle(text);
            }
        }

        private bool IsVideoPlayerPlaying()
        {
#if USHARPVIDEO_FOUND
            if (_videoManager)
                return _videoManager.IsPlaying();
#endif

            foreach (BaseVRCVideoPlayer baseVRCVideoPlayer in baseVRCVideoPlayers)
                if (baseVRCVideoPlayer && baseVRCVideoPlayer.IsPlaying)
                    return true;

            return false;
        }

        private float GetVideoTime()
        {
            float time = 0.0f;

#if USHARPVIDEO_FOUND
            if (_videoManager)
                time = _videoManager.GetTime();
            else
#endif
                foreach (BaseVRCVideoPlayer baseVRCVideoPlayer in baseVRCVideoPlayers)
                    if (baseVRCVideoPlayer && baseVRCVideoPlayer.IsPlaying)
                    {
                        time = baseVRCVideoPlayer.GetTime();
                        break;
                    }

            if (_timeOffset != 0.0f)
            {
                time += _timeOffset;

                if (time < 0.0f)
                    time = 0.0f;
            }

            return time;
        }

        private void LoadSubtitles(string subtitles, bool closeInputMenu)
        {
            if (subtitles != "")
                InitializeParser(subtitles);
            else
                LogError("Requested to load empty data - this shouldn't happen");

            if (closeInputMenu)
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler._CloseInputMenu();
        }

        private void ResetSubtitleTrackingState()
        {
            _currentDataIndex = 0;
            _lastVideoTime = 0;

            if (subtitleOverlayHandler)
            {
                subtitleOverlayHandler.ClearSubtitle();
                subtitleOverlayHandler.SetPlaceholder(false);
                LogDebug("Subtitles tracking state reset");
            }
        }

        private void ClearSubtitlesInternal()
        {
            if (_dataCount > 0) Log("Clearing subtitles");

            _dataText = new string[0];
            _dataTime = new Vector2[0];
            _dataCount = 0;

            ResetSubtitleTrackingState();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetString("CLEARED"));

            SendCallback("OnUSharpVideoSubtitlesClear");
        }

        private void UnsetSubtitlesInternal()
        {
            ClearSubtitlesInternal();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetString("NOT_LOADED"));
        }

        #endregion
        #region Synchronization

        private void SetAndTransmitSubtitles(string text)
        {
            if (!CanControlSubtitles() || !syncEnabled)
                return;

            _dataSynced = text; // Must be set no matter what for the local toggle to work correctly

            if (allowUrlSync && _URLTmp != VRCUrl.Empty && text != "") // Make sure to also handle clear button by checking for empty text
                _URLSync = _URLTmp;
            else
                _URLSync = VRCUrl.Empty;

            _syncId = Networking.GetServerTimeInMilliseconds();
            _lastSyncId = _syncId;

            TakeOwnership();
            TransmitSubtitles();
        }

        private void TransmitSubtitles()
        {
            if (!Networking.IsOwner(gameObject) || !syncEnabled)
                return;

            if (VRCPlayerApi.GetPlayerCount() == 1) // No point to even attempt to synchronize when alone in the instance
                return;

            if (_URLSync.ToString().Length > 0)
            {
                Log($"Transmitting URL: {_URLSync}");

                _chunkCount = 1;
                _chunkSync = 0;
            }
            else
            {
                Log($"Transmitting text (length = {_dataSynced.Length})");

                _chunkCount = _dataSynced.Length / syncChunkSize + 1;
                _chunkSync = 0;
            }

            if (!_isLocal) // Do not touch the UI if owner is in local mode
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                {
                    handler._SaveStatusText(); // Does not save anything if we already have something saved
                    handler.SetStatusText(GetString("SYNCING", 0, _chunkCount, ARROW_UP));
                }
            }

            RequestSerialization();
            SendCallback("OnUSharpVideoSubtitlesTransmitStart");
        }

        public void _QueueSerialize()
        {
            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            if (syncEnabled && _chunkSync < _chunkCount) // Makes sure this doesn't run while syncing just the lock state
            {
                LogDebug($"About to send chunk {_chunkSync + 1} / {_chunkCount} ({_syncId})");

                if (_URLSync.ToString().Length > 0)
                {
                    _syncedChunk = "";
                    return;
                }

                int start = Mathf.Min(_chunkSync * syncChunkSize, _dataSynced.Length);
                int length = Mathf.Min(syncChunkSize, _dataSynced.Length - _chunkSync * syncChunkSize);

                if (length < 0) // This can happen when master presses the lock button at the same time as someone else is starting to send the subtitles, this will also bug out UI for both players (toggling local mode cleans up the UI)
                    length = syncChunkSize;

                _syncedChunk = _dataSynced.Substring(start, length);
                _syncTime = Networking.GetServerTimeInSeconds();
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                LogError("Failed to serialize data, retrying in 1 second...");

                SendCustomEventDelayedSeconds(nameof(_QueueSerialize), 1f);
                return;
            }

            if (syncEnabled && _chunkSync < _chunkCount)
            {
                if (!_isLocal) // Do not touch the UI if owner is in local mode
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(GetString("SYNCING", _chunkSync + 1, _chunkCount, ARROW_UP));
                }

                Log($"Sent chunk {_chunkSync + 1} / {_chunkCount} ({_syncId})");

                _chunkSync++;

                if (_chunkSync < _chunkCount)
                {
                    LogDebug("Will send another chunk...");

                    SendCustomEventDelayedFrames(nameof(_QueueSerialize), 1);
                    SendCallback("OnUSharpVideoSubtitlesTransmitProgress");
                }
                else
                {
                    LogDebug($"Sent all chunks");

                    if (!_isLocal) // Do not touch the UI if owner is in local mode
                    {
                        foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                            handler._RestoreStatusText();
                    }

                    SendCallback("OnUSharpVideoSubtitlesTransmitFinish");
                }
            }
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject)) // Ignore own updates
                return;

#if USHARPVIDEO_FOUND
            if (!uSharpVideoPlayer && _lastLocked != _isLocked)
#else
            if (_lastLocked != _isLocked)
#endif
            {
                _lastLocked = _isLocked;

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.UpdateLockState();

                SendCallback("OnUSharpVideoSubtitlesLockChange");
            }

            /*if (_timeOffset != _lastTimeOffset)
            {
                _lastTimeOffset = _timeOffset;

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.UpdateTimeOffset();

                SendCallback("OnUSharpVideoSubtitlesTimeOffsetChange");
            }*/

            // Synced just the lock state, video time offset, received resync data or sync is disabled
            if (IsSynchronized() || !syncEnabled)
                return;

            if (IsSameSyncId())
            {
                LogDebug($"Not loading chunk {_chunkSync + 1} / {_chunkCount} because it has the same identifier ({_syncId}) as the previously loaded one ({_lastSyncId})");
                return;
            }

            if (!_isLocal)
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetStatusText(GetString("SYNCING", _chunkSync + 1, _chunkCount, ARROW_DOWN));
            }

            Log($"Received chunk {_chunkSync + 1} / {_chunkCount} ({_syncId})");

            if (_chunkSync == 0)
            {
                _localChunkSync = 0;
                _dataSynced = _syncedChunk;
            }
            else if (_localChunkSync == _chunkSync - 1)
            {
                _localChunkSync++;
                _dataSynced += _syncedChunk;
            }
            else
                LogWarning($"Rejected chunk {_chunkSync + 1} because local chunk is {_localChunkSync}");

            if (_localChunkSync == _chunkCount - 1)
            {
                _lastSyncId = _syncId;

                LogDebug($"Received all chunks");

                if (!_isLocal)
                {
                    if (_URLSync.ToString().Length > 0)
                    {
                        Log($"Using synchronized URL: {_URLSync}");

                        FetchFromURL(_URLSync);
                    }
                    else
                    {
                        if (_dataSynced.Length == 0)
                        {
                            ClearSubtitlesInternal();
                            return;
                        }

                        Log($"Using synchronized text (length = {_dataSynced.Length})");

                        LoadSubtitles(_dataSynced, false);
                    }

                    ResetSubtitleTrackingState();
                }
            }
        }

        private bool IsSameSyncId()
        {
            if (!syncEnabled)
                return false;

            return _lastSyncId == _syncId;
        }

        [PublicAPI]
        public bool IsSynchronized()
        {
            if (!syncEnabled)
                return false;

            return _chunkSync == _chunkCount && IsSameSyncId();
        }

        [PublicAPI]
        public bool IsSyncedURL()
        {
            return _URLSync.ToString().Length > 0;
        }

        #endregion
        #region Parser

        private void InitializeParser(string text)
        {
            ResetParser();

            int len = text.Split(new string[] { " --> " }, StringSplitOptions.None).Length - 1;

            if (len <= 0)
            {
                LogDebug($"Could not check total subtitle count, file is invalid?");

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetStatusText(GetString("PARSE_FAILED"));

                SendCallback("OnUSharpVideoSubtitlesParseError");

                return;
            }

            LogDebug($"Detected {len} subtitle groups");

            _parserArray = (text + "\n").Replace("\r\n", "\n").Split('\n'); // We are adding empty line at the end to make sure the parser reaches final state
            _dataText = new string[len];
            _dataTime = new Vector2[len];
            _dataCount = 0;
            _isParsing = true;
            _isParserDone = false;

            SendCustomEventDelayedFrames(nameof(_ParserWork), 0);
        }

        private void ResetParser()
        {
            _isParsing = false;
            _parserArray = new string[0];
            _parserLine = 0;
            _parserIndex = 0;
        }

        public void _ParserWork()
        {
            float startTime = Time.realtimeSinceStartup;
            float timeLimit = parserTimeLimit / 1000.0f; // Convert from ms to seconds (float)

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetString("PARSING", (int)Math.Round((double)(100 * _parserIndex) / _dataText.Length)));

            int parserState = 0;
            for (int i = _parserLine; i < _parserArray.Length; i++)
            {
                string line = _parserArray[i];

                if (parserState == 0 && line.Contains(" --> "))
                {
                    string[] times = line.Split(new string[] { " --> " }, StringSplitOptions.None);

                    if (times[1].Contains(" ")) // Per SRT specs there can be text coordinates after the timestamp and we can't support that
                        times[1] = times[1].Split(' ')[0];

                    if (_parserIndex > _dataText.Length - 1) // Prevent a crash when exceeding the max index
                    {
                        LogError($"Ran out of space in data array ({_parserIndex})");

                        _isParsing = false;
                        break;
                    }

                    _dataText[_parserIndex] = "";
                    _dataTime[_parserIndex] = new Vector2(ParseTimestamp(times[0]), ParseTimestamp(times[1]));

                    parserState = 1;
                }
                else if (parserState == 1 && line != "")
                {
                    _dataText[_parserIndex] = ProcessSubtitleText(line);
                    parserState = 2;
                }
                else if (parserState == 2 && line != "")
                {
                    _dataText[_parserIndex] += "\n" + ProcessSubtitleText(line);
                }
                else if (parserState != 0 && (line == "" || i == _parserArray.Length - 1))
                {
                    _parserLine = i;
                    _parserIndex++;
                    parserState = 0;
                }

                if (parserState == 0 && Time.realtimeSinceStartup > startTime + timeLimit)
                    break;

                // This is necessary to prevent infinite loop
                if (i >= _parserArray.Length - 1)
                {
                    _parserLine = i;
                    break;
                }
            }

            if (_parserLine >= _parserArray.Length - 1)
            {
                int groups = _parserIndex;
                ResetParser();

                LogDebug($"Parsed {groups} subtitle groups");
            }

            if (!_isParsing)
            {
                if (_dataText.Length > 0)
                {
                    _dataCount = _dataText.Length;

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(GetString("LOADED"));

                    SendCallback("OnUSharpVideoSubtitlesLoad");
                }
                else
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(GetString("PARSE_FAILED"));

                    SendCallback("OnUSharpVideoSubtitlesParseError");
                }

                _isParserDone = true;
            }
            else
                SendCustomEventDelayedFrames(nameof(_ParserWork), 0);
        }

        private float ParseTimestamp(string timestamp)
        {
            string[] allParts = timestamp.Split(':');

            if (allParts.Length != 3)
                return 0;

            string[] secondsPart = allParts[2].Replace('.', ',').Split(','); // Sometimes instead of comma we have a dot, this change also makes VTT files parsable
            float milliseconds = secondsPart.Length == 0 ? 0f : (int.Parse(secondsPart[1]) * 0.001f);

            return int.Parse(allParts[0]) * 3600 + int.Parse(allParts[1]) * 60 + int.Parse(secondsPart[0]) + milliseconds;
        }

        private string ProcessSubtitleText(string text)
        {
            text = text.Replace("\\n", "\n").Replace("\\N", "\n");

            if (filterSubtitles)
                text = FilterSubtitle(text);

            return text;
        }

        private string FilterSubtitle(string text) // This function removes unsupported HTML tags and all ASS tags
        {
            char[] allowedShortHTMLTags = { 'b', 'i', 'u' };

            // Replace {x} with <x> for allowed tags
            foreach (char tag in allowedShortHTMLTags)
                text = text.Replace("{" + tag + "}", "<" + tag + ">").Replace("{/" + tag + "}", "</" + tag + ">");

            string result = "";
            char tagEnd = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                if (tagEnd != '\0')
                {
                    // Skip contents until the end of tag
                    if (text[i] == tagEnd)
                        tagEnd = '\0';

                    continue;
                }

                if (i + 1 < text.Length)
                {
                    if (text[i] == '{' && text[i + 1] == '\\') // Start of ASS tag
                    {
                        tagEnd = '}';
                        continue;
                    }

                    if (text[i] == '<') // Start of HTML tag
                    {
                        bool isEndingTag = text[i + 1] == '/';
                        bool isShortTag = false;
                        char shortTagValue = '\0';

                        if (i + 3 < text.Length)
                            isShortTag = isEndingTag ? text[i + 3] == '>' : text[i + 2] == '>';

                        if (i + 2 < text.Length)
                            shortTagValue = isShortTag ? (isEndingTag ? text[i + 2] : text[i + 1]) : ' ';

                        if (!isShortTag || Array.IndexOf(allowedShortHTMLTags, shortTagValue) == -1)
                        {
                            tagEnd = '>';
                            continue;
                        }
                    }
                }

                result += text[i];
            }

            return result;
        }

        #endregion
        #region Input

        [PublicAPI]
        public void ProcessInput(string input)
        {
            if (!_isLocal && !CanControlSubtitles())
                return;

            if (input == string.Empty)
            {
                LogDebug("String input is empty");
                return;
            }

            Log($"Loaded string input (length = {input.Length})");

            LoadSubtitles(input, true);

            _dataTmp = input;

            SendCustomEventDelayedFrames(nameof(_ProcessInputWaitForParser), 1);
        }

        public void _ProcessInputWaitForParser()
        {
            if (!_isParserDone)
            {
                SendCustomEventDelayedFrames(nameof(_ProcessInputWaitForParser), 10);
                return;
            }

            if (_dataCount > 0)
            {
                if (!_isLocal)
                    SetAndTransmitSubtitles(_dataTmp); // Synchronize to others only if the input is valid
                else
                    _dataLocal = _dataTmp; // Set the data to local variable

                // Clear input data
                _dataTmp = "";
                _URLTmp = VRCUrl.Empty;

                ResetSubtitleTrackingState();
            }
        }

        [PublicAPI]
        public void ProcessURLInput(VRCUrl url)
        {
            if (!_isLocal && !CanControlSubtitles())
                return;

            if (url == VRCUrl.Empty)
            {
                LogDebug("URL input is empty");
                return;
            }

            Log($"Loaded URL input: {url}");

            _URLTmp = url;

            FetchFromURL(url);
        }

        private void FetchFromURL(VRCUrl url)
        {
            LogDebug($"Loading text from URL: {url}");

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetString("FETCHING"));

            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            Log($"Remote string load success ({BitConverter.ToInt32(result.ResultBytes, 0)} bytes)");

            if (result.Url == _URLTmp) // User entered the URL - to be synchronized
                ProcessInput(result.Result);
            else // User received URL - to be loaded
            {
                _dataSynced = result.Result; // Prevent re-fetching from URL when user toggles local mode

                LoadSubtitles(result.Result, false);
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _URLTmp = VRCUrl.Empty;

            LogError($"Remote string load failure: {result.Error}");

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetString("FETCH_FAILED"));

            SendCallback("OnUSharpVideoSubtitlesFetchError");
        }

        #endregion
        #region API

        [PublicAPI]
        public bool HasSubtitles()
        {
            return _dataCount > 0;
        }

        [PublicAPI]
        public bool IsLocked()
        {
#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
                return uSharpVideoPlayer.IsLocked();
#endif

            return _isLocked;
        }

        [PublicAPI]
        public void SetLocked(bool state)
        {
            if (!IsPrivilegedUser(Networking.LocalPlayer))
                return;

#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
            {
                LogError("Method SetLocked cannot be used while using USharpVideo");
                return;
            }
#endif

            // Stop when someone else is running the sync as this would break it for everyone
            if (!IsSynchronized() && !Networking.IsOwner(gameObject)) 
                return;

            _isLocked = state;
            _lastLocked = _isLocked;

            if (IsSynchronized()) // We don't have to call this when we are sending the data already
            {
                TakeOwnership();
                _QueueSerialize();
            }

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateLockState();

            LogDebug($"Locked state = {(_isLocked ? "ON" : "OFF")}");

            SendCallback("OnUSharpVideoSubtitlesLockChange");
        }

        [PublicAPI]
        public bool IsEnabled()
        {
            return _isEnabled;
        }

        [PublicAPI]
        public void SetEnabled(bool state)
        {
            if (_isEnabled == state)
                return;

            _isEnabled = state;

            if (!state)
                if (subtitleOverlayHandler) subtitleOverlayHandler.ClearSubtitle();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateValues();

            SendCallback("OnUSharpVideoSubtitlesEnabledChange");
            LogDebug($"Enabled state = {(_isEnabled ? "ON" : "OFF")}");
        }

        [PublicAPI]
        public bool IsLocal()
        {
            return _isLocal;
        }

        [PublicAPI]
        public void SetLocal(bool state)
        {
            if (!syncEnabled && !state)
                return;

            if (_isLocal == state)
                return;

            _isLocal = state;

            if (state)
            {
                if (_dataLocal != "")
                    LoadSubtitles(_dataLocal, false);
                else
                    UnsetSubtitlesInternal();
            }
            else
            {
                if (IsSynchronized())
                {
                    if (_dataSynced != "")
                        LoadSubtitles(_dataSynced, false);
                    else if (_URLSync.ToString().Length > 0)
                        FetchFromURL(_URLSync);
                    else
                        UnsetSubtitlesInternal();
                }
                else
                    UnsetSubtitlesInternal();
            }

            ResetSubtitleTrackingState();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateValues();

            SendCallback("OnUSharpVideoSubtitlesModeChange");
            LogDebug($"Local state = {(_isLocal ? "ON" : "OFF")}");
        }

        [PublicAPI]
        public float GetTimeOffset()
        {
            return _timeOffset;
        }

        [PublicAPI]
        public void SetTimeOffset(float offset)
        {
            if (_timeOffset == -offset)
                return;

            _timeOffset = -offset;
            //_lastTimeOffset = _timeOffset;

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateValues();

            SendCallback("OnUSharpVideoSubtitlesTimeOffsetChange");
            LogDebug($"Time offset = {offset}");
        }

        /*[PublicAPI]
        public void SyncTimeOffset()
        {
            if (!syncEnabled)
                return;

            if (CanControlSubtitles() && (IsSynchronized() || Networking.IsOwner(gameObject)))
            {
                if (IsSynchronized()) // We don't have to call this when we are sending the data already
                {
                    TakeOwnership();
                    _QueueSerialize();
                }
            }
        }*/

        [PublicAPI]
        public void SetVideoPlayers(BaseVRCVideoPlayer[] videoPlayers)
        {
#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
            {
                LogWarning("Method SetVideoPlayers cannot be used with USharpVideo");
                return;
            }
#endif

            baseVRCVideoPlayers = videoPlayers;
            ResetSubtitleTrackingState();

            SendCallback("OnUSharpVideoSubtitlesVideoPlayerChange");
        }

        [PublicAPI, Obsolete("Use SetVideoPlayers() instead")]
        public void SetVideoPlayer(BaseVRCVideoPlayer videoPlayer)
        {
            LogWarning("Method SetVideoPlayer() is deprecated, use SetVideoPlayers() instead");
            SetVideoPlayers(new BaseVRCVideoPlayer[] { videoPlayer });
        }

        [PublicAPI]
        public void SetOverlayHandler(SubtitleOverlayHandler overlayHandler)
        {
            if (subtitleOverlayHandler)
                subtitleOverlayHandler.ClearSubtitle();

            subtitleOverlayHandler = overlayHandler;
            overlayHandler.ClearSubtitle();

            SendCallback("OnUSharpVideoSubtitlesOverlayChange");
        }

        [PublicAPI]
        public void ClearSubtitles()
        {
            if (!_isLocal)
            {
                if (!CanControlSubtitles())
                    return;

                // Prevent "subtitles loaded" status to be set after clearing when synchronization is still running
                if (!IsSynchronized())
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler._RestoreStatusText();

                ClearSubtitlesInternal(); // Must be called first otherwise _RestoreStatusText() in OnPostSerialization will send previous status instead of cleared message
                SetAndTransmitSubtitles("");
            }
            else
            {
                _dataLocal = "";
                ClearSubtitlesInternal();
            }
        }

        [PublicAPI]
        public bool CanSynchronizeSubtitles()
        {
            if (!IsSynchronized())
                return false;

            return Networking.IsOwner(gameObject) || CanControlSubtitles();
            //return Networking.IsOwner(gameObject) || (IsSynchronized() && IsPrivilegedUser(Networking.LocalPlayer));
        }

        [PublicAPI]
        public void SynchronizeSubtitles()
        {
            // Owner is guaranteed to have the subtitles and master should always be able to resync if synchronized
            if (CanSynchronizeSubtitles())
            {
                LogDebug("Reload (owner/master)");

                if (_dataSynced != "") // Sanity check
                {
                    TakeOwnership();
                    TransmitSubtitles();
                }
                else
                {
                    LogError("No subtitles data to transmit");
                    
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler._DisableReloadButton();
                }
            }
            else if (!Networking.IsOwner(gameObject))
            {
                LogDebug("Reload (unprivileged)");

                if (IsSynchronized() && IsSyncedURL())
                {
                    LogDebug("Reload from synchronized URL");

                    FetchFromURL(_URLSync);
                }
                else if (_syncTime + syncCooldown < Networking.GetServerTimeInSeconds())
                {
                    SendCustomNetworkEvent(NetworkEventTarget.Owner, "SynchronizeSubtitles");

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler._DisableReloadButton(syncCooldown);

                    LogDebug("Resync request sent to owner");
                }
                else
                    LogDebug("Resync request cooldown");
            }
            else
                LogDebug("Reload (local)");

            ResetSubtitleTrackingState();
        }

        /*[PublicAPI]
        public void ReloadSyncedURL()
        {
            if (IsSyncedURL())
                FetchFromURL(_URLSync);
        }*/

        [PublicAPI]
        public void SynchronizeSettings(SubtitleControlHandler callingHandler = null)
        {
            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
            {
                if (callingHandler != null && handler == callingHandler)
                    continue;

                handler.UpdateValues();
            }

            SendCallback("OnUSharpVideoSubtitlesSettingsUpdate");
        }

        #endregion
        #region VRChat events

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject)) // Owner is guaranteed to have the subtitles
            {
                if (_dataSynced != "")
                    TransmitSubtitles();
                // To make sure the lock state is correct on the joiner
#if USHARPVIDEO_FOUND
                else if (!IsUsingUSharpVideo())
#else
                else
#endif
                    RequestSerialization();

#if USHARPVIDEO_FOUND
                if (_dataSynced != "" || !IsUsingUSharpVideo())
#endif
                    LogDebug($"Player joined ({player.displayName}) - request serialization");
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // Player who left was running the synchronization, resume it as we have all the data
            if (Networking.IsOwner(gameObject) && player == _previousOwner && !IsSynchronized() && IsSameSyncId())
            {
                if (!_isLocal)
                {
                    // This will prevent the status being stuck at "synchronizing last chunk"
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    {
                        handler._RestoreStatusText();

                        if (_dataSynced != "")
                            handler.SetStatusText(GetString("LOADED"));
                        else
                            handler.SetStatusText(GetString("NOT_LOADED"));

                        handler._SaveStatusText();
                    }
                }

                RequestSerialization();

                LogDebug($"Player left ({player.displayName}) - request serialization");
            }
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            if (!verifyOwnershipRequest)
                return true;

#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
                return !uSharpVideoPlayer.IsLocked() || uSharpVideoPlayer.IsPrivilegedUser(requestedOwner);
#endif

            return !_isLocked || IsPrivilegedUser(requestedOwner);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _previousOwner = _currentOwner;
            _currentOwner = Networking.GetOwner(gameObject);

            if (!_isLocal)
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                {
                    handler.UpdateOwner();
                    handler.UpdateLockState();
                }
            }

            SendCallback("OnUSharpVideoSubtitlesOwnershipChange");

            LogDebug($"Ownership changed ({player.displayName})");
        }

        #endregion
        #region USharpVideo integration

#if USHARPVIDEO_FOUND
        [PublicAPI]
        public bool IsUsingUSharpVideo()
        {
            return uSharpVideoPlayer != null;
        }

        [PublicAPI]
        public VRCPlayerApi GetUSharpVideoOwner()
        {
            if (uSharpVideoPlayer)
                return Networking.GetOwner(uSharpVideoPlayer.gameObject);

            return null; // Should never happen
        }

        public void OnUSharpVideoPlay()
        {
            VRCUrl currentURL = uSharpVideoPlayer.GetCurrentURL();

            if (currentURL != _lastVideoURL)
            {
                _lastVideoURL = currentURL;

                if (clearOnNewVideo && _dataSynced != "" && Networking.IsMaster)
                {
                    LogDebug("New URL detected, clearing subtitles...");

                    SetAndTransmitSubtitles("");

                    if (!_isLocal) // Prevents clearing of local subtitles
                        ClearSubtitlesInternal();
                }
            }
        }

        public void OnUSharpVideoLockChange()
        {
            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateLockState();

            if (setToVideoPlayerOwner)
                _MigrateToUSharpVideoOwner();

            SendCallback("OnUSharpVideoSubtitlesLockChange");
        }

        public void _MigrateToUSharpVideoOwner()
        {
            VRCPlayerApi videoPlayerOwner = Networking.GetOwner(uSharpVideoPlayer.gameObject);

            if (uSharpVideoPlayer.IsLocked() && Networking.LocalPlayer == videoPlayerOwner && Networking.GetOwner(gameObject) != videoPlayerOwner)
            {
                if (IsSynchronized())
                {
                    Log("Taking ownership because USharpVideo is now locked...");

                    TakeOwnership();
                }
                else
                {
                    LogDebug("Waiting 1 second before taking ownership because the synchronization is still running...");

                    SendCustomEventDelayedSeconds(nameof(_MigrateToUSharpVideoOwner), 1f);
                }
            }
        }

        public void OnUSharpVideoOwnershipChange()
        {
            if (uSharpVideoPlayer.IsLocked()) // Only to update the master in the input field's placeholder
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.UpdateLockState();
            }
        }
#endif

        #endregion
        #region Callback events

        [PublicAPI]
        public void RegisterCallbackReceiver(UdonSharpBehaviour callbackReceiver)
        {
            if (!callbackReceiver)
                return;

            if (_registeredCallbackReceivers == null)
                _registeredCallbackReceivers = new UdonSharpBehaviour[0];

            foreach (UdonSharpBehaviour currReceiver in _registeredCallbackReceivers)
            {
                if (callbackReceiver == currReceiver)
                    return;
            }

            UdonSharpBehaviour[] newControlHandlers = new UdonSharpBehaviour[_registeredCallbackReceivers.Length + 1];
            _registeredCallbackReceivers.CopyTo(newControlHandlers, 0);
            _registeredCallbackReceivers = newControlHandlers;

            _registeredCallbackReceivers[_registeredCallbackReceivers.Length - 1] = callbackReceiver;
        }

        [PublicAPI]
        public void UnregisterCallbackReceiver(UdonSharpBehaviour callbackReceiver)
        {
            if (!callbackReceiver)
                return;

            if (_registeredCallbackReceivers == null)
                _registeredCallbackReceivers = new UdonSharpBehaviour[0];

            int callbackReceiverCount = _registeredCallbackReceivers.Length;
            for (int i = 0; i < callbackReceiverCount; ++i)
            {
                UdonSharpBehaviour currHandler = _registeredCallbackReceivers[i];

                if (callbackReceiver == currHandler)
                {
                    UdonSharpBehaviour[] newCallbackReceivers = new UdonSharpBehaviour[callbackReceiverCount - 1];

                    for (int j = 0; j < i; ++j)
                        newCallbackReceivers[j] = _registeredCallbackReceivers[j];

                    for (int j = i + 1; j < callbackReceiverCount; ++j)
                        newCallbackReceivers[j - 1] = _registeredCallbackReceivers[j];

                    _registeredCallbackReceivers = newCallbackReceivers;

                    return;
                }
            }
        }

        private void SendCallback(string callbackName)
        {
            foreach (UdonSharpBehaviour callbackReceiver in _registeredCallbackReceivers)
            {
                if (callbackReceiver)
                    callbackReceiver.SendCustomEvent(callbackName);
            }

            LogDebug($"Send callback: {callbackName}");
        }

        #endregion
    }
}
