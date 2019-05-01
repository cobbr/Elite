# Elite

Elite is a .NET core, client application built for interacting with [Covenant](https://github.com/cobbr/Covenant).

## Covenant

Covenant is a .NET command and control framework that aims to highlight the attack surface of .NET, make the use of offensive .NET tradecraft easier, and serve as a collaborative command and control platform for red teamers.

Covenant is an ASP.NET Core, cross-platform application that includes a robust API to enable a client-server architecture that allows for multi-user collaboration. There are three main components of Covenant's architecture:

* **Covenant** - Covenant is the server-side component of the client-server architecture. Covenant runs the command and control server hosted on infrastructure shared between operators. I will also frequently use the term "Covenant" to refer to the entire overarching project that includes all components of the architecture.
* **Elite** - [Elite](https://github.com/cobbr/Elite) is the client-side component of the client-server architecture. Elite is a command-line interface that operators use to interact with the Covenant server to conduct operations.
* **Grunt** - A "Grunt" is the name of Covenant's implant that is deployed to targets.

## Features

Covenant has several key features:

* **Multi-Platform** - Covenant and Elite both target .NET Core, which makes them multi-platform. This allows these programs to run natively on Linux, MacOS, and Windows platforms. Additionally, both Covenant and Elite have docker support, allowing these programs to run within a container on any system that has docker installed.
* **Multi-User** - Covenant supports multi-user collaboration. The ability to collaborate has become crucial for effective red team operations. Many users can start Elite clients that connect to the same Covenant server and operate independently or collaboratively.
* **API Driven** - Covenant is driven by a server-side API that enables multi-user collaboration and is easily extendible. Additionally, Covenant includes a Swagger UI that makes development and debugging easier and more convenient.
* **Listener Profiles** - Covenant supports listener "profiles" that control how the network communication between Grunt implants and Covenant listeners look on the wire.
* **Encrypted Key Exchange** - Covenant implements an encrypted key exchange between Grunt implants and Covenant listeners that is largely based on a similar exchange in the [Empire project](https://github.com/EmpireProject/Empire), in addition to optional SSL encryption. This achieves the cryptographic property of forward secrecy between Grunt implants.
* **Dynamic Compilation** - Covenant uses the [Roslyn API](https://github.com/dotnet/roslyn) for dynamic C# compilation. Every time a new Grunt is generated or a new task is assigned, the relevant code is recompiled and obfuscated with [ConfuserEx](https://github.com/mkaring/ConfuserEx), avoiding totally static payloads. Covenant reuses much of the compilation code from the [SharpGen](https://github.com/cobbr/sharpgen) project, which I described in much more detail [in a previous post](https://cobbr.io/SharpGen.html).
* **Inline C# Execution** - Covenant borrows code and ideas from both the [SharpGen](https://github.com/cobbr/sharpgen) and [SharpShell](https://github.com/cobbr/sharpshell) projects to allow operators to execute C# one-liners on Grunt implants. This allows for similar functionality to that described in the [SharpShell post](https://cobbr.io/SharpShell.html), but allows the one-liners to be executed on remote implants.
* **Tracking Indicators** - Covenant tracks "indicators" throughout an operation, and summarizes them in the `Indicators` menu. This allows an operator to conduct actions that are tracked throughout an operation and easily summarize those actions to the blue team during or at the end of an assessment for deconfliction and educational purposes. This feature is still in it's infancy and still has room for improvement.

## Users Quick-Start Guide

First, you need to start Covenant! Go checkout the [Covenant README](https://github.com/cobbr/Covenant/blob/master/README.md) to see how to do that.

### Dotnet Core

The easiest way to use Elite, is by installing dotnet core. You can download dotnet core for your platform from [here](https://dotnet.microsoft.com/download).

Once you have installed dotnet core, we can build and run Elite using the dotnet CLI:
```
$ ~/Elite/Elite > dotnet build
$ ~/Elite/Elite > dotnet run
```

### Docker

Elite can also be run with Docker. There are a couple of gotchas with Docker, so I only recommend using docker if you are familiar with docker or are willing to learn the subtle gotchas.

First, build the docker image:
```
$ ~/Elite/Elite > docker build -t elite .
```

Now we can run Elite in a Docker container:
```
$ ~/Elite/Elite > docker run -it --rm --name elite -v /absolute/path/to/Elite/Data:/app/Data elite --username AdminUser --computername <Covenant IP>
```
The `--username AdminUser` and `--computername <Covenant IP>` are arguments being passed to Elite. This instructs Elite to connect to a Covenant instance hosted at the specifiec IP address and login as a user named `AdminUser`.

The `-it` parameter is a Docker parameter that indicates that we should begin Elite in an interactive tty. This is important, as Elite is an interactive console application! The `-v /absolute/path/to/Elite/Data:/app/Data` parameter mounts a shared `Data` folder between your host and container, and allows you to easily copy/paste outside tools and payloads generated during operation with Elite. Be sure to replace `/absolute/path/to/Elite/Data` with the location of your own Elite Data folder.

You will also be prompted to provide a password for the `AdminUser` user. Alternatively, you can set this non-interactively with the `--password` parameter to Elite, but this will leave your password in plaintext in command history, not ideal.

### Questions and Discussion

Have questions or want to chat more about Covenant/Elite? Join the #Covenant channel in the [BloodHound Gang Slack](https://bloodhoundgang.herokuapp.com/).