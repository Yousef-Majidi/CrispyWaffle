﻿// ***********************************************************************
// Assembly         : CrispyWaffle.RabbitMQ
// Author           : Guilherme Branco Stracini
// Created          : 03-31-2021
//
// Last Modified By : Guilherme Branco Stracini
// Last Modified On : 05-05-2021
// ***********************************************************************
// <copyright file="RabbitMqLogProvider.cs" company="Guilherme Branco Stracini ME">
//     © 2020 Guilherme Branco Stracini. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace CrispyWaffle.RabbitMQ.Log
{
    using Extensions;
    using Infrastructure;
    using CrispyWaffle.Log;
    using CrispyWaffle.Log.Providers;
    using Utils.Communications;
    using Serialization;
    using global::RabbitMQ.Client;
    using System;
    using System.Collections.Concurrent;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Class RabbitMQLogProvider.
    /// Implements the <see cref="CrispyWaffle.Log.Providers.ILogProvider" />
    /// Implements the <see cref="System.IDisposable" />
    /// </summary>
    /// <seealso cref="CrispyWaffle.Log.Providers.ILogProvider" />
    /// <seealso cref="System.IDisposable" />
    public class RabbitMQLogProvider : ILogProvider, IDisposable
    {
        #region Private fields

        /// <summary>
        /// The level
        /// </summary>
        private LogLevel _level;

        /// <summary>
        /// The connector
        /// </summary>
        private readonly RabbitMQConnector _connector;

        /// <summary>
        /// The channel
        /// </summary>
        private readonly IModel _channel;

        /// <summary>
        /// The cancellation token
        /// </summary>
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// The queue
        /// </summary>
        private readonly ConcurrentQueue<string> _queue;

        #endregion

        #region ~Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQLogProvider" /> class.
        /// </summary>
        /// <param name="connector">The connector.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">connector</exception>
        public RabbitMQLogProvider(RabbitMQConnector connector, CancellationToken cancellationToken)
        {
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _channel = _connector.ConnectionFactory.CreateConnection().CreateModel();
            _channel.ExchangeDeclare(_connector.DefaultExchangeName, ExchangeType.Fanout, true);
            _cancellationToken = cancellationToken;
            _queue = new ConcurrentQueue<string>();
            var thread = new Thread(Worker);
            thread.Start();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RabbitMQLogProvider" /> class.
        /// </summary>
        ~RabbitMQLogProvider()
        {
            Dispose(false);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Workers this instance.
        /// </summary>
        private void Worker()
        {
            Thread.CurrentThread.Name = "Message queue RabbitMQ log provider worker";
            Thread.Sleep(1000);

            while (true)
            {
                while (_queue.Count > 0)
                {
                    if (!_queue.TryDequeue(out var message))
                    {
                        break;
                    }

                    PropagateMessageInternal(message);
                }

                if (_cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Propagates the message internal.
        /// </summary>
        /// <param name="message">The message.</param>
        private void PropagateMessageInternal(string message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message);
                _channel.BasicPublish(_connector.DefaultExchangeName, "", null, body);
            }
            catch (Exception e)
            {
                LogConsumer.Trace(e);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _channel.Close();
        }

        /// <summary>
        /// Serializes the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        /// <param name="identifier">The identifier.</param>
        /// <returns>System.String.</returns>
        private static string Serialize(
            LogLevel level,
            string category,
            string message,
            string identifier = null
        )
        {
            return (string)
                new LogMessage
                {
                    Application = EnvironmentHelper.ApplicationName,
                    Category = category,
                    Date = DateTime.Now,
                    Hostname = EnvironmentHelper.Host,
                    Id = Guid.NewGuid().ToString(),
                    IpAddress = EnvironmentHelper.IpAddress,
                    IpAddressRemote = EnvironmentHelper.IpAddressExternal,
                    Level = level.GetHumanReadableValue(),
                    Message = message,
                    MessageIdentifier = identifier,
                    Operation = EnvironmentHelper.Operation,
                    ProcessId = EnvironmentHelper.ProcessId,
                    UserAgent = EnvironmentHelper.UserAgent,
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                    ThreadName = Thread.CurrentThread.Name
                }.GetSerializer();
        }

        /// <summary>
        /// Propagates the internal.
        /// </summary>
        /// <param name="message">The message.</param>
        private void PropagateInternal(string message)
        {
            _queue.Enqueue(message);
        }

        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Implementation of ILogProvider

        /// <summary>
        /// Sets the level.
        /// </summary>
        /// <param name="level">The level.</param>
        public void SetLevel(LogLevel level)
        {
            _level = level;
        }

        /// <summary>
        /// Fatal the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public void Fatal(string category, string message)
        {
            if (!_level.HasFlag(LogLevel.Fatal))
            {
                return;
            }

            PropagateInternal(Serialize(LogLevel.Fatal, category, message));
        }

        /// <summary>
        /// Errors the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public void Error(string category, string message)
        {
            if (!_level.HasFlag(LogLevel.Error))
            {
                return;
            }

            PropagateInternal(Serialize(LogLevel.Error, category, message));
        }

        /// <summary>
        /// Warnings the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public void Warning(string category, string message)
        {
            if (!_level.HasFlag(LogLevel.Warning))
            {
                return;
            }

            PropagateInternal(Serialize(LogLevel.Warning, category, message));
        }

        /// <summary>
        /// Information the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public void Info(string category, string message)
        {
            if (!_level.HasFlag(LogLevel.Info))
            {
                return;
            }

            PropagateInternal(Serialize(LogLevel.Info, category, message));
        }

        /// <summary>
        /// Traces the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public void Trace(string category, string message)
        {
            if (!_level.HasFlag(LogLevel.Trace))
            {
                return;
            }

            PropagateInternal(Serialize(LogLevel.Trace, category, message));
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

            PropagateInternal(Serialize(LogLevel.Trace, category, message));

            Trace(category, exception);
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
                PropagateInternal(Serialize(LogLevel.Trace, category, exception.Message));
                PropagateInternal(Serialize(LogLevel.Trace, category, exception.StackTrace));

                exception = exception.InnerException;
            } while (exception != null);
        }

        /// <summary>
        /// Debugs the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        public void Debug(string category, string message)
        {
            if (!_level.HasFlag(LogLevel.Debug))
            {
                return;
            }

            PropagateInternal(Serialize(LogLevel.Debug, category, message));
        }

        /// <summary>
        /// Debugs the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="content">The content.</param>
        /// <param name="identifier">The identifier.</param>
        public void Debug(string category, string content, string identifier)
        {
            if (!_level.HasFlag(LogLevel.Debug))
            {
                return;
            }

            PropagateInternal(Serialize(LogLevel.Debug, category, content, identifier));
        }

        /// <summary>
        /// Debugs the specified category.
        /// </summary>
        /// <typeparam name="T">any class that can be serialized to the <paramref name="customFormat" /> serializer format</typeparam>
        /// <param name="category">The category.</param>
        /// <param name="content">The content.</param>
        /// <param name="identifier">The identifier.</param>
        /// <param name="customFormat">The custom format.</param>
        public void Debug<T>(
            string category,
            T content,
            string identifier,
            SerializerFormat customFormat
        )
            where T : class, new()
        {
            if (!_level.HasFlag(LogLevel.Debug))
            {
                return;
            }

            string serialized;

            if (customFormat == SerializerFormat.None)
            {
                serialized = (string)content.GetSerializer();
            }
            else
            {
                serialized = (string)content.GetCustomSerializer(customFormat);
            }

            PropagateInternal(Serialize(LogLevel.Debug, category, serialized, identifier));
        }

        #endregion
    }
}
