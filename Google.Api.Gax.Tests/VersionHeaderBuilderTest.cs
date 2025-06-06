﻿/*
 * Copyright 2016 Google Inc. All Rights Reserved.
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file or at
 * https://developers.google.com/open-source/licenses/bsd
 */

using System;
using Xunit;

namespace Google.Api.Gax.Tests
{
    public class VersionHeaderBuilderTest
    {
        [Fact]
        public void InitiallyEmpty()
        {
            Assert.Equal("", new VersionHeaderBuilder().ToString());
        }

        // This theory will only have a single test case per framework, but it's the simplest way of expressing what we mean.
        [Theory]
#if NETCOREAPP3_1_OR_GREATER
        // Before Microsoft.NET.Test.Sdk verison 17.4.0, .NET Core and .NET 5+ were always reported as 3.1.0.
        // From Microsoft.NET.Test.Sdk version 17.14.0 onwards, the real runtime version is reported.
        [InlineData("8.0.0")]
#elif NET462
        // This will be the runtime version, which will always be 4.0.x, but we don't know x.
        [InlineData("4.0.")]
#else
#error Unsupported test frameowrk
#endif
        public void AppendDotNetEnvironment_AddsDotNetEntry(string expectedVersionPrefix)
        {
            string versionHeader = new VersionHeaderBuilder().AppendDotNetEnvironment().ToString();
            Assert.StartsWith($"gl-dotnet/{expectedVersionPrefix}", versionHeader);
        }

        [Theory]
        [InlineData("foo", "1.2.3-bar", "foo/1.2.3-bar")]
        [InlineData("foo", "", "foo/")]
        public void AppendVersion(string name, string version, string expected)
        {
            var builder = new VersionHeaderBuilder();
            builder.AppendVersion(name, version);
            var actual = builder.ToString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MultipleEntries()
        {
            Assert.Equal("foo/1.2.3-bar baz/1.0.0",
                new VersionHeaderBuilder()
                    .AppendVersion("foo", "1.2.3-bar")
                    .AppendVersion("baz", "1.0.0")
                    .ToString());
        }

        [Fact]
        public void AppendAssemblyVersion()
        {
            Assert.Equal("foo/1.0.0",
                new VersionHeaderBuilder().AppendAssemblyVersion("foo", GetType()).ToString());
        }

        private const string SampleHex = "0123456789abcdef0123456789ABCDEF01234567";
        private const string SampleNonHex = "0123456789GHIJKL0123456789ABCDEF01234567";

        [Theory]
        [InlineData("1.2.3+" + SampleHex, "1.2.3")]
        [InlineData("1.2.3-preview+" + SampleHex, "1.2.3-preview")]
        [InlineData("1.2.3-preview+build." + SampleHex, "1.2.3-preview+build")]
        public void FormatInformationalVersion(string info, string expected)
        {
            var actual = VersionHeaderBuilder.FormatInformationalVersion(info);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("1.2.3")]
        [InlineData("1.2.3-" + SampleHex)] // We require + or . before the hex
        [InlineData("1.2.3+" + SampleNonHex)] // We check that it's hex
        [InlineData("1.2.3+ABCDEF")] // We check that it's 40 characters (this is too short)
        [InlineData("1.2.3+A" + SampleHex)] // We check that it's 40 characters (this is too long)
        public void FormatInformationalVersion_NoTrimming(string info)
        {
            var actual = VersionHeaderBuilder.FormatInformationalVersion(info);
            Assert.Equal(info, actual);
        }

        [Fact]
        public void NamesAreUnique()
        {
            var builder = new VersionHeaderBuilder();
            builder.AppendVersion("name", "1.0.0");
            Assert.Throws<ArgumentException>(() => builder.AppendVersion("name", "2.0.0"));
        }

        [Fact]
        public void Clone()
        {
            var builder1 = new VersionHeaderBuilder();
            builder1.AppendVersion("x", "1.0.0");
            var builder2 = builder1.Clone();
            builder1.AppendVersion("y", "2.0.0");
            builder2.AppendVersion("z", "3.0.0");
            Assert.Equal("x/1.0.0 y/2.0.0", builder1.ToString());
            Assert.Equal("x/1.0.0 z/3.0.0", builder2.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("x y")]
        [InlineData("x/y")]
        public void InvalidName(string name)
        {
            var builder = new VersionHeaderBuilder();
            Assert.Throws<ArgumentException>(() => builder.AppendVersion(name, "1.0.0"));
        }

        [Theory]
        [InlineData("x y")]
        [InlineData("x/y")]
        public void InvalidVersion(string version)
        {
            var builder = new VersionHeaderBuilder();
            Assert.Throws<ArgumentException>(() => builder.AppendVersion("name", version));
        }
    }
}
