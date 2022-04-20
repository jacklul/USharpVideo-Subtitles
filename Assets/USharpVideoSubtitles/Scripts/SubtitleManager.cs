﻿/*
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
using VRC.SDK3.Video.Components.Base;

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
        private const string MESSAGE_SYNCHRONIZING = "Synchronizing {0} / {1} {2}";

        [SerializeField]
        private USharpVideoPlayer targetVideoPlayer;

        [SerializeField]
        private BaseVRCVideoPlayer baseVideoPlayer;

        [Header("Settings")]

        [SerializeField, Range(5000, 50000), Tooltip("Maximum size of a single data chunk when synchronizing the subtitles to others - big chunk sizes can make synchronization fail")]
        private int chunkSize = 10000;

        [Range(10, 255), Tooltip("How many frames to wait before the next subtitle update - higher values decrease time accuracy of the subtitles but could increase game performance\nThe default is fine")]
        public int updateRate = 10;

        [Tooltip("Should we automatically clear loaded subtitles when a new video starts?\nThis setting only works when using USharpVideo")]
        public bool clearOnNewVideo = false;

        [SerializeField, Tooltip("When false then only the master can manage the subtitles\nThis setting does nothing when using USharpVideo as the lock state is shared with it")]
        private bool defaultUnlocked = true;

        [UdonSynced]
        private string _syncedChunk;
        [UdonSynced]
        private int _syncId;

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

        [UdonSynced]
        private bool _isLocked = true; // Does nothing when USharpVideo is used
        private bool _lastLocked;
        
        private VideoPlayerManager _videoManager;
        private SubtitleOverlayHandler _overlayHandler;
        private SubtitleControlHandler[] _registeredControlHandlers;
        private UdonSharpBehaviour[] _registeredCallbackReceivers;

        private int _lastUpdateFrame = 0;
        private int _currentDataIndex = 0;
        private float _lastVideoTime = 0;
        private VRCUrl _lastVideoURL = VRCUrl.Empty;
        private VRCPlayerApi _currentOwner;
        private VRCPlayerApi _previousOwner;

        private void OnEnable()
        {
            if (targetVideoPlayer) targetVideoPlayer.RegisterCallbackReceiver(this);
        }

        private void Start()
        {
            if (!targetVideoPlayer && !baseVideoPlayer)
                LogWarning("No video player reference assigned!");

            if (targetVideoPlayer && baseVideoPlayer)
                LogWarning("You cannot reference USharpVideo and Unity or AVPro Video Player at the same time - USharpVideo takes precedence!");

            if (_registeredControlHandlers == null)
                _registeredControlHandlers = new SubtitleControlHandler[0];

            if (_registeredCallbackReceivers == null)
                _registeredCallbackReceivers = new UdonSharpBehaviour[0];
            
            if (targetVideoPlayer)
            {
                _videoManager = targetVideoPlayer.GetVideoManager();
                
                if (Networking.IsOwner(gameObject))
                    SendCustomEventDelayedFrames(nameof(OnUSharpVideoLockChange), 1); // The event will be initially triggered only for non-master players, this fixes the issue with wrong lock state on master player
            }
            else
            {
                if (Networking.IsOwner(gameObject))
                {
                    _isLocked = !defaultUnlocked;
                    
                    SendCustomEventDelayedFrames(nameof(_QueueSerialize), 1); // Send lock state to everyone as initial state

                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SynchronizeLockState();
                }

                _lastLocked = _isLocked;
            }
            
            _previousOwner = Networking.GetOwner(gameObject);
            _currentOwner = _previousOwner;

            ResetSubtitleTrackingState();
        }

        private void OnDisable()
        {
            if (targetVideoPlayer) targetVideoPlayer.UnregisterCallbackReceiver(this);
        }

        public void Update()
        {
            if (!_isEnabled || _dataTotal == 0)
                return;

            if (_lastUpdateFrame < updateRate)
            {
                _lastUpdateFrame++;
                return;
            }
            _lastUpdateFrame = 0;

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

        private bool IsVideoPlayerPlaying()
        {
            if (_videoManager)
                return _videoManager.IsPlaying();

            if (baseVideoPlayer)
                return baseVideoPlayer.IsPlaying;

            return false;
        }

        private float GetVideoTime()
        {
            if (_videoManager)
                return _videoManager.GetTime();

            if (baseVideoPlayer)
                return baseVideoPlayer.GetTime();

            return 0.0f;
        }

        private void TransmitSubtitles()
        {
            if (!Networking.IsOwner(gameObject))
                return;

            if (VRCPlayerApi.GetPlayerCount() == 1) // No point to even attempt to synchronize when alone in the instance
                return;

            LogMessage($"Transmitting subtitles... (length = {_data.Length})");

            _chunkCount = _data.Length / chunkSize + 1;
            _chunkSync = 0;

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

                int start = Mathf.Min(_chunkSync * chunkSize, _data.Length);
                int length = Mathf.Min(chunkSize, _data.Length - _chunkSync * chunkSize);

                if (length < 0) // This can happen when master presses the lock button at the same time as someone else is starting to send the subtitles, this will also bug out UI for both players (toggling local mode cleans up the UI)
                    length = chunkSize;

                _syncedChunk = _data.Substring(start, length);
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

            if (!targetVideoPlayer && _lastLocked != _isLocked)
            {
                _lastLocked = _isLocked;

                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                    handler.SynchronizeLockState();

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
                _data = _syncedChunk;
            }
            else if (_localChunkSync == _chunkSync - 1)
            {
                _localChunkSync++;
                _data += _syncedChunk;
            }
            else
                LogWarning($"Rejected chunk {_chunkSync + 1} because local chunk is {_localChunkSync}");

            if (_localChunkSync == _chunkCount - 1)
            {
                _lastSyncId = _syncId;

                LogMessage($"Received all chunks");

                if (!_isLocal)
                {
                    if (_data.Length == 0)
                    {
                        ClearSubtitlesLocal();
                        return;
                    }

                    LogMessage($"Applying synchronized data (length = {_data.Length})");

                    LoadSubtitles(_data, false);
                    ResetSubtitleTrackingState();
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

        private void ClearSubtitlesLocal()
        {
            LogMessage("Clearing subtitles locally");

            _dataText = new string[0];
            _dataStart = new float[0];
            _dataEnd = new float[0];
            _dataTotal = 0;

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

                    SendCallback("OnUSharpVideoSubtitlesLoad");
                }
                else
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                        handler.SetStatusText(MESSAGE_FAILED);
                    
                    SendCallback("OnUSharpVideoSubtitlesError");
                }
            }
            else
                LogError("Requested to load empty data - this shouldn't happen");
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

        private bool ParseSubtitles(string text)
        {
            string[] array = text.Replace("\r\n", "\n").Split('\n');

            int initialSubtitleCount = FindSubtitleCount(array) + 1; // Add one in case the counter started at 0

            if (initialSubtitleCount <= 0) // If we couldn't find the count fallback to manual calculation
                initialSubtitleCount = array.Length / 3 + 1; // File might not have a counter and when dividing by 4 we will run out of array space

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

                    if (endStr.Contains(" ")) // Per SRT specs there can be text coordinates after the timestamp and we can't support that
                        endStr = endStr.Split(' ')[0];

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
                    _dataText[currentIndex] = ProcessText(line);
                    parserState = 2;
                }
                else if (parserState == 2 && line != "")
                {
                    _dataText[currentIndex] += "\n" + ProcessText(line);
                }
                else if (parserState != 0 && line == "")
                {
                    currentIndex++;
                    parserState = 0;
                }

                if (currentIndex > initialSubtitleCount - 1) // Prevent a crash when exceeding the max index
                {
                    LogError($"Ran out of space in data array ({currentIndex})");
                    return false;
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
            int n;
            return int.TryParse(number, out n);
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

        private string ProcessText(string text)
        {
            if (text.Contains("<font "))
                text = text.Replace("<font  ", "<").Replace("<font ", "<").Replace("</font>", "</color>").Replace("=\"", "=").Replace("\">", ">");

            return text;
        }

        public void ProcessInput(string input)
        {
            if (!_isLocal && !CanControlSubtitles())
                return;

            LogMessage("Loaded input of length " + input.Length);

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
            _syncId = Networking.GetServerTimeInMilliseconds();
            _lastSyncId = _syncId;

            TransmitSubtitles();
        }

        private void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject))
                return;

            if (CanControlSubtitles())
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
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

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject) && _data != "")
                TransmitSubtitles();
        }
        
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject) && player == _previousOwner && !IsSynchronized() && IsSameSyncId()) // Player who left was running the synchronization, resume it as we have all the data
            {
                if (!_isLocal)
                {
                    foreach (SubtitleControlHandler handler in _registeredControlHandlers) // This will prevent the status being stuck at "synchronizing last chunk"
                    {
                        if (_data != "")
                            handler.SetStatusText(MESSAGE_LOADED);
                        else
                            handler.SetStatusText(MESSAGE_NOT_LOADED);
                            
                        handler.SaveStatusText();
                    }
                }
                    
                RequestSerialization();
            }
        }

        // Similary to how it is in USharpVideo - uncomment this to prevent people from taking ownership when they shouldn't be able to
        //public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        //{
        //    if (targetVideoPlayer)
        //        return !targetVideoPlayer.IsLocked() || targetVideoPlayer.IsPrivilegedUser(requestedOwner);
        //    
        //    return !_isLocked || IsPrivilegedUser(requestedOwner);
        //}

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            _previousOwner = _currentOwner;
            _currentOwner = Networking.GetOwner(gameObject);

            if (!_isLocal)
            {
                foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                {
                    handler.UpdateOwner();
                    handler.SynchronizeLockState();
                }
            }
            
            SendCallback("OnUSharpVideoSubtitlesOwnershipChange");
        }

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

        public bool IsLocked()
        {
            if (targetVideoPlayer)
                return targetVideoPlayer.IsLocked();

            return _isLocked;
        }

        public void SetLocked(bool state)
        {
            if (!IsPrivilegedUser(Networking.LocalPlayer))
                return;

            if (targetVideoPlayer)
            {
                LogError("Method SetLocked cannot be used while using USharpVideo");
                return;
            }

            if (!IsSynchronized() && !Networking.IsOwner(gameObject)) // Prevent locking when someone else is redistributing the subtitles as this would break the sync for everyone
                return;

            TakeOwnership();

            _isLocked = state;
            _lastLocked = _isLocked;

            if (IsSynchronized()) // We don't have to call this when the synchronization is still going
                RequestSerialization();

            foreach (SubtitleControlHandler handler in _registeredControlHandlers)
                handler.SynchronizeLockState();
            
            SendCallback("OnUSharpVideoSubtitlesLockChange");
        }

        public bool CanControlSubtitles()
        {
            if (targetVideoPlayer)
                return targetVideoPlayer.CanControlVideoPlayer();

            return !_isLocked || IsPrivilegedUser(Networking.LocalPlayer);
        }
        
        public bool IsPrivilegedUser(VRCPlayerApi player)
        {
            if (targetVideoPlayer)
                return targetVideoPlayer.IsPrivilegedUser(player);

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

            if (state)
            {
                if (_dataLocal != "")
                    LoadSubtitles(_dataLocal, false);
                else
                    UnsetSubtitlesLocal();
            }
            else
            {
                if (_data != "")
                {
                    if (IsSynchronized())
                        LoadSubtitles(_data, false);
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
                handler.SynchronizeLockState();

            SendCallback("OnUSharpVideoSubtitlesModeChange");
        }

        public bool HasSubtitles()
        {
            return _dataTotal > 0;
        }

        public void SetVideoPlayer(BaseVRCVideoPlayer videoPlayer)
        {
            if (!targetVideoPlayer)
            {
                baseVideoPlayer = videoPlayer;

                ResetSubtitleTrackingState();

                SendCallback("OnUSharpVideoSubtitlesVideoPlayerChange");
            }
            else
                LogWarning("Method SetVideoPlayer cannot be used with USharpVideo");
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
                if (_data == "")
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

        public void OnUSharpVideoPlay()
        {
            VRCUrl currentURL = targetVideoPlayer.GetCurrentURL();

            if (currentURL != _lastVideoURL)
            {
                _lastVideoURL = currentURL;

                if (clearOnNewVideo && _data != "" && Networking.IsMaster)
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
                handler.SynchronizeLockState();

            SendCallback("OnUSharpVideoSubtitlesLockChange");

            // Uncomment this if you want to force the owner of this object to be whoever owns the video player when this callback triggers (this will usually be the master or instance creator)
            //_MigrateToUSharpVideoOwner();
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
                    handler.SynchronizeLockState();
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
            newControlHandler.SetStatusText(_dataTotal > 0 ? MESSAGE_LOADED : MESSAGE_NOT_LOADED);
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
            if (!Utilities.IsValid(callbackReceiver))
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
            if (!Utilities.IsValid(callbackReceiver))
                return;

            if (_registeredCallbackReceivers == null)
                _registeredCallbackReceivers = new UdonSharpBehaviour[0];

            int callbackReceiverCount = _registeredControlHandlers.Length;
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
                if (Utilities.IsValid(callbackReceiver))
                    callbackReceiver.SendCustomEvent(callbackName);
            }
        }
    }
}
