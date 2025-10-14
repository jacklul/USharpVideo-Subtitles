/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using UnityEngine;

namespace UdonSharp.Video.Subtitles
{
    // This is just a stub, you must implement Log, LogWarning, LogError methods
    public abstract class Logger : UdonSharpBehaviour
    {
        public virtual void Log(string message, Object context)
        {
            //Debug.Log(message, context);
        }

        public virtual void LogWarning(string message, Object context)
        {
            //Debug.LogWarning(message, context);
        }

        public virtual void LogError(string message, Object context)
        {
            //Debug.LogError(message, context);
        }
    }
}
