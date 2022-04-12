
# About Logger

`TkLogger` is a structured logging replacement for `Debug.Log` used in Unity.

What is the benefit of using `TkLogger` over Unity's built in logging solution?

Regular Unity log events only have 3 pieces of information

- Message
- Stacktrace 
- LogType

A structured log has 

- Message
- LoggerName
- LogLevel
- Stacktrace
- Timestamp
- Params

# Features
- Set log levels per class or by namespace to filter log events
  - If done correctly filtered log events have no allocations
- Structured logging with additional params per log
- Global common fields to be added to log events
- Multiple log destinations
    - Local log file as plain text or as NDJSON
    - Unity logger
    - Loggly
- `#ifdef` out different log levels to increase performance
- Log configuration profiles for different environments

# Usage

# Concepts

## TkLogger
A logger instance that you send log events to. A logger should be created per class that you intend to log from.

Here is a simple example of getting a logger and logging an event.

```c#
using Tekly.Logging;
using UnityEngine;

public class TestLogs : MonoBehaviour
{
    private TkLogger m_logger = TkLogger.Get<TestLogs>();

    public void Start()
    {
        m_logger.Info("Start"); // Outputs: [TestLogs] Start
    }
}
```

## LoggerDestination
Loggers send logs to `LoggerDestinations`. Destinations are used to actually output the log to somewhere useful like a file or central log service.

## LoggerConfigData
This is the configuration for `TkLogger`.

## Common Fields
`TkLogger` supports common fields that are set globally and are available to every log. This is useful if you need common data to be associated with every log event.

Some common use cases:

- Application version
- Application name
- Application environment
- Player ID
- Session ID
- Current game state or game area
- Last button pressed

# Setup

## Initialization
You must manually initialize `TkLogger` by calling `TkLogger.Initialize`. Generally you want to do this as soon as possible in your application.

For example:

```csharp
using Tekly.Logging;
using UnityEngine;

public class AppCore
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        TkLogger.Initialize();
    }
}
```


## Configuration
If you don't pass a config to `TkLogger.Initialize()` the system looks for a config in the Resources directory. It must be named `logger_config.xml`.

A simple example config where:
- For all loggers:
    - `LogLevel` is `Info`
    - They log to a `FlatFile` with the prefix `main` and log to `Unity` which is the console
- For classes in the `Net` namespace the configuration is overridden
    - `LogLevel` is `Warning`
    - They log to a `FlatFile` with the prefix `net` and log to `Unity` which is the console

```xml
<?xml version="1.0" encoding="utf-8"?>
<LoggerConfig>
    <Profile Name="default" Default="true">
        <Destinations>
            <FlatFile Name="main" Prefix="main"/>
            <FlatFile Name="net" Prefix="net"/>
            <Unity Name="unity" />
        </Destinations>

        <Groups>
            <Group Name="default" Default="true">
                <Destinations>
                    <string>main</string>
                    <string>unity</string>
                </Destinations>
            </Group>

            <Group Name="network">
                <Destinations>
                    <string>net</string>
                    <string>unity</string>
                </Destinations>
            </Group>
        </Groups>

        <Loggers>
            <Default Level="Info" Group="default" />
            <Logger Logger="Net" Level="Warning" Group="network" />
        </Loggers>
    </Profile>
</LoggerConfig>
```


## Runtime
```csharp
using Tekly.Logging;
using UnityEngine;

public class TestLogs : MonoBehaviour
{
    private TkLogger m_logger = TkLogger.Get<TestLogs>();

    public void Start()
    {
        TkLogger.SetCommonField("g_user", "Joe");

        m_logger.Info("Start"); // Outputs: [TestLogs] Start
        m_logger.Info("Start {g_user}"); // Outputs: [TestLogs] Start Joe
        m_logger.Info("Start {g_user} {id}", ("id", 5)); // Outputs: [TestLogs] Start Joe 5
        m_logger.Info("Start {date}", ("date", DateTime.Now)); // Outputs: [TestLogs] Start 9/7/2020 10:15:22 AM
    }
}
```