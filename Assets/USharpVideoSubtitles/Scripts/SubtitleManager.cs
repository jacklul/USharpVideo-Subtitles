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
using UnityEngine.UI;
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

#if USHARPVIDEO_FOUND
        [SerializeField, Tooltip("Takes priority over Base VRC Video Player if both are assigned")]
        private USharpVideoPlayer uSharpVideoPlayer;
#endif

        [SerializeField]
        private BaseVRCVideoPlayer baseVRCVideoPlayer;

        [Header("Settings")]

        [SerializeField, Range(5000, 50000), Tooltip("Maximum size of a single data chunk when synchronizing the subtitles to others - big chunk sizes can make synchronization fail\nValues above 10000 were not tested")]
        private int chunkSize = 10000;

        [Range(0, 60), PublicAPI, Tooltip("How many frames to wait before the next subtitle update - higher values decrease time accuracy of the subtitles but could increase game performance\nThe default is fine for most cases\nSetting this to zero updates every frame")]
        public int updateRate = 10;

        [Range(4.16f, 33.3f), PublicAPI, Tooltip("Approximate maximum processing time for the parser to take in miliseconds\nRecommended to keep this under 16.6 as otherwise it will reduce everyone's FPS below 60 during parsing\nSee https://fpstoms.com for more info")]
        public float parserTimeLimit = 11.1f;

        [SerializeField, Tooltip("When checked then anyone can control the subtitles by default\nThis setting does nothing when using USharpVideo as the lock state is inherited from it")]
        private bool defaultUnlocked = true;

        [PublicAPI, Tooltip("When checked then the instance creator can always control the subtitles regardless of if they are the master or not")]
        public bool allowInstanceCreatorControl = true;

        [PublicAPI, Tooltip("Remove unsupported tags from subtitle text\nYou should only disable this if you're going to serve pre-filtered subtitles")]
        public bool filterSubtitles = true;

        [PublicAPI, Tooltip("Sync entered URL to everyone for them to individually fetch the data themselves\nWhen false then the URL is fetched by the person who entered it and then the text is synchronized to everyone\nYou should keep it set to true for better VRC networking performance")]
        public bool syncOnlyUrl = true;

        [Range(0, 3), PublicAPI, Tooltip("Logging verbosity\n0 = none, 1 = errors, 2 = warnings, 3 = info")]
        public int logLevel = 2; // 0 = none, 1 = errors, 2 = warnings, 3 = info

#if USHARPVIDEO_FOUND
        [Header("USharpVideo Integration Settings")]

        [PublicAPI, Tooltip("Clear loaded subtitles when a new video starts?")]
        public bool clearOnNewVideo = false;

        [SerializeField, Tooltip("Force owner of this object to be whoever owns the video player when owner changes?")]
        private bool setToVideoPlayerOwner = false;
#endif

        [Header("Other")]

        [SerializeField, Tooltip("URL to load subtitles from on start")]
        private VRCUrl subtitlesURL;

        [SerializeField, Tooltip("JSON file containing translations for the hardcoded status messages\nSee LoadDefaultTranslation() function for all string keys")]
        private TextAsset translationFile;

        [SerializeField, Tooltip("Field to prepend debug log messages to")]
        private Text debugLogField;

        #endregion
        #region Variables

        // Sync
        [UdonSynced]
        private int _syncId; // Unique sync ID
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
        private float _timeOffset = 0.0f; // Video time offset

        // Translations
        private DataDictionary defaultTranslation;
        private DataDictionary translation;

        // References
#if USHARPVIDEO_FOUND
        private VideoPlayerManager _videoManager;
#endif
        private SubtitleOverlayHandler _overlayHandler;
        private SubtitleControlHandler[] _registeredControlHandlers;
        private UdonSharpBehaviour[] _registeredCallbackReceivers;

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
            if (!uSharpVideoPlayer && !baseVRCVideoPlayer)
                LogWarning("No video player reference assigned!");
#else
            if (!baseVRCVideoPlayer)
                LogWarning("No video player reference assigned!");
#endif

            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new SubtitleControlHandler[0];

            if (_registeredCallbackReceivers == null)
                _registeredCallbackReceivers = new UdonSharpBehaviour[0];

            LoadDefaultTranslation();

            if (translationFile)
                LoadTranslationFile();

