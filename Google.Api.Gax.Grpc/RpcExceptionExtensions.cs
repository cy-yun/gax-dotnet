﻿/*
 * Copyright 2021 Google LLC
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file or at
 * https://developers.google.com/open-source/licenses/bsd
 */

using Google.Protobuf;
using Google.Rpc;
using Grpc.Core;
using System.Collections.Generic;
using System.Linq;
using Status = Google.Rpc.Status;

namespace Google.Api.Gax.Grpc
{
    /// <summary>
    /// Utility extension methods to make it easier to retrieve extended error information from an <see cref="RpcException"/>.
    /// </summary>
    public static class RpcExceptionExtensions
    {
        // Visible for testing, and for synthesis in ReadHttpResponseMessage.
        internal const string StatusDetailsTrailerName = "grpc-status-details-bin";

        /// <summary>
        /// Retrieves the <see cref="Status"/> message containing extended error information
        /// from the trailers in an <see cref="RpcException"/>, if present.
        /// </summary>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>The <see cref="Status"/> message specified in the exception, or null
        /// if there is no such information.</returns>
        public static Rpc.Status GetRpcStatus(this RpcException ex) =>
            DecodeTrailer(GetTrailer(ex, StatusDetailsTrailerName), Status.Parser);

        /// <summary>
        /// Retrieves the <see cref="BadRequest"/> message containing extended error information
        /// from the trailers in an <see cref="RpcException"/>, if present.
        /// </summary>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>The <see cref="BadRequest"/> message specified in the exception, or null
        /// if there is no such information.</returns>
        public static BadRequest GetBadRequest(this RpcException ex) => GetStatusDetail<BadRequest>(ex);

        /// <summary>
        /// Retrieves the <see cref="ErrorInfo"/> message containing extended error information
        /// from the trailers in an <see cref="RpcException"/>, if present.
        /// </summary>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>The <see cref="ErrorInfo"/> message specified in the exception, or null
        /// if there is no such information.</returns>
        public static ErrorInfo GetErrorInfo(this RpcException ex) => GetStatusDetail<ErrorInfo>(ex);

        /// <summary>
        /// Retrieves the <see cref="Help"/> message containing extended error information
        /// from the trailers in an <see cref="RpcException"/>, if present.
        /// </summary>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>The <see cref="Help"/> message specified in the exception, or null
        /// if there is no such information.</returns>
        public static Help GetHelp(this RpcException ex) => GetStatusDetail<Help>(ex);

        /// <summary>
        /// Retrieves the <see cref="LocalizedMessage"/> message containing extended error information
        /// from the trailers in an <see cref="RpcException"/>, if present.
        /// </summary>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>The <see cref="Help"/> message specified in the exception, or null
        /// if there is no such information.</returns>
        public static LocalizedMessage GetLocalizedMessage(this RpcException ex) => GetStatusDetail<LocalizedMessage>(ex);

        /// <summary>
        /// Retrieves the error details of type <typeparamref name="T"/> from the <see cref="Status"/>
        /// message associated with an <see cref="RpcException"/>, if any.
        /// </summary>
        /// <typeparam name="T">The message type to decode from within the error details.</typeparam>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>The requested status detail, or null if the exception does not contain one.</returns>
        public static T GetStatusDetail<T>(this RpcException ex) where T : class, IMessage<T>, new()
        {
            var status = GetRpcStatus(ex);
            try
            {
                return status.GetDetail<T>();
            }
            catch
            {
                // If the message is malformed, just report there's no information.
                return null;
            }
        }

        /// <summary>
        /// Returns all standard status details associated with an RPC exception.
        /// </summary>
        /// <remarks>
        /// Status details are assumed to be in the <see cref="StandardErrorTypeRegistry">standard error type registry</see>.
        /// If any detail messages are invalid (known, but containing invalid data) then iterating over the returned collection
        /// will throw <see cref="InvalidProtocolBufferException"/> when the iterator reaches the invalid message.
        /// </remarks>
        /// <param name="ex">The RPC exception to retrieve details from. Must not be null.</param>
        /// <returns>All standard status details within the exception.</returns>
        public static IEnumerable<IMessage> GetAllStatusDetails(this RpcException ex) =>
            GetRpcStatus(ex)?.UnpackDetailMessages() ?? Enumerable.Empty<IMessage>();

        private static Metadata.Entry GetTrailer(RpcException ex, string key)
        {
            GaxPreconditions.CheckNotNull(ex, nameof(ex));
            return ex.Trailers.FirstOrDefault(t => t.Key == key);
        }

        private static T DecodeTrailer<T>(Metadata.Entry entry, MessageParser<T> parser) where T : class, IMessage<T>
        {
            if (entry is null)
            {
                return null;
            }
            try
            {
                return parser.ParseFrom(entry.ValueBytes);
            }
            catch
            {
                // If the message is malformed, just report there's no information.
                return null;
            }
        }
    }
}
