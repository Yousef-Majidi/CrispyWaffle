﻿// ***********************************************************************
// Assembly         : CrispyWaffle.Log4Net
// Author           : Guilherme Branco Stracini
// Created          : 09-04-2020
//
// Last Modified By : Guilherme Branco Stracini
// Last Modified On : 09-04-2020
// ***********************************************************************
// <copyright file="Log4NetLogProvider.cs" company="Guilherme Branco Stracini ME">
//     Copyright (c) Guilherme Branco Stracini ME. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace CrispyWaffle.Log4Net
{
    using Log;
    using Log.Providers;
    using Serialization;
    using System;
    using log4net;

    /// <summary>
    /// The Log4Net log provider
    /// </summary>
    /// <seealso cref="ILogProvider" />
    public sealed class Log4NetLogProvider : ILogProvider
    {
        #region Private fields

        /// <summary>
        /// The level
        /// </summary>
        private LogLevel _level;

        /// <summary>
        /// The adapter
        /// </summary>
        private readonly ILog _adapter;

        #endregion

        #region ~Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetLogProvider" /> class.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        public Log4NetLogProvider(ILog adapter)
        {
            _adapter = adapter;
        }

        #endregion

        #region Implementation of ILogProvider

        /// <summary>
        /// Sets the log level of the instance
        /// </summary>
        /// <param name="level">The log level</param>
        public void SetLevel(LogLevel level)
        {
            _level = level;
        }

        /// <summary>
        /// Fatals the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public void Fatal(string category, string message)
        {
            if (_level.HasFlag(LogLevel.Fatal))
            {
                _adapter.Fatal(message);
            }
        }

        /// <summary>
        /// Logs the message with error level
        /// </summary>
        /// <param name="category">The category</param>
        /// <param name="message">The message to be logged</param>
        public void Error(string category, string message)
        {
            if (_level.HasFlag(LogLevel.Error))
            {
                _adapter.Error(message);
            }
        }

        /// <summary>
        /// Logs the message with warning level
        /// </summary>
        /// <param name="category">The category</param>
        /// <param name="message">The message to be logged</param>
        public void Warning(string category, string message)
        {
            if (_level.HasFlag(LogLevel.Warning))
            {
                _adapter.Warn(message);
            }
        }

        /// <summary>
        /// Logs the message with info level
        /// </summary>
        /// <param name="category">The category</param>
        /// <param name="message">The message to be logged</param>
        public void Info(string category, string message)
        {
            if (_level.HasFlag(LogLevel.Info))
            {
                _adapter.Info(message);
            }
        }

        /// <summary>
        /// Logs the message with trace level
        /// </summary>
        /// <param name="category">The category</param>
        /// <param name="message">The message to be logged</param>
        public void Trace(string category, string message)
        {
            if (_level.HasFlag(LogLevel.Trace))
            {
                _adapter.Info(message);
            }
        }

        /// <summary>
        /// Traces the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Trace(string category, string message, Exception exception)
        {
            if (!_level.HasFlag(LogLevel.Trace))
            {
                return;
            }

            _adapter.Info(message);
            do
            {
                _adapter.Info(exception.Message);
                _adapter.Info(exception.StackTrace);

                exception = exception.InnerException;
            } while (exception != null);
        }

        /// <summary>
        /// Traces the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="exception">The exception.</param>
        public void Trace(string category, Exception exception)
        {
            if (!_level.HasFlag(LogLevel.Trace))
            {
                return;
            }

            do
            {
                _adapter.Info(exception.Message);
                _adapter.Info(exception.StackTrace);

                exception = exception.InnerException;
            } while (exception != null);
        }

        /// <summary>
        /// Logs the message with debug level
        /// </summary>
        /// <param name="category">The category</param>
        /// <param name="message">The message to be logged</param>
        public void Debug(string category, string message)
        {
            if (_level.HasFlag(LogLevel.Debug))
            {
                _adapter.Debug(message);
            }
        }

        /// <summary>
        /// Logs the message as a file/attachment with a file name/identifier with debug level
        /// </summary>
        /// <param name="category">The category</param>
        /// <param name="content">The content to be stored</param>
        /// <param name="identifier">The file name of the content. This can be a filename, a key, a identifier. Depends upon each implementation</param>
        public void Debug(string category, string content, string identifier)
        {
            if (!_level.HasFlag(LogLevel.Debug))
            {
                return;
            }

            _adapter.Debug(identifier);
            _adapter.Debug(content);
        }

        /// <summary>
        /// Logs the message as a file/attachment with a file name/identifier with debug level using a custom serializer or default.
        /// </summary>
        /// <typeparam name="T">any class that can be serialized to the <paramref name="customFormat" /> serializer format</typeparam>
        /// <param name="category">The category</param>
        /// <param name="content">The object to be serialized</param>
        /// <param name="identifier">The filename/attachment identifier (file name or key)</param>
        /// <param name="customFormat">(Optional) the custom serializer format</param>
        public void Debug<T>(
            string category,
            T content,
            string identifier,
            SerializerFormat customFormat = SerializerFormat.None
        )
            where T : class, new()
        {
            if (!_level.HasFlag(LogLevel.Debug))
            {
                return;
            }

            _adapter.Debug(identifier);
            if (customFormat == SerializerFormat.None)
            {
                _adapter.Debug((string)content.GetSerializer());
            }
            else
            {
                _adapter.Debug((string)content.GetCustomSerializer(customFormat));
            }
        }

        #endregion
    }
}
