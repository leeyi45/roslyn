﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if !NETSTANDARD1_3
using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Interop;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal static class SigningTestHelpers
    {
        public static readonly StrongNameProvider s_defaultDesktopProvider =
            new DesktopStrongNameProvider(ImmutableArray<string>.Empty, null, new VirtualizedStrongNameFileSystem());

        public static readonly StrongNameProvider s_defaultPortableProvider =
            new PortableStrongNameProvider(ImmutableArray<string>.Empty, new VirtualizedStrongNameFileSystem());

        // these are virtual paths that don't exist on disk
        internal static string KeyPairFile = @"R:\__Test__\KeyPair_" + Guid.NewGuid() + ".snk";
        internal static string PublicKeyFile = @"R:\__Test__\PublicKey_" + Guid.NewGuid() + ".snk";

        internal static string KeyPairFile2 = @"R:\__Test__\KeyPair2_" + Guid.NewGuid() + ".snk";
        internal static string PublicKeyFile2 = @"R:\__Test__\PublicKey2_" + Guid.NewGuid() + ".snk";

        internal static string MaxSizeKeyFile = @"R:\__Test__\MaxSizeKey_" + Guid.NewGuid() + ".snk";

        private static bool s_keyInstalled;
        internal const string TestContainerName = "RoslynTestContainer";

        internal static readonly ImmutableArray<byte> PublicKey = ImmutableArray.Create(TestResources.General.snPublicKey);

        internal static object s_keyInstalledLock = new object();

        // Modifies machine wide state.
        internal unsafe static void InstallKey()
        {
            lock (s_keyInstalledLock)
            {
                if (!s_keyInstalled)
                {
                    InstallKey(TestResources.General.snKey, TestContainerName);
                    s_keyInstalled = true;
                }
            }
        }

        private unsafe static void InstallKey(byte[] keyBlob, string keyName)
        {
            try
            {
                IClrStrongName strongName = new DesktopStrongNameProvider().GetStrongNameInterface();

                //EDMAURER use marshal to be safe?
                fixed (byte* p = keyBlob)
                {
                    strongName.StrongNameKeyInstall(keyName, (IntPtr)p, keyBlob.Length);
                }
            }
            catch (COMException ex)
            {
                if (unchecked((uint)ex.ErrorCode) != 0x8009000F)
                    throw;
            }
        }

        internal sealed class VirtualizedStrongNameFileSystem : StrongNameFileSystem
        {
            private static bool PathEquals(string left, string right)
            {
                return string.Equals(
                    FileUtilities.NormalizeAbsolutePath(left),
                    FileUtilities.NormalizeAbsolutePath(right),
                    StringComparison.OrdinalIgnoreCase);
            }

            internal override bool FileExists(string fullPath)
            {
                return PathEquals(fullPath, KeyPairFile) || PathEquals(fullPath, PublicKeyFile);
            }

            internal override byte[] ReadAllBytes(string fullPath)
            {
                if (PathEquals(fullPath, KeyPairFile))
                {
                    return TestResources.General.snKey;
                }
                else if (PathEquals(fullPath, PublicKeyFile))
                {
                    return TestResources.General.snPublicKey;
                }
                else if (PathEquals(fullPath, KeyPairFile2))
                {
                    return TestResources.General.snKey2;
                }
                else if (PathEquals(fullPath, PublicKeyFile2))
                {
                    return TestResources.General.snPublicKey2;
                }
                else if (PathEquals(fullPath, MaxSizeKeyFile))
                {
                    return TestResources.General.snMaxSizeKey;
                }

                throw new FileNotFoundException("File not found", fullPath);
            }
        }
    }
}

#endif
