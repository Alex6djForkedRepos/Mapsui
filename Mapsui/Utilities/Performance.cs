﻿using Mapsui.Widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mapsui.Utilities;

public class Performance
{
    private readonly int _maxValues;
    private readonly double[] _drawingTimes;
    private bool _turnaround;
    private int _pos;
    private int _count;
    private double _min, _max;
    private double _sum;
    private double _runningFps;
    private readonly double _alphaForRunningFps = 0.99;
    private readonly Stopwatch _stopwatch = new();

    public static ActiveMode DefaultIsActive { get; set; } = ActiveMode.OnlyInDebugMode;

    /// <summary>
    /// Counter for number of redraws of map
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Minimal drawing time
    /// </summary>
    public double Min => _min == double.MaxValue ? 0 : _min;

    /// <summary>
    /// Maximal drawing time
    /// </summary>
    public double Max => _max;

    /// <summary>
    /// MaxValues of drawing times that are saved and used for mean value
    /// </summary>
    public int MaxValues => _maxValues;

    /// <summary>
    /// Mean value of all MaxValues drawing times
    /// </summary>
    public double Mean => _turnaround ? _sum / _maxValues : (_pos > 0 ? _sum / _pos : 0);

    /// <summary>
    /// Possible frames per second calculated from Mean
    /// </summary>
    public int FPS => Mean == 0 ? 0 : (int)(1000.0 / Mean);

    /// <summary>
    /// Running average of the actual frames per second.
    /// </summary>
    public double RunningFps => _runningFps;

    public ActiveMode IsActive { get; set; } = DefaultIsActive;

    /// <summary>
    /// Time be used for the last drawing
    /// </summary>
    public double LastDrawingTime
    {
        get
        {
            var pos = _pos == 0 ? _maxValues - 1 : _pos - 1;

            return _drawingTimes[pos];
        }
    }

    /// <summary>
    /// Get list of all drawing times
    /// </summary>
    /// <remarks>
    /// First entry is the newest time
    /// </remarks>
    public List<double> DrawingTimes
    {
        get
        {
            var result = new List<double>(_maxValues);
            var pos = _pos == 0 ? _maxValues - 1 : _pos - 1;

            while (pos != _pos)
            {
                result.Add(_drawingTimes[pos]);
                pos = pos <= 0 ? _maxValues - 1 : --pos;
            }

            return result;
        }
    }

    public Performance(int maxValues = 20)
    {
        if (maxValues <= 0)
            throw new ArgumentException("maxValues must not be equal or less 0");

        _maxValues = maxValues;
        _drawingTimes = new double[_maxValues];

        Clear();
    }

    /// <summary>
    /// Add next drawing time
    /// </summary>
    /// <param name="time"></param>
    public void Add(double time)
    {
        if (!GetIsActive())
            return;

        _sum = _sum - _drawingTimes[_pos] + time;
        _drawingTimes[_pos++] = time;
        _count++;

        if (_pos >= _maxValues)
        {
            _pos = 0;
            _turnaround = true;
        }

        if (_max < time)
            _max = time;

        if (_min > time)
            _min = time;

        var elapsed = _stopwatch.ElapsedMilliseconds;
        if (elapsed > 0)
        {
            double currentFps = 1000.0 / elapsed; // Milliseconds to FPS
            if (_runningFps == 0)
                _runningFps = currentFps;
            else
                _runningFps = _alphaForRunningFps * _runningFps + (1.0 - _alphaForRunningFps) * currentFps;
        }
        _stopwatch.Restart();
    }

    /// <summary>
    /// Clear all existing values up to now
    /// </summary>
    public void Clear()
    {
        _pos = 0;
        _sum = 0;
        for (var i = 0; i < _maxValues; i++)
            _drawingTimes[i] = 0.0;
        _min = double.MaxValue;
        _max = 0;
        _count = 0;
        _turnaround = false;
    }

    public bool GetIsActive() =>
        IsActive switch
        {
            ActiveMode.Yes => true,
            ActiveMode.No => false,
            ActiveMode.OnlyInDebugMode => Debugger.IsAttached,
            _ => throw new NotSupportedException(nameof(IsActive))
        };
}