#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer)
            {
                _videoManager = uSharpVideoPlayer.GetVideoManager();

                if (Networking.IsOwner(gameObject))
                    SendCustomEventDelayedFrames(nameof(OnUSharpVideoLockChange), 1); // The event will be initially triggered only for non-master players, this fixes the issue with wrong lock state on master player
            }
            else
            {
#endif
                if (Networking.IsOwner(gameObject))
                {
                    _isLocked = !defaultUnlocked;

                    SendCustomEventDelayedFrames(nameof(_QueueSerialize), 1); // Send lock state to everyone as initial state

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.UpdateLockState();
                }

                _lastLocked = _isLocked;
#if USHARPVIDEO_FOUND
            }
#endif

            _previousOwner = Networking.GetOwner(gameObject);
            _currentOwner = _previousOwner;

            ResetSubtitleTrackingState();
            LogMessage("Initialized");

            if (subtitlesURL.ToString() != "" && Networking.IsMaster) // subtitlesURL != VRCUrl.Empty doesn't seem to work here?
            {
                LogMessage("Will load initial subtitles");
                SendCustomEventDelayedSeconds(nameof(_LoadSubtitlesOnStart), 1f);
            }
        }

#if USHARPVIDEO_FOUND
        private void OnDisable()
        {
            if (uSharpVideoPlayer) uSharpVideoPlayer.UnregisterCallbackReceiver(this);
        }
