/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 * 
 * Based on code by Haï~ (https://github.com/hai-vr) - https://gist.github.com/hai-vr/b340f9a46952640f81efe7f02da6bdf6
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Video.Components.Base;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace UdonSharp.Video.Subtitles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SubtitleManager : UdonSharpBehaviour
    {
        private const char ARROW_UP = '▲'; // ▲ ⇪ ↑
        private const char ARROW_DOWN = '▼'; // ▼ ⇩ ↓
        private const string LOG_PREFIX = "[<color=#7ecad6>USharpVideo-Subtitles</color>]";
        private const string MESSAGE_LOADED = "Subtitles loaded";
        private const string MESSAGE_NOT_LOADED = "No subtitles loaded";
        private const string MESSAGE_CLEARED = "Subtitles cleared";
        private const string MESSAGE_FETCHING = "Fetching from URL...";
        private const string MESSAGE_PARSING = "Parsing... {0}%";
        private const string MESSAGE_FAILED = "Failed to parse subtitles";
        private const string MESSAGE_SYNCHRONIZING = "Synchronizing {0} / {1} {2}";

#if USHARPVIDEO_FOUND
        [SerializeField]
        private USharpVideoPlayer targetVideoPlayer;
#endif

        [SerializeField]
        private BaseVRCVideoPlayer baseVideoPlayer;

        [Header("Settings")]

        [SerializeField, Range(5000, 50000), Tooltip("Maximum size of a single data chunk when synchronizing the subtitles to others - big chunk sizes can make synchronization fail")]
        private int chunkSize = 10000;

        [Range(0, 60), Tooltip("How many frames to wait before the next subtitle update - higher values decrease time accuracy of the subtitles but could increase game performance\nThe default is fine for most cases\nSetting this to zero updates every frame")]
        public int updateRate = 10;

        [Range(0.0069f, 0.033f), Tooltip("Maximum processing time for the parser to take as a fraction of one second\nRecommended to keep this under 0.0166 (16.6ms) as otherwise it will reduce everyone's FPS below 60 during parsing\nSee https://fpstoms.com for more info")]
        public float parserTimeLimit = 0.01f;

        [SerializeField, Tooltip("When false then only the master can manage the subtitles by default\nThis setting does nothing when using USharpVideo as the lock state is inherited from it")]
        private bool defaultUnlocked = true;

        [Tooltip("Removes unsupported tags from subtitle text\nDisabling this will speed up processing of huge files - you should only disable this if you serve pre-filtered subtitles")]
        public bool filterSubtitles = true;

        [Tooltip("Sync entered URL to everyone for each to individually fetch the data themselves\nWhen disabled the URL is fetched by the person who entered it and then the text is synchronized to everyone\nYou should keep it on for better networking performance")]
        public bool syncOnlyUrl = true;

#if USHARPVIDEO_FOUND
        [Header("USharpVideo Integration Settings")]

        [Tooltip("Clear loaded subtitles when a new video starts?")]
        public bool clearOnNewVideo = false;

        [SerializeField, Tooltip("Force owner of this object to be whoever owns the video player when owner changes?")]
        private bool setToVideoPlayerOwner = false;
#endif

        [Header("Other")]

        [SerializeField, Tooltip("Field to prepend log messages to\nUseful only in development")]
        private Text debugLogField;

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

#if USHARPVIDEO_FOUND
        private void OnEnable()
        {
            if (targetVideoPlayer) targetVideoPlayer.RegisterCallbackReceiver(this);
        }
#endif

        private void Start()
        {
#if USHARPVIDEO_FOUND
            if (!targetVideoPlayer && !baseVideoPlayer)
                LogWarning("No video player reference assigned!");

            if (targetVideoPlayer && baseVideoPlayer)
                LogWarning("You cannot reference USharpVideo and Unity or AVPro Video Player at the same time - USharpVideo takes precedence!");
#else
            if (!baseVideoPlayer)
                LogWarning("No video player reference assigned!");
#endif

            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new SubtitleControlHandler[0];

            if (_registeredCallbackReceivers == null)
                _registeredCallbackReceivers = new UdonSharpBehaviour[0];

#if USHARPVIDEO_FOUND
            if (targetVideoPlayer)
            {
                _videoManager = targetVideoPlayer.GetVideoManager();

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
        }

#if USHARPVIDEO_FOUND
        private void OnDisable()
        {
            if (targetVideoPlayer) targetVideoPlayer.UnregisterCallbackReceiver(this);
        }
#endif

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

                for (int i = _currentDataIndex; i < _dataText.Length; i++)
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

            if (baseVideoPlayer)
                return baseVideoPlayer.IsPlaying;

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
            if (baseVideoPlayer)
                time = baseVideoPlayer.GetTime();

            if (_timeOffset != 0.0f)
            {
                time += _timeOffset;

                if (time < 0.0f)
                    time = 0.0f;
            }

            return time;
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
                    handler.SetStatusText(string.Format(@MESSAGE_SYNCHRONIZING, 0, _chunkCount, ARROW_UP));
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
            if (_chunkSync < _chunkCount)
            {
                if (!_isLocal)
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(string.Format(@MESSAGE_SYNCHRONIZING, _chunkSync + 1, _chunkCount, ARROW_UP));
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
            if (!targetVideoPlayer && _lastLocked != _isLocked)
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
                    handler.SetStatusText(string.Format(@MESSAGE_SYNCHRONIZING, _chunkSync + 1, _chunkCount, ARROW_DOWN));
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

        public bool IsSynchronized()
        {
            return _chunkSync == _chunkCount && IsSameSyncId();
        }

        public bool IsSameSyncId()
        {
            return _lastSyncId == _syncId;
        }

        public bool IsSyncedURL()
        {
            return _URLSync.ToString().Length > 0;
        }

        private void ClearSubtitlesLocal()
        {
            if (_dataText.Length > 0) LogMessage("Clearing subtitles locally");

            _dataText = new string[0];
            _dataTime = new Vector2[0];
            _dataCount = 0;

            ResetSubtitleTrackingState();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(MESSAGE_CLEARED);

            SendCallback("OnUSharpVideoSubtitlesClear");
        }

        private void UnsetSubtitlesLocal()
        {
            ClearSubtitlesLocal();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(MESSAGE_NOT_LOADED);
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

        private void InitializeParser(string text)
        {
            ResetParser();

            int len = text.Split(new string[] {" --> "}, StringSplitOptions.None).Length - 1;

            if (len <= 0)
            {
                LogError($"Could not check total subtitle count");

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetStatusText(MESSAGE_FAILED);

                SendCallback("OnUSharpVideoSubtitlesError");

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

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(string.Format(@MESSAGE_PARSING, (int)Math.Round((double)(100 * _parserIndex) / _dataText.Length)));

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
                    _dataText[_parserIndex] = HandleTextNewLine(filterSubtitles ? FilterSubtitle(line) : line);
                    parserState = 2;
                }
                else if (parserState == 2 && line != "")
                {
                    _dataText[_parserIndex] += "\n" + HandleTextNewLine(filterSubtitles ? FilterSubtitle(line) : line);
                }
                else if (parserState != 0 && (line == "" || i == _parserArray.Length - 1))
                {
                    _parserLine = i;
                    _parserIndex++;
                    parserState = 0;
                }

                if (parserState == 0 && Time.realtimeSinceStartup > startTime + parserTimeLimit)
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
                        handler.SetStatusText(MESSAGE_LOADED);

                    SendCallback("OnUSharpVideoSubtitlesLoad");
                }
                else
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(MESSAGE_FAILED);

                    SendCallback("OnUSharpVideoSubtitlesError");
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

        private string FilterSubtitle(string text) // This function removes unsupported HTML tags, VTT cues and other unwanted characters
        {
            char[] allowedShortHTMLTags = { 'b', 'i', 'u' };
            // Technically we could support font and color tags here... but support for this would have to be implemented into SubtitleOverlayHandler

            // Replace {} with <> for allowed tags
            foreach (char tag in allowedShortHTMLTags)
                text = text.Replace("{" + tag + "}", "<" + tag + ">").Replace("{/" + tag + "}", "</" + tag + ">");

            char[] textArray = text.ToCharArray();
            text = "";

            bool inHtmlTag = false;
            bool inVTTCue = false;
            for (int i = 0; i < textArray.Length; i++)
            {
                if (!inHtmlTag && textArray[i] == '{' && i + 1 < textArray.Length && textArray[i + 1] == '\\') // Start of VTT cue
                {
                    inVTTCue = true;
                    continue;
                }
                else if (!inHtmlTag && inVTTCue) // Skip contents until end of VTT cue
                {
                    if (textArray[i] == '}')
                        inVTTCue = false;

                    continue;
                }
                else if (!inVTTCue && textArray[i] == '<' && i + 1 < textArray.Length) // Start of HTML tag
                {
                    bool isEndingTag = textArray[i + 1] == '/';
                    bool isShortTag = false;
                    char shortTagValue = '\0';

                    if (i + 3 < textArray.Length)
                        isShortTag = isEndingTag ? textArray[i + 3] == '>' : textArray[i + 2] == '>';

                    if (i + 2 < textArray.Length)
                        shortTagValue = isShortTag ? (isEndingTag ? textArray[i + 2] : textArray[i + 1]) : ' ';

                    if (!isShortTag || Array.IndexOf(allowedShortHTMLTags, shortTagValue) == -1)
                    {
                        inHtmlTag = true;
                        continue;
                    }
                }
                else if (!inVTTCue && inHtmlTag) // Skip contents until end of HTML tag
                {
                    if (textArray[i] == '>')
                        inHtmlTag = false;

                    continue;
                } 

                text += textArray[i];
            }

            return text;
        }

        private string HandleTextNewLine(string text)
        {
            text = text.Replace("\\n", "\n").Replace("\\N", "\n");

            return text;
        }

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

            if (_dataText.Length > 0)
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
                handler.SetStatusText(MESSAGE_FETCHING);

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
                handler.SetStatusText(result.Error);
        }

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

        private void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject))
                return;

            if (CanControlSubtitles()) {
                LogMessage("Taking ownership");

                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        private void LogMessage(string message)
        {
            Debug.Log(LOG_PREFIX + " " + message, this);

            if (debugLogField)
                debugLogField.text = message + "\n" + debugLogField.text;
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning(LOG_PREFIX + " " + message, this);

            if (debugLogField)
                debugLogField.text = message + "\n" + debugLogField.text;
        }

        private void LogError(string message)
        {
            Debug.LogError(LOG_PREFIX + " " + message, this);

            if (debugLogField)
                debugLogField.text = message + "\n" + debugLogField.text;
        }

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
                            handler.SetStatusText(MESSAGE_LOADED);
                        else
                            handler.SetStatusText(MESSAGE_NOT_LOADED);

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
//            if (targetVideoPlayer)
//                return !targetVideoPlayer.IsLocked() || targetVideoPlayer.IsPrivilegedUser(requestedOwner);
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

#if USHARPVIDEO_FOUND
        public bool IsUsingUSharpVideo()
        {
            return targetVideoPlayer != null;
        }

        public VRCPlayerApi GetUSharpVideoOwner()
        {
            if (targetVideoPlayer)
                return Networking.GetOwner(targetVideoPlayer.gameObject);

            return null; // Should never happen
        }
#endif

        public bool IsLocked()
        {
#if USHARPVIDEO_FOUND
            if (targetVideoPlayer)
                return targetVideoPlayer.IsLocked();
#endif

            return _isLocked;
        }

        public void SetLocked(bool state)
        {
            if (!IsPrivilegedUser(Networking.LocalPlayer))
                return;

#if USHARPVIDEO_FOUND
            if (targetVideoPlayer)
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

        public bool CanControlSubtitles()
        {
#if USHARPVIDEO_FOUND
            if (targetVideoPlayer)
                return targetVideoPlayer.CanControlVideoPlayer();
#endif

            return !_isLocked || IsPrivilegedUser(Networking.LocalPlayer);
        }

        public bool IsPrivilegedUser(VRCPlayerApi player)
        {
#if USHARPVIDEO_FOUND
            if (targetVideoPlayer)
                return targetVideoPlayer.IsPrivilegedUser(player);
#endif

            return player.isMaster;
        }

        public bool IsEnabled()
        {
            return _isEnabled;
        }

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

            SendCallback("OnUSharpVideoSubtitlesEnabledStatusChange");
        }

        public bool IsLocal()
        {
            return _isLocal;
        }

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

        public float GetTimeOffset()
        {
            return _timeOffset;
        }

        public void SetTimeOffset(float offset)
        {
            _timeOffset = -offset;

            SendCallback("OnUSharpVideoSubtitlesTimeOffsetChange");
        }

        public bool HasSubtitles()
        {
            return _dataText.Length > 0;
        }

        public void SetVideoPlayer(BaseVRCVideoPlayer videoPlayer)
        {
#if USHARPVIDEO_FOUND
            if (targetVideoPlayer) {
                LogWarning("Method SetVideoPlayer cannot be used with USharpVideo");
                return;
            }
#endif

            baseVideoPlayer = videoPlayer;
            ResetSubtitleTrackingState();
            SendCallback("OnUSharpVideoSubtitlesVideoPlayerChange");
        }

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
            SendCallback("OnUSharpVideoSubtitlesSynchronize");
        }

        public void ReloadSyncedURL()
        {
            if (IsSyncedURL())
                FetchFromURL(_URLSync);
        }

        public bool CanSynchronizeSubtitles()
        {
            return Networking.IsOwner(gameObject) || IsPrivilegedUser(Networking.LocalPlayer);
        }

        public void SynchronizeSettings(SubtitleControlHandler callingHandler)
        {
            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
            {
                if (handler == callingHandler)
                    continue;

                handler.UpdateSettingsValues();
            }

            SendCallback("OnUSharpVideoSubtitlesSettingsUpdate");
        }

#if USHARPVIDEO_FOUND
        public void OnUSharpVideoPlay()
        {
            VRCUrl currentURL = targetVideoPlayer.GetCurrentURL();

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
            VRCPlayerApi videoPlayerOwner = Networking.GetOwner(targetVideoPlayer.gameObject);

            if (targetVideoPlayer.IsLocked() && Networking.LocalPlayer == videoPlayerOwner && Networking.GetOwner(gameObject) != videoPlayerOwner)
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
            if (targetVideoPlayer.IsLocked()) // Only to update the master in the input field's placeholder
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.UpdateLockState();
            }
        }
#endif

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
            newControlHandler.SetStatusText(_dataText.Length > 0 ? MESSAGE_LOADED : MESSAGE_NOT_LOADED);
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
    }
}
