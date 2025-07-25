﻿using System;
using System.Linq;
using Mapsui.Rendering.Skia.Extensions;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using SkiaSharp;

namespace Mapsui.Rendering.Skia.SkiaWidgets;

public class ScaleBarWidgetRenderer : ISkiaWidgetRenderer, IDisposable
{
    private readonly SKPaint _paintScaleBar = CreateScaleBarPaint(SKPaintStyle.Fill);
    private readonly SKPaint _paintScaleBarStroke = CreateScaleBarPaint(SKPaintStyle.Stroke);
    private readonly SKPaint _paintScaleText = CreateTextPaint(SKPaintStyle.Fill);
    private readonly SKFont _paintScaleTextFont = CreateFont();
    private readonly SKPaint _paintScaleTextStroke = CreateTextPaint(SKPaintStyle.Stroke);
    private readonly SKFont _paintScaleTextStrokeFont = CreateFont();

    public void Draw(SKCanvas canvas, Viewport viewport, IWidget widget, RenderService renderService,
        float layerOpacity)
    {
        var scaleBar = (ScaleBarWidget)widget;
        if (!scaleBar.CanProject()) return;

        // Update paints with new values
        _paintScaleBar.Color = scaleBar.TextColor.ToSkia(layerOpacity);
        _paintScaleBar.StrokeWidth = (float)(scaleBar.StrokeWidth * scaleBar.Scale);
        _paintScaleBarStroke.Color = scaleBar.Halo.ToSkia(layerOpacity);
        _paintScaleBarStroke.StrokeWidth = (float)(scaleBar.StrokeWidthHalo * scaleBar.Scale);
        _paintScaleText!.Color = scaleBar.TextColor.ToSkia(layerOpacity);
        _paintScaleText.StrokeWidth = (float)(scaleBar.StrokeWidth * scaleBar.Scale);
        _paintScaleTextFont.Typeface = SKTypeface.FromFamilyName(scaleBar.Font?.FontFamily,
            SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        _paintScaleTextFont.Size = (float)((scaleBar.Font?.Size ?? 10) * scaleBar.Scale);
        _paintScaleTextStroke!.Color = scaleBar.Halo.ToSkia(layerOpacity);
        _paintScaleTextStroke.StrokeWidth = (float)(scaleBar.StrokeWidthHalo / 2 * scaleBar.Scale);
        _paintScaleTextStrokeFont.Typeface = SKTypeface.FromFamilyName(scaleBar.Font?.FontFamily,
            SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        _paintScaleTextStrokeFont.Size = (float)((scaleBar.Font?.Size ?? 10) * scaleBar.Scale);

        double scaleBarLength1;
        string? scaleBarText1;
        double scaleBarLength2;
        string? scaleBarText2;

        (scaleBarLength1, scaleBarText1, scaleBarLength2, scaleBarText2) = scaleBar.GetScaleBarLengthAndText(viewport);

        // Calc height of scale bar

        // Do this, because height of text changes sometimes (e.g. from 2 m to 1 m)
        _paintScaleTextStrokeFont.MeasureText("9999 m", out var textSize, _paintScaleTextStroke);

        var scaleBarHeight = textSize.Height + (scaleBar.TickLength + scaleBar.StrokeWidthHalo * 0.5f + ScaleBarWidget.TextMargin) * scaleBar.Scale;

        if (scaleBar.ScaleBarMode == ScaleBarMode.Both && scaleBar.SecondaryUnitConverter != null)
        {
            scaleBarHeight *= 2;
        }
        else
        {
            scaleBarHeight += scaleBar.StrokeWidthHalo * 0.5f * scaleBar.Scale;
        }

        scaleBar.Height = scaleBarHeight;

        // Draw lines

        // Get lines for scale bar
        var points = scaleBar.GetScaleBarLinePositions(viewport, scaleBarLength1, scaleBarLength2, scaleBar.StrokeWidthHalo);

        // Draw outline of scale bar
        for (var i = 0; i < points.Count; i += 2)
        {
            canvas.DrawLine((float)points[i].X, (float)points[i].Y, (float)points[i + 1].X, (float)points[i + 1].Y, _paintScaleBarStroke);
        }

        // Draw scale bar
        for (var i = 0; i < points.Count; i += 2)
        {
            canvas.DrawLine((float)points[i].X, (float)points[i].Y, (float)points[i + 1].X, (float)points[i + 1].Y, _paintScaleBar);
        }

        if (!points.Any()) throw new NotImplementedException($"A {nameof(ScaleBarWidget)} can not be drawn without line positions");

        var envelop = new MRect(points.Select(p => new MRect(p.X, p.Y)));
        envelop = envelop.Grow(scaleBar.StrokeWidthHalo * 0.5f * scaleBar.Scale);

        // Draw text

        // Calc text height
        var textSize2 = SKRect.Empty;

        scaleBarText1 ??= string.Empty;
        _paintScaleTextStrokeFont.MeasureText(scaleBarText1, out var textSize1, _paintScaleTextStroke);

        if (scaleBar.ScaleBarMode == ScaleBarMode.Both && scaleBar.SecondaryUnitConverter != null)
        {
            // If there is SecondaryUnitConverter we need to calculate the size before passing it into GetScaleBarTextPositions
            scaleBarText2 ??= string.Empty;
            _paintScaleTextStrokeFont.MeasureText(scaleBarText2, out textSize2, _paintScaleTextStroke);
        }

        var (posX1, posY1, posX2, posY2) = scaleBar.GetScaleBarTextPositions(viewport, textSize1.ToMRect(), textSize2.ToMRect(), scaleBar.StrokeWidthHalo);

        // Now draw text
        canvas.DrawText(scaleBarText1, (float)posX1, (float)(posY1 - textSize1.Top), SKTextAlign.Left, _paintScaleTextStrokeFont, _paintScaleTextStroke);
        canvas.DrawText(scaleBarText1, (float)posX1, (float)(posY1 - textSize1.Top), SKTextAlign.Left, _paintScaleTextFont, _paintScaleText);

        envelop = envelop?.Join(new MRect(posX1, posY1, posX1 + textSize1.Width, posY1 + textSize1.Height));

        if (scaleBar.ScaleBarMode == ScaleBarMode.Both && scaleBar.SecondaryUnitConverter != null)
        {
            // Now draw second text
            canvas.DrawText(scaleBarText2, (float)posX2, (float)(posY2 - textSize2.Top), SKTextAlign.Left, _paintScaleTextStrokeFont, _paintScaleTextStroke);
            canvas.DrawText(scaleBarText2, (float)posX2, (float)(posY2 - textSize2.Top), SKTextAlign.Left, _paintScaleTextFont, _paintScaleText);

            envelop = envelop?.Join(new MRect(posX2, posY2, posX2 + textSize2.Width, posY2 + textSize2.Height));
        }

        scaleBar.Envelope = envelop;

        if (scaleBar.ShowEnvelop && envelop != null)
        {
            // Draw a rect around the scale bar for testing
            var tempPaint = _paintScaleTextStroke;
            canvas.DrawRect(new SKRect((float)envelop.MinX, (float)envelop.MinY, (float)envelop.MaxX, (float)envelop.MaxY), tempPaint);
        }
    }

    private static SKPaint CreateScaleBarPaint(SKPaintStyle style)
    {
        return new SKPaint
        {
            Style = style,
            StrokeCap = SKStrokeCap.Square
        };
    }

    private static SKPaint CreateTextPaint(SKPaintStyle style)
    {
        return new SKPaint
        {
            Style = style,
            IsAntialias = true
        };
    }

    private static SKFont CreateFont()
    {
        return new SKFont()
        {
            Edging = SKFontEdging.SubpixelAntialias,
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _paintScaleBar.Dispose();
            _paintScaleBarStroke.Dispose();
            _paintScaleText.Dispose();
            _paintScaleTextFont.Dispose();
            _paintScaleTextStroke.Dispose();
            _paintScaleTextStrokeFont.Dispose();
        }
    }
}