#endif

        public void _LoadSubtitlesOnStart()
        {
            if (subtitlesURL.ToString() != "") // subtitlesURL != VRCUrl.Empty doesn't seem to work here?
            {
                LogMessage($"Loading subtitles from configured URL: {subtitlesURL}");
                FetchFromURL(subtitlesURL);
            }
        }

        public void RegisterOverlayHandler(SubtitleOverlayHandler handler)
        {
            if (_overlayHandler == null)
            {
                _overlayHandler = handler;
                _overlayHandler.ClearSubtitle();
            }
            else
                LogError("SubtitleOverlayHandler is already registered, only one can be active at the same time");
        }

        public void UnregisterOverlayHandler(SubtitleOverlayHandler handler)
        {
            if (handler == _overlayHandler)
                _overlayHandler = null;
            else
                LogError("This method must be called by the currently registered SubtitleOverlayHandler");
        }

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

            newControlHandler.SetToggleButtonState(_isEnabled);
            newControlHandler.SetLocalToggleButtonState(_isLocal);
            newControlHandler.SetStatusText(_dataCount > 0 ? GetTranslation("LOADED") : GetTranslation("NOT_LOADED"));
        }

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
        #region Translation

        private void LoadDefaultTranslation()
        {
            defaultTranslation = new DataDictionary();
            defaultTranslation.SetValue("LOADED", "Subtitles loaded");
            defaultTranslation.SetValue("NOT_LOADED", "No subtitles loaded");
            defaultTranslation.SetValue("CLEARED", "Subtitles cleared");
            defaultTranslation.SetValue("FETCHING", "Fetching from URL...");
            defaultTranslation.SetValue("FETCH_FAILED", "Failed to fetch from URL");
            defaultTranslation.SetValue("PARSING", "Parsing... {0}%");
            defaultTranslation.SetValue("PARSE_FAILED", "Failed to parse subtitles");
            defaultTranslation.SetValue("SYNCHRONIZING", "Synchronizing {0} / {1} {2}");
            defaultTranslation.SetValue("PLACEHOLDER_PASTE", "Paste SRT subtitles...");
            defaultTranslation.SetValue("PLACEHOLDER_URL", "Paste URL to SRT subtitles...");
            defaultTranslation.SetValue("WAIT_FOR_SYNC", "Wait for synchronization to finish");
            defaultTranslation.SetValue("ONLY_MASTER_CAN_ADD", "Only master {0} can add subtitles");
            defaultTranslation.SetValue("ONLY_OWNER_CAN_SYNC", "Only {0} can synchronize subtitles");
            defaultTranslation.SetValue("INDICATOR_LOCAL", "(local)");
            defaultTranslation.SetValue("INDICATOR_ANYONE", "(anyone)");
            defaultTranslation.SetValue("ALIGNMENT_BOTTOM", "Bottom");
            defaultTranslation.SetValue("ALIGNMENT_TOP", "Top");
        }

        [PublicAPI]
        public void LoadTranslationFile(TextAsset file = null)
        {
            if (file != null)
                translationFile = file;

            translation = null;

            if (translationFile != null)
            {
                if (VRCJson.TryDeserializeFromJson(translationFile.text, out DataToken result))
                {
                    if (result.TokenType == TokenType.DataDictionary)
                    {
                        translation = result.DataDictionary;
                        LogMessage("Translation loaded");
                    }
                    else
                        LogError("Translation file is not a valid dictionary object");
                }
                else
                    LogError("Failed to parse translation file");
            } else
                LogMessage("Translation file unset, using built-in messages");
        }

        public string GetTranslation(string key, params object[] args)
        {
            DataToken value = new DataToken();

            if (translation != null && translation.Count > 0 && translation.ContainsKey(key))
                translation.TryGetValue(key, out value);

            if (value.IsNull && defaultTranslation.ContainsKey(key))
                defaultTranslation.TryGetValue(key, out value);

            if (value.TokenType == TokenType.String)
                return args.Length > 0 ? string.Format(value.ToString(), args) : value.ToString();

            LogError($"Translation key '{key}' not found");
            return key;
        }

        #endregion
        #region Helpers

        public void LogMessage(string message)
        {
            if (debugLogField)
                debugLogField.text = message + "\n" + debugLogField.text;

            if (logLevel >= 3)
                Debug.Log(LOG_PREFIX + " " + message, this);
        }

        public void LogWarning(string message)
        {
            if (debugLogField)
                debugLogField.text = message + "\n" + debugLogField.text;

            if (logLevel >= 2)
                Debug.LogWarning(LOG_PREFIX + " " + message, this);
        }

        public void LogError(string message)
        {
            if (debugLogField)
                debugLogField.text = message + "\n" + debugLogField.text;

            if (logLevel >= 1)
                Debug.LogError(LOG_PREFIX + " " + message, this);
        }

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

            if (CanControlSubtitles()) {
                LogMessage("Taking ownership");

                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        // Used by SubtitleControlHandler to notify all other handlers about settings change
        public void SynchronizeSettings(SubtitleControlHandler callingHandler = null)
        {
            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
            {
                if (callingHandler != null && handler == callingHandler)
                    continue;

                handler.UpdateSettingsValues();
            }

            SendCallback("OnUSharpVideoSubtitlesSettingsUpdate");
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

                if (_overlayHandler) _overlayHandler.DisplaySubtitle(text);
            }
        }

        private bool IsVideoPlayerPlaying()
        {
#if USHARPVIDEO_FOUND
            if (_videoManager)
                return _videoManager.IsPlaying();
#endif

            if (baseVRCVideoPlayer)
                return baseVRCVideoPlayer.IsPlaying;

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
            if (baseVRCVideoPlayer)
                time = baseVRCVideoPlayer.GetTime();

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
                    handler.CloseInputMenu();
        }

        private void ResetSubtitleTrackingState()
        {
            _currentDataIndex = 0;
            _lastVideoTime = 0;

            if (_overlayHandler)
            {
                _overlayHandler.ClearSubtitle();
                _overlayHandler.SetPlaceholder(false);
            }
        }

        private void ClearSubtitlesLocal()
        {
            if (_dataCount > 0) LogMessage("Clearing subtitles locally");

            _dataText = new string[0];
            _dataTime = new Vector2[0];
            _dataCount = 0;

            ResetSubtitleTrackingState();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetTranslation("CLEARED"));

            SendCallback("OnUSharpVideoSubtitlesClear");
        }

        private void UnsetSubtitlesLocal()
        {
            ClearSubtitlesLocal();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetTranslation("NOT_LOADED"));
        }

        #endregion
        #region Synchronization

        private void SetAndTransmitSubtitles(string text)
        {
            TakeOwnership();

            _dataSynced = text; // Must be set no matter what for the local toggle to work correctly

            if (syncOnlyUrl && _URLTmp != VRCUrl.Empty && text != "") // Make sure to also handle clear button by checking for empty text
                _URLSync = _URLTmp;
            else
                _URLSync = VRCUrl.Empty;

            _syncId = Networking.GetServerTimeInMilliseconds();
            _lastSyncId = _syncId;

            TransmitSubtitles();
        }

        private void TransmitSubtitles()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            if (VRCPlayerApi.GetPlayerCount() == 1) // No point to even attempt to synchronize when alone in the instance
                return;

            if (_URLSync.ToString().Length > 0)
            {
                LogMessage($"Transmitting URL: {_URLSync}");

                _chunkCount = 1;
                _chunkSync = 0;
            }
            else
            {
                LogMessage($"Transmitting subtitles... (length = {_dataSynced.Length})");

                _chunkCount = _dataSynced.Length / chunkSize + 1;
                _chunkSync = 0;
            }

            if (!_isLocal)
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                {
                    handler.SaveStatusText();
                    handler.SetStatusText(GetTranslation("SYNCHRONIZING", 0, _chunkCount, ARROW_UP));
                }
            }

            RequestSerialization();
            SendCallback("OnUSharpVideoSubtitlesTransmitStart");
        }

        public override void OnPreSerialization()
        {
            if (_chunkSync < _chunkCount) // Makes sure this doesn't run while syncing just the lock state
            {
                LogMessage($"About to send chunk {_chunkSync + 1} / {_chunkCount} ({_syncId})");

                if (_URLSync.ToString().Length > 0) {
                    _syncedChunk = "";
                    return;
                }

                int start = Mathf.Min(_chunkSync * chunkSize, _dataSynced.Length);
                int length = Mathf.Min(chunkSize, _dataSynced.Length - _chunkSync * chunkSize);

                if (length < 0) // This can happen when master presses the lock button at the same time as someone else is starting to send the subtitles, this will also bug out UI for both players (toggling local mode cleans up the UI)
                    length = chunkSize;

                _syncedChunk = _dataSynced.Substring(start, length);
            }
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                LogError("Failed to serialize data, retrying in 1 second...");

                SendCustomEventDelayedSeconds(nameof(TransmitSubtitles), 1f);
                return;
            }

            if (_chunkSync < _chunkCount)
            {
                if (!_isLocal)
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(GetTranslation("SYNCHRONIZING", _chunkSync + 1, _chunkCount, ARROW_UP));
                }

                LogMessage($"Sent chunk {_chunkSync + 1} / {_chunkCount} ({_syncId})");

                _chunkSync++;

                if (_chunkSync < _chunkCount)
                {
                    LogMessage("Will send another chunk...");

                    SendCallback("OnUSharpVideoSubtitlesTransmitProgress");
                }
                else
                {
                    LogMessage($"Sent all chunks");

                    if (!_isLocal)
                    {
                        foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                            handler.RestoreStatusText();
                    }

                    SendCallback("OnUSharpVideoSubtitlesTransmitFinish");
                }

                SendCustomEventDelayedFrames(nameof(_QueueSerialize), 1);
            }
        }

        public void _QueueSerialize()
        {
            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject))
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

            if (IsSynchronized()) // This will only happen when syncing just the lock state or after the last chunk was received
                return;

            if (IsSameSyncId())
            {
                LogMessage($"Not loading chunk {_chunkSync + 1} / {_chunkCount} because it has the same identifier ({_syncId}) as the previously loaded one ({_lastSyncId})");
                return;
            }

            if (!_isLocal)
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetStatusText(GetTranslation("SYNCHRONIZING", _chunkSync + 1, _chunkCount, ARROW_DOWN));
            }

            LogMessage($"Received chunk {_chunkSync + 1} / {_chunkCount} ({_syncId})");

            if (_chunkSync == 0)
            {
                _localChunkSync = 0;
                _dataSynced = _syncedChunk;

                foreach (SubtitleControlHandler handler in _registeredControlHandlers) // Update reload button color at the start of sync
                    handler.UpdateOwner();
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

                LogMessage($"Received all chunks");

                if (!_isLocal)
                {
                    if (_URLSync.ToString().Length > 0)
                    {
                        LogMessage($"Applying synchronized URL: {_URLSync}");

                        FetchFromURL(_URLSync);
                    }
                    else
                    {
                        if (_dataSynced.Length == 0)
                        {
                            ClearSubtitlesLocal();
                            return;
                        }

                        LogMessage($"Applying synchronized data (length = {_dataSynced.Length})");

                        LoadSubtitles(_dataSynced, false);
                    }

                    ResetSubtitleTrackingState();

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers) // Update reload button color at the end of sync (in case of URL sync)
                        handler.UpdateOwner();
                }
            }
        }

        [PublicAPI]
        public bool IsSynchronized()
        {
            return _chunkSync == _chunkCount && IsSameSyncId();
        }

        private bool IsSameSyncId()
        {
            return _lastSyncId == _syncId;
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
                LogWarning($"Could not check total subtitle count");

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetStatusText(GetTranslation("PARSE_FAILED"));

                SendCallback("OnUSharpVideoSubtitlesParseError");

                return;
            }

            LogMessage($"Detected {len} subtitle groups");

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
                handler.SetStatusText(GetTranslation("PARSING", (int)Math.Round((double)(100 * _parserIndex) / _dataText.Length)));

            int parserState = 0;
            for (int i = _parserLine; i < _parserArray.Length; i++)
            {
                string line = _parserArray[i];

                if (parserState == 0 && line.Contains(" --> "))
                {
                    string[] times = line.Split(new string[] {" --> "}, StringSplitOptions.None);

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

                LogMessage($"Parsed {groups} subtitle groups");
            }

            if (!_isParsing)
            {
                if (_dataText.Length > 0)
                {
                    _dataCount = _dataText.Length;

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(GetTranslation("LOADED"));

                    SendCallback("OnUSharpVideoSubtitlesLoad");
                }
                else
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(GetTranslation("PARSE_FAILED"));

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
                LogWarning("String input is empty");
                return;
            }

            LogMessage($"Loaded string input (length = {input.Length})");

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
                LogWarning("URL input is empty");
                return;
            }

            LogMessage($"Loaded URL input: {url}");

            _URLTmp = url;

            FetchFromURL(url);
        }

        private void FetchFromURL(VRCUrl url)
        {
            LogMessage("Loading text from URL: " + url);

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetTranslation("FETCHING"));

            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            LogMessage($"Remote string load success ({BitConverter.ToInt32(result.ResultBytes, 0)} bytes)");

            if (result.Url == _URLTmp) // User entered URL - to be synchronized
                ProcessInput(result.Result);
            else // User received URL - to be loaded
            {
                _dataSynced = result.Result; // This prevent re-fetching from URL when user toggles local mode after fetching

                LoadSubtitles(result.Result, false);
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _URLTmp = VRCUrl.Empty;

            LogError("Failed to load subtitles from URL: " + result.Error);

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(GetTranslation("FETCH_FAILED")); //handler.SetStatusText(result.Error);

            SendCallback("OnUSharpVideoSubtitlesFetchError");
        }

        #endregion
        #region API
        // These methods are not used internally, they are only for external use

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

            if (!IsSynchronized() && !Networking.IsOwner(gameObject)) // Prevent locking when someone else is redistributing the subtitles as this would break the sync for everyone
                return;

            TakeOwnership();

            _isLocked = state;
            _lastLocked = _isLocked;

            if (IsSynchronized()) // We don't have to call this when the synchronization is still going
                RequestSerialization();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateLockState();

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

            if (state)
            {
                _isEnabled = true;
            }
            else
            {
                _isEnabled = false;
                if (_overlayHandler) _overlayHandler.ClearSubtitle();
            }

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetToggleButtonState(_isEnabled);

            SendCallback("OnUSharpVideoSubtitlesEnabledChange");
        }

        [PublicAPI]
        public bool IsLocal()
        {
            return _isLocal;
        }

        [PublicAPI]
        public void SetLocal(bool state)
        {
            if (_isLocal == state)
                return;

            _isLocal = state;

            LogMessage($"Local mode = {(_isLocal ? "ON" : "OFF")}");

            if (state)
            {
                if (_dataLocal != "")
                    LoadSubtitles(_dataLocal, false);
                else
                    UnsetSubtitlesLocal();
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
                        UnsetSubtitlesLocal();
                }
                else
                    UnsetSubtitlesLocal();
            }

            ResetSubtitleTrackingState();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetLocalToggleButtonState(_isLocal);

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateOwner();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.UpdateLockState();

            SendCallback("OnUSharpVideoSubtitlesModeChange");
        }

        [PublicAPI]
        public float GetTimeOffset()
        {
            return _timeOffset;
        }

        [PublicAPI]
        public void SetTimeOffset(float offset, bool sync = true)
        {
            _timeOffset = -offset;

            SendCallback("OnUSharpVideoSubtitlesTimeOffsetChange");

            if (sync)
                SynchronizeSettings();
        }

        [PublicAPI]
        public bool HasSubtitles()
        {
            return _dataCount > 0;
        }

        [PublicAPI]
        public void SetVideoPlayer(BaseVRCVideoPlayer videoPlayer)
        {
#if USHARPVIDEO_FOUND
            if (uSharpVideoPlayer) {
                LogWarning("Method SetVideoPlayer cannot be used with USharpVideo");
                return;
            }
#endif

            baseVRCVideoPlayer = videoPlayer;
            ResetSubtitleTrackingState();
            SendCallback("OnUSharpVideoSubtitlesVideoPlayerChange");
        }

        [PublicAPI]
        public void ClearSubtitles()
        {
            if (!_isLocal)
            {
                if (!CanControlSubtitles())
                    return;

                if (!IsSynchronized())
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.RestoreStatusText(); // Prevent "subtitles loaded" status to be set after clearing when synchronization is still running
                }

                ClearSubtitlesLocal(); // Must be called first otherwise RestoreStatusText() in OnPostSerialization will send previous status instead of cleared message
                SetAndTransmitSubtitles("");
            }
            else
            {
                _dataLocal = "";
                ClearSubtitlesLocal();
            }
        }

        [PublicAPI]
        public void SynchronizeSubtitles()
        {
            if (!_isLocal)
            {
                if (_dataSynced == "")
                    return;

                if (CanSynchronizeSubtitles() && IsSynchronized()) // Owner is guranteed to have the subtitles and master should always be able to resync
                {
                    TakeOwnership();
                    TransmitSubtitles();
                }
            }

            ResetSubtitleTrackingState();
        }

        [PublicAPI]
        public void ReloadSyncedURL()
        {
            if (IsSyncedURL())
                FetchFromURL(_URLSync);
        }

        [PublicAPI]
        public bool CanSynchronizeSubtitles()
        {
            return Networking.IsOwner(gameObject) || IsPrivilegedUser(Networking.LocalPlayer);
        }

        #endregion
        #region VRChat events

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject))
            {
                if (_dataSynced != "")
                    TransmitSubtitles();
#if USHARPVIDEO_FOUND
                else if (!IsUsingUSharpVideo()) // To make sure the lock state is correct on the joiner
#else
                else
#endif
                    RequestSerialization();

#if USHARPVIDEO_FOUND
                if (_dataSynced != "" || !IsUsingUSharpVideo())
#endif
                    LogMessage($"Player joined ({player.displayName}) - request serialization");
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject) && player == _previousOwner && !IsSynchronized() && IsSameSyncId()) // Player who left was running the synchronization, resume it as we have all the data
            {
                if (!_isLocal)
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers) // This will prevent the status being stuck at "synchronizing last chunk"
                    {
                        handler.RestoreStatusText();

                        if (_dataSynced != "")
                            handler.SetStatusText(GetTranslation("LOADED"));
                        else
                            handler.SetStatusText(GetTranslation("NOT_LOADED"));

                        handler.SaveStatusText();
                    }
                }

                RequestSerialization();

                LogMessage($"Player left ({player.displayName}) - request serialization");
            }
        }

// Similary to how it is in USharpVideo - uncomment this to prevent people from taking ownership when they shouldn't be able to
//        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
//        {
//#if USHARPVIDEO_FOUND
//            if (uSharpVideoPlayer)
//                return !uSharpVideoPlayer.IsLocked() || uSharpVideoPlayer.IsPrivilegedUser(requestedOwner);
//#endif
//            
//            return !_isLocked || IsPrivilegedUser(requestedOwner);
//        }

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

            LogMessage($"Ownership changed ({player.displayName})");
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
                    LogMessage("New URL detected, clearing subtitles...");

                    SetAndTransmitSubtitles("");

                    if (!_isLocal) // Prevents clearing of local subtitles
                        ClearSubtitlesLocal();
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
                    LogMessage("Taking ownership because USharpVideo is now locked...");

                    TakeOwnership();
                }
                else
                {
                    LogMessage("Waiting 1 second before taking ownership because the synchronization is still running...");

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
        }

        #endregion
    }
}
