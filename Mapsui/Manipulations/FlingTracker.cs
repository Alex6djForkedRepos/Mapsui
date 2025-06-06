﻿using Mapsui.Logging;
using System;
using System.Collections.Generic;

namespace Mapsui.Manipulations;

public class FlingTracker
{
    private const int _maxSize = 50;
    private const long _maxTicks = 200 * 10000;  // Use only events from the last 200 ms
    private readonly Queue<(double x, double y, long time)> _events;

    public static double MaxVelocity { get; set; } = 4000.0;   // When testing on WPF 4000 felt okay. Not sure if this is a good value in general.
    public static double MinVelocity { get; set; } = 1000.0;

    public FlingTracker()
    {
        _events = [];
    }

    public void AddEvent(ScreenPosition position, long ticks)
    {
        _events.Enqueue((position.X, position.Y, ticks));

        // Check, if we at the end of array
        if (_events.Count > 2)
        {
            while (_events.Count > _maxSize || _events.Peek().time < ticks - _maxTicks)
                _events.Dequeue();
        }
    }

    public void Restart()
    {
        _events.Clear();
    }

    private (double vx, double vy, double v) CalcVelocity(long now)
    {
        double distanceX = 0;
        double distanceY = 0;

        var eventQueue = _events;
        var eventsArray = eventQueue.ToArray();

        if (eventsArray.Length == 0)
            return (0, 0, 0);

        (_, _, var firstTime) = eventsArray[0];

        long finalTime = 0;

        for (var i = 1; i < eventsArray.Length; i++)
        {
            (var lastX, var lastY, var lastTime) = eventsArray[i - 1];
            (var nowX, var nowY, var nowTime) = eventsArray[i];

            // Only calc velocities for last maxTicks ticks
            if (now - lastTime < _maxTicks)
            {
                // Calc velocity in pixel per sec
                distanceX += (nowX - lastX) * 10000000;
                distanceY += (nowY - lastY) * 10000000;
            }

            finalTime = nowTime;
        }

        var totalTime = finalTime - firstTime;

        var vx = distanceX / totalTime;
        var vy = distanceY / totalTime;
        var v = Math.Sqrt(vx * vx + vy * vy);
        return (vx, vy, v);
    }

    public void FlingIfNeeded(Action<double, double> onFling)
    {
        var (velocityX, velocityY, velocity) = CalcVelocity(DateTime.Now.Ticks);

        // Only fling if the velocity is above a threshold.
        if (velocity <= MinVelocity)
        {
            if (Logger.Settings.LogFlingEvents)
                Logger.Log(LogLevel.Information, $"FlingTracker: Do not fling because the velocity of {velocity:F0} is lower than the min velocity {MinVelocity:F0}.");
            return;
        }

        // In some cases overloading of the app can cause incorrect velocity values which 
        // could result in extreme speeds. Therefor we limit the velocity to a maximum value.
        if (velocity > MaxVelocity && velocity > 0)
        {
            if (Logger.Settings.LogFlingEvents)
                Logger.Log(LogLevel.Information, $"FlingTracker: Limiting velocity from {velocity:F0} to {MaxVelocity:F0}.");
            var scale = MaxVelocity / velocity;
            velocityX *= scale;
            velocityY *= scale;
        }
        else
        {
            if (Logger.Settings.LogFlingEvents)
                Logger.Log(LogLevel.Information, $"FlingTracker: Fling with velocity {velocity:F0}.");
        }

        onFling(velocityX, velocityY);
    }
}
