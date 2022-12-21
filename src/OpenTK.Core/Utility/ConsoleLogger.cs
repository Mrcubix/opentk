﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK.Core.Utility
{
    /// <summary>
    /// A simple console logger that will write out log messages to the console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <inheritdoc/>
        public LogLevel Filter { get; set; }

        void ILogger.LogInternal(string str, LogLevel level, string filePath, int line, string member)
        {
            if (level < Filter)
            {
                return;
            }

            TextWriter writer = level switch
            {
                LogLevel.Debug => Console.Out,
                LogLevel.Info => Console.Out,
                LogLevel.Warning => Console.Out,
                LogLevel.Error => Console.Error,
                _ => Console.Out,
            };

            string fileName = Path.GetFileName(filePath);

            // Example format:
            // [Warning] Method ConsoleLogger.cs:123 <Message>
            writer.Write($"[{level}] {member} {fileName}:{line} ");
            writer.WriteLine(str);
        }
    }
}