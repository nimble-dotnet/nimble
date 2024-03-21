/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Text.RegularExpressions;
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

        static string SimpleMarkdown(string input)
        {
            const string pattern = @"\*{1,2}(.*?)\*{1,2}";

            return Regex.Replace(input, pattern, m =>
            {
                if (m.Value.StartsWith("*"))
                {
                    return $"<b>{m.Groups[1].Value}</b>";
                }
                else
                {
                    return $"<color=green>{m.Groups[1].Value}</color>";
                }
            });
        }

        public void Log(LogLevel level, string prefix, string message, object[] args)
        {
            var replacedMessage = SimpleMarkdown(message);
            var messageWithValues = ArgumentReplace.ReplaceArgumentsWithValues(replacedMessage, args);

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