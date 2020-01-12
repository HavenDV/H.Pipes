# NamedPipeServerStream.NetFrameworkVersion for .NET Standard 2.0

[![Language](https://img.shields.io/badge/language-C%23-blue.svg?style=flat-square)](https://github.com/HavenDV/H.Pipes/search?l=C%23&o=desc&s=&type=Code) 
[![License](https://img.shields.io/github/license/HavenDV/H.Pipes.svg?label=License&maxAge=86400)](LICENSE.md) 
[![Requirements](https://img.shields.io/badge/Requirements-.NET%20Standard%202.0-blue.svg)](https://github.com/dotnet/standard/blob/master/docs/versions/netstandard2.0.md)
[![Build Status](https://github.com/HavenDV/H.Pipes/workflows/.NET%20Core/badge.svg?branch=master)](https://github.com/HavenDV/H.Pipes/actions?query=workflow%3A%22.NET+Core%22)

## Nuget

[![NuGet](https://img.shields.io/nuget/dt/NamedPipeServerStream.NetFrameworkVersion.svg?style=flat-square&label=NamedPipeServerStream.NetFrameworkVersion)](https://www.nuget.org/packages/NamedPipeServerStream.NetFrameworkVersion/)

## Usage

```
Install-Package NamedPipeServerStream.NetFrameworkVersion
```

```csharp
using System.IO.Pipes;

var pipeSecurity = new PipeSecurity();
pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.ReadWrite, AccessControlType.Allow));

using var serverStream = NamedPipeServerStreamConstructors.New(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, 0, 0, pipeSecurity);
```