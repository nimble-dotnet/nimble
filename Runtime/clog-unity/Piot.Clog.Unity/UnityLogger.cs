/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using UnityEngine;

namespace Piot.Clog.Unity
{
    public class UnityLogger : ILogTarget
    {
        static string Color(string message, string color, bool useBold)
        {
            var colorString = $"<color={color}>{message}</color>";
            return useBold ? $"<b>{colorString}</b>" : colorString;
        }

        static string ColorMessage(LogLevel level, string message)
        {
            if (level != LogLevel.Notice)
            {
                return message;
            }

            const string noticeColorName = "#ffca7a";

            return Color(message, noticeColorName, true);
        }

        public void Log(LogLevel level, string prefix, string message, object[] args)
        {
            var messageWithValues = ArgumentReplace.ReplaceArgumentsWithValues(message, args);

            var msg = $"<b>{prefix}</b>: {ColorMessage(level, messageWithValues)}";

            switch (level)
            {
                case LogLevel.LowLevel:
                    Debug.Log(msg);
                    break;
                case LogLevel.Debug:
                    Debug.Log(msg);
                    break;
                case LogLevel.Info:
                    Debug.Log(msg);
                    break;
                case LogLevel.Notice:
                    Debug.Log(msg);
                    break;
                case LogLevel.Error:
                    Debug.LogError(msg);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(msg);
                    break;
                default:
                    throw new Exception("unknown log level");
            }
        }
    }
}