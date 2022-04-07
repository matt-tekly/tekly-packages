# About Logger

Logger is a structured logging replacement for `Debug.Log` used in Unity.

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
- Set log levels per class or by namespace
- Structured logging with additional params per log
- Global common fields to be added to log events
- Multiple log destinations
    - Local log file as plain text or as JSON
    - Unity logger
    - Loggly
- `#ifdef` out different log levels to increase performance

# Usage

## Configuration
You must place a LoggerConfig in the resources directory. It must be named `logger_config.xml`.

A simple example config where:
- Default LogLevel for all loggers is `Info`
- LogLevel for `Game.Auth` namespace is `Error`

```
<LoggerConfig>
    <DefaultProfile Name="Default">
        <Levels Logger="*" Level="Info" />
        <Levels Logger="Game.Auth" Level="Error" />
    </DefaultProfile>
</LoggerConfig>
```

Note that there is a concept of a LogProfile but only one is supported right now.

## Runtime
```csharp
using System;
using Tekly.Logging;
using UnityEngine;

public class TestLogs : MonoBehaviour
{
    private TkLogger m_logger = TkLogger.Get<TestLogs>();

    public void Start()
    {
        TkLogger.SetCommonField("g_user", "Joe");

        m_logger.Info("Start"); // [TestLogs] Start
        m_logger.Info("Start {g_user}"); // [TestLogs] Start Joe
        m_logger.Info("Start {g_user} {id}", ("id", 5)); // [TestLogs] Start Joe 5
        m_logger.Info("Start {date}", ("date", DateTime.Now)); // [TestLogs] Start 9/7/2020 10:15:22 AM
    }
}
```