/*
 * Copyright (c) 2022 Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 * 
 * Based on code by Haï~ (https://github.com/hai-vr) - https://gist.github.com/hai-vr/b340f9a46952640f81efe7f02da6bdf6
 */

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

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
        private const string MESSAGE_PARSING = "Parsing... (length={0})";
        private const string MESSAGE_FAILED = "Failed to parse subtitles";
        private const string MESSAGE_EMPTY = "Input data is empty";
        private const string MESSAGE_WAIT_SYNC = "Wait for synchronization to finish";
        private const string MESSAGE_ONLY_OWNER_CAN = "Only {0} can {1}";
        private const string MESSAGE_ONLY_ACTION_SYNC = "synchronize subtitles";
        private const string MESSAGE_ONLY_ACTION_ADD = "add subtitles";
        private const string MESSAGE_ONLY_ACTION_CLEAR = "clear subtitles";
        private const string MESSAGE_SYNCHRONIZING = "Synchronizing {0} / {1} {2}";
        private const string SUBTITLE_PLACEHOLDER = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
        private const string INDICATOR_LOCAL = "(local)";

        [SerializeField]
        private USharpVideoPlayer targetVideoPlayer;

        [Header("Settings")]

        [SerializeField, Range(5000, 50000), Tooltip("Maximum size of a single data chunk when syncing the subtitles to others - big chunk sizes can make synchronization fail")]
        private int chunkSize = 10000;

        [Range(10, 60), Tooltip("How many frames to wait before the next subtitle update - higher values decrease time accuracy of the subtitles but increase game performance")]
        public int updateRate = 10;

        [Tooltip("Should we automatically clear loaded subtitles when a new video starts")]
        public bool clearOnNewVideo = false;

        [UdonSynced]
        private string syncedChunk;
        [UdonSynced]
        private int syncId;

        private string _data = "";
        private string _dataLocal = "";

        private string[] _dataText = new string[0];
        private float[] _dataStart = new float[0];
        private float[] _dataEnd = new float[0];
        private int _dataTotal = 0;

        [UdonSynced]
        private int _chunkCount;
        [UdonSynced]
        private int _chunkSync;

        private int _localChunkSync;
        private int _lastSyncId;

        private bool _isEnabled = true;
        private bool _isLocal = false;
        private bool _showPlaceholder = false;

        private VideoPlayerManager _videoManager;
        private SubtitleControlHandler[] _registeredControlHandlers;
        private SubtitleOverlayHandler _overlayHandler;

        private int _lastUpdateFrame = 0;
        private int _currentDataIndex = 0;
        private float _lastVideoTime = 0;
        private VRCUrl _lastVideoURL = VRCUrl.Empty;

        private void OnEnable()
        {
            targetVideoPlayer.RegisterCallbackReceiver(this);
        }

        private void Start()
        {
            _videoManager = targetVideoPlayer.GetVideoManager();

            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new SubtitleControlHandler[0];

            ResetSubtitleTrackingState();
        }

        private void OnDisable()
        {
            targetVideoPlayer.UnregisterCallbackReceiver(this);
        }

        public USharpVideoPlayer GetTargetVideoPlayer()
        {
            if (targetVideoPlayer == null)
            {
                LogWarning("Target Video Player reference is not set, searching for the component in the parent object...");
                targetVideoPlayer = gameObject.GetComponentInParent<USharpVideoPlayer>();
            }

            return targetVideoPlayer;
        }

        public void RegisterOverlayHandler(SubtitleOverlayHandler handler)
        {
            if (_overlayHandler == null)
            {
                _overlayHandler = handler;
                _overlayHandler.ClearSubtitle();
            }
            else
                LogWarning("SubtitleOverlayHandler is already registered, only one can be active at the same time");
        }

        public void UnregisterOverlayHandler(SubtitleOverlayHandler handler)
        {
            if (handler == _overlayHandler)
                _overlayHandler = null;
            else
                LogWarning("This method must be called by the currently registered SubtitleOverlayHandler");
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

            if (_dataTotal > 0)
                newControlHandler.SetStatusText(MESSAGE_LOADED);
            else
                newControlHandler.SetStatusText(MESSAGE_NOT_LOADED);
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

        public void Update()
        {
            if ((!_isEnabled || _dataTotal == 0) && !_showPlaceholder) return;

            if (_lastUpdateFrame < updateRate)
            {
                _lastUpdateFrame++;
                return;
            }
            _lastUpdateFrame = 0;

            if (_showPlaceholder)
            {
                if (_overlayHandler) _overlayHandler.DisplaySubtitle(SUBTITLE_PLACEHOLDER);
                return;
            }

            if (_videoManager.IsPlaying()) // Don't update subtitles if the video is not playing
            {
                float time = _videoManager.GetTime();

                if (time == _lastVideoTime) return;
                if (time < _lastVideoTime)
                    ResetSubtitleTrackingState();
                _lastVideoTime = time;

                string text = "";

                for (int i = _currentDataIndex; i < _dataText.Length; i++)
                {
                    if (time >= _dataStart[i] && time <= _dataEnd[i]) // Subtitle display time matches current time
                    {
                        if (text != "")
                            text += "\n" + _dataText[i]; // Support overlapping subtitles
                        else
                            text = _dataText[i];
                    }
                    else if (time > _dataEnd[_currentDataIndex]) // Currently tracked subtitle is no longer to be shown
                        _currentDataIndex++;
                    else
                        break;
                }

                if (_overlayHandler) _overlayHandler.DisplaySubtitle(text);
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject) && _data != "")
                TransmitSubtitles();
        }

        private void TransmitSubtitles()
        {
            if (VRCPlayerApi.GetPlayerCount() == 1) return; // No point to even attempt to synchronize when alone in the instance

            LogMessage($"Transmitting subtitles... (length = {_data.Length})");

            _chunkCount = _data.Length / chunkSize + 1;
            _chunkSync = 0;

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
            {
                handler.SaveStatusText();
                handler.SetStatusText(string.Format(@MESSAGE_SYNCHRONIZING, 0, _chunkCount, ARROW_UP));
            }

            RequestSerialization();
        }

        public override void OnPreSerialization()
        {
            LogMessage($"About to send chunk {_chunkSync + 1} / {_chunkCount} ({syncId})");

            syncedChunk = _data.Substring(
                Mathf.Min(_chunkSync * chunkSize, _data.Length),
                Mathf.Min(chunkSize, _data.Length - _chunkSync * chunkSize)
            );
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(string.Format(@MESSAGE_SYNCHRONIZING, _chunkSync + 1, _chunkCount, ARROW_UP));

            LogMessage($"Sent chunk {_chunkSync + 1} / {_chunkCount} ({syncId})");

            _chunkSync++;

            if (_chunkSync < _chunkCount)
            {
                LogMessage("Will send another chunk...");
                SendCustomEventDelayedFrames(nameof(_SendNextChunk), 1);
            }
            else
            {
                LogMessage($"Sent all chunks");

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.RestoreStatusText();
            }
        }

        public void _SendNextChunk()
        {
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            if (_lastSyncId == syncId)
            {
                LogMessage($"Not loading chunk {_chunkSync + 1} / {_chunkCount} because it has the same identifier ({syncId}) as the previously loaded one ({_lastSyncId})");
                return;
            }

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetStatusText(string.Format(@MESSAGE_SYNCHRONIZING, _chunkSync + 1, _chunkCount, ARROW_DOWN));

            LogMessage($"Received chunk {_chunkSync + 1} / {_chunkCount} ({syncId})");

            if (_chunkSync == 0)
            {
                _localChunkSync = 0;
                _data = syncedChunk;
            }
            else if (_localChunkSync == _chunkSync - 1)
            {
                _localChunkSync++;
                _data += syncedChunk;
            }
            else
                LogWarning($"Rejected chunk {_chunkSync + 1} because local chunk is {_localChunkSync}");

            if (_localChunkSync == _chunkCount - 1)
            {
                LogMessage($"Received all chunks");

                if (!_isLocal)
                {
                    _lastSyncId = syncId;

                    if (_data.Length == 0)
                    {
                        ClearSubtitlesLocal();
                        return;
                    }

                    LogMessage($"Applying synchronized data (length = {_data.Length})");

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.ClearSubtitleInput();

                    LoadSubtitles(_data, false);
                    ResetSubtitleTrackingState();
                }
            }
        }

        private void ClearSubtitlesLocal()
        {
            LogMessage("Clearing subtitles locally");

            _dataText = new string[0];
            _dataStart = new float[0];
            _dataEnd = new float[0];
            _dataTotal = 0;

            ResetSubtitleTrackingState();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
            {
                handler.ClearSubtitleInput();
                handler.SetStatusText(MESSAGE_CLEARED);
            }
        }

        private void LoadSubtitles(string subtitles, bool closeInputMenu)
        {
            if (subtitles != "")
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetStatusText(string.Format(@MESSAGE_PARSING, subtitles.Length));

                if (ParseSubtitles(subtitles))
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    {
                        handler.SetStatusText(MESSAGE_LOADED);
                        if (closeInputMenu) handler.CloseInputMenu();
                    }
                }
                else
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(MESSAGE_FAILED);
                }
            }
            else
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetStatusText(MESSAGE_EMPTY);

                LogError("Requested to load empty data - this shouldn't happen");
            }
        }

        private void ResetSubtitleTrackingState()
        {
            _currentDataIndex = 0;
            _lastVideoTime = 0;

            if (_overlayHandler != null)
                _overlayHandler.ClearSubtitle();

            SetPlaceholder(false);
        }

        private bool ParseSubtitles(string text)
        {
            string[] array = text.Replace("\r\n", "\n").Split('\n');

            int initialSubtitleCount = FindSubtitleCount(array) + 1; // Add one in case the counter started at 0

            if (initialSubtitleCount <= 0) // If we couldn't find the count fallback to manual calculation
                initialSubtitleCount = array.Length / 4 + 1;

            _dataText = new string[initialSubtitleCount];
            _dataStart = new float[initialSubtitleCount];
            _dataEnd = new float[initialSubtitleCount];

            int parserState = 0;
            int currentIndex = 0;
            int actualSubtitleCount = 0;
            foreach (var line in array)
            {
                if (parserState == 0 && line.Contains(" --> "))
                {
                    int arrowPos = line.IndexOf(" --> ");
                    string startStr = line.Substring(0, arrowPos);
                    string endStr = line.Substring(arrowPos + 5);
                    float startSecond = ParseTimestamp(startStr);
                    float endSecond = ParseTimestamp(endStr);

                    _dataStart[currentIndex] = startSecond;
                    _dataEnd[currentIndex] = endSecond;
                    _dataText[currentIndex] = "";

                    actualSubtitleCount += 1;
                    parserState = 1;
                }
                else if (parserState == 1 && line != "")
                {
                    _dataText[currentIndex] = line;
                    parserState = 2;
                }
                else if (parserState == 2 && line != "")
                {
                    _dataText[currentIndex] += "\n" + line;
                }
                else if (parserState != 0 && line == "")
                {
                    currentIndex++;
                    parserState = 0;
                }
            }

            _dataTotal = actualSubtitleCount;

            LogMessage($"Parsed {_dataTotal} subtitle groups");

            return _dataTotal > 0;
        }

        private int FindSubtitleCount(string[] array)
        {
            for (int i = array.Length - 1; i >= 1; i--)
            {
                if (array[i].Contains(" --> "))
                {
                    if (IsNumeric(array[i - 1]))
                        return int.Parse(array[i - 1]);

                    break;
                }
            }

            return -1;
        }

        private bool IsNumeric(string number)
        {
#pragma warning disable IDE0018, IDE0059 // UdonSharp does not currently support node type DeclarationExpression
            int n;
            return int.TryParse(number, out n);
#pragma warning restore IDE0018, IDE0059
        }

        private float ParseTimestamp(string timestamp)
        {
            string[] allParts = timestamp.Split(':');

            if (allParts.Length != 3) return 0;

            string[] secondsPart = allParts[2].Replace('.', ',').Split(','); // Sometimes instead of comma we have a dot, this change also makes VTT files parsable
            float milliseconds = secondsPart.Length == 0 ? 0f : (int.Parse(secondsPart[1]) * 0.001f);

            return int.Parse(allParts[0]) * 3600 + int.Parse(allParts[1]) * 60 + int.Parse(secondsPart[0]) + milliseconds;
        }

        public void ProcessInput(string input)
        {
            LogMessage("Loaded input of length " + input.Length);

            if (!_isLocal)
            {
                if (!targetVideoPlayer.CanControlVideoPlayer())
                {
                    string statusText = string.Format(@MESSAGE_ONLY_OWNER_CAN, Networking.GetOwner(gameObject).displayName, MESSAGE_ONLY_ACTION_ADD);

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetTemporaryStatusText(statusText, 3.0f);

                    return;
                }
            }

            LoadSubtitles(input, true);

            if (_dataTotal > 0)
            {
                if (!_isLocal)
                    SetAndTransmitSubtitles(input); // Synchronize to others only if the input is valid
                else
                    _dataLocal = input;
            }

            ResetSubtitleTrackingState();
        }

        private void SetAndTransmitSubtitles(string text)
        {
            TakeOwnership();

            _data = text;
            syncId = Networking.GetServerTimeInMilliseconds();

            TransmitSubtitles();
        }

        private void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SetOwner(Networking.LocalPlayer.displayName);
        }

        private void LogMessage(string message)
        {
            Debug.Log(LOG_PREFIX + " " + message, this);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning(LOG_PREFIX + " " + message, this);
        }

        private void LogError(string message)
        {
            Debug.LogError(LOG_PREFIX + " " + message, this);
        }

        public bool IsEnabled()
        {
            return _isEnabled;
        }

        public void SetEnabled(bool state)
        {
            if (state)
            {
                _isEnabled = true;
            }
            else
            {
                _isEnabled = false;
                if (_overlayHandler) _overlayHandler.ClearSubtitle();
            }
        }

        public bool IsLocal()
        {
            return _isLocal;
        }

        public void SetLocal(bool state)
        {
            _isLocal = state;

            if (state)
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetOwner(Networking.LocalPlayer.displayName + " " + INDICATOR_LOCAL);

                if (_dataLocal != "")
                    LoadSubtitles(_dataLocal, false);
            }
            else
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SetOwner(Networking.GetOwner(gameObject).displayName);

                if (_data != "")
                    LoadSubtitles(_data, false);
            }
        }

        public void SetPlaceholder(bool state)
        {
            _showPlaceholder = state;
        }

        public void ClearSubtitles()
        {
            if (_dataTotal == 0) return;

            if (!_isLocal)
            {
                if (_chunkSync < _chunkCount)
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStickyStatusText(MESSAGE_WAIT_SYNC, 3.0f);

                    return;
                }

                if (!targetVideoPlayer.CanControlVideoPlayer())
                {
                    string statusText = string.Format(@MESSAGE_ONLY_OWNER_CAN, Networking.GetOwner(gameObject).displayName, MESSAGE_ONLY_ACTION_CLEAR);

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetTemporaryStatusText(statusText, 3.0f);

                    return;
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
                if (_chunkSync < _chunkCount)
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStickyStatusText(MESSAGE_WAIT_SYNC, 3.0f);

                    return;
                }

                if (_data == "")
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(MESSAGE_NOT_LOADED);

                    return;
                }

                if (Networking.IsOwner(gameObject)) // Owner is guranteed to have the subtitles
                {
                    TransmitSubtitles();
                }
                else
                {
                    string statusText = string.Format(@MESSAGE_ONLY_OWNER_CAN, Networking.GetOwner(gameObject).displayName, MESSAGE_ONLY_ACTION_SYNC);

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetTemporaryStatusText(statusText, 3.0f);

                    ParseSubtitles(_data); // Just in case something went wrong and subtitles didn't load in SetLocal()
                }
            }

            ResetSubtitleTrackingState();
        }

        public void OnUSharpVideoPlay()
        {
            if (targetVideoPlayer.GetCurrentURL() != _lastVideoURL)
            {
                _lastVideoURL = targetVideoPlayer.GetCurrentURL();

                if (clearOnNewVideo && _data != "" && Networking.IsMaster)
                {
                    LogMessage("New URL detected, clearing subtitles...");

                    SetAndTransmitSubtitles("");

                    if (!_isLocal) // Prevents clearing of local subtitles
                        ClearSubtitlesLocal();
                }
            }
        }

        public void OnUSharpVideoReload()
        {
            ResetSubtitleTrackingState();
        }

        public void OnUSharpVideoLockChange()
        {
            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SynchronizeLockState();
        }

        public void OnUSharpVideoOwnershipChange()
        {
            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SynchronizeLockState();
        }
    }
}
