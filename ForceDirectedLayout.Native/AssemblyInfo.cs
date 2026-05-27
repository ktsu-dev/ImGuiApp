// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// This assembly is a thin AOT publish wrapper around ktsu.ForceDirectedLayout.
// Its only purpose is to be `dotnet publish -r <rid> /p:PublishAot=true /p:NativeLib=Shared`-ed
// into a per-platform shared library that native callers can dlopen.
// The exported C entry points live on ktsu.ForceDirectedLayout.NativeExports.
