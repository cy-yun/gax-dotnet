﻿/*
 * Copyright 2016 Google Inc. All Rights Reserved.
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file or at
 * https://developers.google.com/open-source/licenses/bsd
 */
using Google.Protobuf;
using Grpc.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Google.Api.Gax.Grpc.IntegrationTests.EchoHeadersResponse.Types;

namespace Google.Api.Gax.Grpc.IntegrationTests
{
    /// <summary>
    /// Test fixture for a simple gRPC service hosted on a local port. This can be manually constructed,
    /// or used as a dependency for a test collection.
    /// </summary>
    [CollectionDefinition(nameof(TestServiceFixture))]
    public class TestServiceFixture : IDisposable, ICollectionFixture<TestServiceFixture>
    {
        private readonly Server _server;

        public int Port => _server.Ports.First().BoundPort;
        public string Endpoint => $"localhost:{Port}";

        public TestServiceFixture()
        {
            _server = new Server
            {
                Services = { TestService.BindService(new TestServiceImpl()) },
                Ports = { new ServerPort("localhost", 0, ServerCredentials.Insecure) }
            };
            _server.Start();
        }

        public void Dispose()
        {
            Task.Run(() => _server.ShutdownAsync()).Wait();
        }

        private class TestServiceImpl : TestService.TestServiceBase
        {
            public override Task<SimpleResponse> DoSimple(SimpleRequest request, ServerCallContext context)
                => Task.FromResult(new SimpleResponse { Name = request.Name });

            public override Task<EchoHeadersResponse> EchoHeaders(EchoHeadersRequest request, ServerCallContext context)
            {
                var response = new EchoHeadersResponse();
                foreach (var header in context.RequestHeaders)
                {
                    response.Headers[header.Key] = header.IsBinary
                        ? new HeaderValue { BytesValue = ByteString.CopyFrom(header.ValueBytes) }
                        : new HeaderValue { StringValue = header.Value };
                }
                return Task.FromResult(response);
            }
        }
    }
}
