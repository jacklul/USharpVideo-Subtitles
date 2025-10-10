/*
 * Copyright (c) Jack'lul <https://jacklul.github.io>
 * Licensed under the MIT License
 * https://github.com/jacklul/USharpVideo-Subtitles
 */

using UnityEngine;

namespace UdonSharp.Video.Subtitles
{
    public class Common : MonoBehaviour
    {
        public static string ToRGBHex(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", ToByte(color.r), ToByte(color.g), ToByte(color.b));
        }

        public static byte ToByte(float number)
        {
            number = Mathf.Clamp01(number);
            return (byte)(number * 255);
        }

        public static int SafelyParseInt(string number)
        {
            int n;
            if (int.TryParse(number, out n))
                return n;

            return 0;
        }

        public static float SafelyParseFloat(string number)
        {
            string[] tmp = number.Replace('.', ',').Split(',');

            // A hack to let us parse string floats with both comma and dot no matter if running in Unity or in VRC (locale matters)
            if (tmp.Length > 1)
                return SafelyParseInt(tmp[0]) + (SafelyParseInt(tmp[1]) / Mathf.Pow(10, tmp[1].Length)); 

            float n;
            if (float.TryParse(tmp[0], out n))
                return n;

            return 0f;
        }

        public static float RoundFloat(float value, int decimals)
        {
            if (decimals == 0)
                return Mathf.Round(value);

            float n = Mathf.Pow(10, decimals);
            return Mathf.Round(value * n) / n;
        }
    }
}
