/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions)
 *
 * MIT License
 *
 * Copyright (c) 2019 Andreas Truetschel and contributors.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * --------------------------------------------------------------------------------------------------------------------
 */

/* Based on
 * --------------------------------------------------------------------------------------------------------------------
 * BlazorSignalR (https://github.com/csnewman/BlazorSignalR)
 *
 * MIT License
 *
 * Copyright (c) 2018 csnewman
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AI4E.AspNetCore.Blazor.SignalR
{
    internal static class SendUtils
    {
        public static async Task SendMessages(Uri sendUrl, IDuplexPipe application, HttpClient httpClient, ILogger? logger, CancellationToken cancellationToken = default)
        {
            Log.SendStarted(logger);

            try
            {
                while (true)
                {
                    var result = await application.Input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (result.IsCanceled)
                        {
                            Log.SendCanceled(logger);
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            Log.SendingMessages(logger, buffer.Length, sendUrl);

                            // Send them in a single post
                            var request = new HttpRequestMessage(HttpMethod.Post, sendUrl)
                            {
                                // Corefx changed the default version and High Sierra curlhandler tries to upgrade request
                                Version = new Version(1, 1),
                                Content = new ReadOnlySequenceContent(buffer)
                            };

                            // ResponseHeadersRead instructs SendAsync to return once headers are read
                            // rather than buffer the entire response. This gives a small perf boost.
                            // Note that it is important to dispose of the response when doing this to
                            // avoid leaving the connection open.
                            using (var response = await httpClient
                                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                .ConfigureAwait(false))
                            {
                                response.EnsureSuccessStatusCode();
                            }

                            Log.SentSuccessfully(logger);
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                        else
                        {
                            Log.NoMessages(logger);
                        }
                    }
                    finally
                    {
                        application.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.SendCanceled(logger);
            }
            catch (Exception ex)
            {
                Log.ErrorSending(logger, sendUrl, ex);
                throw;
            }
            finally
            {
                application.Input.Complete();
            }

            Log.SendStopped(logger);
        }

        private class ReadOnlySequenceContent : HttpContent
        {
            private readonly ReadOnlySequence<byte> _buffer;

            public ReadOnlySequenceContent(in ReadOnlySequence<byte> buffer)
            {
                _buffer = buffer;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return stream.WriteAsync(_buffer).AsTask();
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _buffer.Length;
                return true;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception?> SendStartedMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(100, "SendStarted"), "Starting the send loop.");

            private static readonly Action<ILogger, Exception?> SendStoppedMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(101, "SendStopped"), "Send loop stopped.");

            private static readonly Action<ILogger, Exception?> SendCanceledMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(102, "SendCanceled"), "Send loop canceled.");

            private static readonly Action<ILogger, long, Uri, Exception?> SendingMessagesMessage =
                LoggerMessage.Define<long, Uri>(LogLevel.Debug, new EventId(103, "SendingMessages"), "Sending {Count} bytes to the server using url: {Url}.");

            private static readonly Action<ILogger, Exception?> SentSuccessfullyMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(104, "SentSuccessfully"), "Message(s) sent successfully.");

            private static readonly Action<ILogger, Exception?> NoMessagesMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(105, "NoMessages"), "No messages in batch to send.");

            private static readonly Action<ILogger, Uri, Exception?> ErrorSendingMessage =
                LoggerMessage.Define<Uri>(LogLevel.Error, new EventId(106, "ErrorSending"), "Error while sending to '{Url}'.");

            // When adding a new log message make sure to check with LongPollingTransport and ServerSentEventsTransport that share these logs to not have conflicting EventIds
            // We start the IDs at 100 to make it easy to avoid conflicting IDs

            public static void SendStarted(ILogger? logger)
            {
                if (logger is null)
                    return;

                SendStartedMessage(logger, null);
            }

            public static void SendCanceled(ILogger? logger)
            {
                if (logger is null)
                    return;

                SendCanceledMessage(logger, null);
            }

            public static void SendStopped(ILogger? logger)
            {
                if (logger is null)
                    return;

                SendStoppedMessage(logger, null);
            }

            public static void SendingMessages(ILogger? logger, long count, Uri url)
            {
                if (logger is null)
                    return;

                SendingMessagesMessage(logger, count, url, null);
            }

            public static void SentSuccessfully(ILogger? logger)
            {
                if (logger is null)
                    return;

                SentSuccessfullyMessage(logger, null);
            }

            public static void NoMessages(ILogger? logger)
            {
                if (logger is null)
                    return;

                NoMessagesMessage(logger, null);
            }

            public static void ErrorSending(ILogger? logger, Uri url, Exception? exception)
            {
                if (logger is null)
                    return;

                ErrorSendingMessage(logger, url, exception);
            }
        }
    }
}
