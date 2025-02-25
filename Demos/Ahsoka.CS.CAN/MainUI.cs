using Ahsoka.Core.Dispatch;
using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka.CS.CAN;

internal class MainUI
{
    // Const Colors for our Drawing
    readonly DrawingColor backgroundColor = new(0xFF4682B4);
    readonly DrawingColor colorFill = new(0xFFFFFFFF);
    readonly DrawingColor colorFillTouched = new(0xFF444444);
    readonly DrawingColor colorStroke = new(0xFF111111);
    DrawingTextInfo defaultTextInfo;
    bool isReady = false;
    string startupString = "Initializing System";
    public string statusString = "";
    string titleText = "";
    DrawingWindow window;
    readonly Dictionary<string, string> statusTextAreas = new();
    readonly List<TouchArea> touchAreas = new();

    public void InitStateChanged(bool isReady, string startupString)
    {
        this.isReady = isReady;
        this.startupString = startupString;
        statusString = startupString;
    }

    public DrawingWindow StartUI(string title)
    {
        titleText = title;

        // Create a full screen Window
        window = new(true);

        // Create the Ahsoka Drawing API and matching Window
        var api = window.GetApi();

        // Load our Font.  These can be used for any Text Infos your app creates
        // and disposed when no longer used.
        var typeface = api.LoadTypeface("Font.ttf", "Droid Sans");

        // Create a Text Info to describe our font / text attributes
        defaultTextInfo = new DrawingTextInfo()
        {
            Alignment = DrawingTextAlignment.Center,
            Color = backgroundColor,
            TextSize = 28,
            Typeface = typeface
        };

        // Close Button Bounds
        var closeButton = new DrawingRect(api.ScreenWidth - 60, 15, 45, 45);
        touchAreas.Add(new TouchArea() { Text = "X", Command = CloseWindow, IsTouched = false, Rect = closeButton });

        // Handle the Touch Received Event
        // here we will simply change the color of the background on the touch
        window.PrepareFrame += (o, args) =>
        {
            foreach (var eventItem in args.Events)
            {
                if (eventItem.Event == TouchEvent.Pressed)
                {
                    foreach (var area in touchAreas)
                        area.IsTouched = area.Rect.Contains(eventItem.X, eventItem.Y);
                }
                else if (eventItem.Event == TouchEvent.Released)
                {
                    foreach (var area in touchAreas)
                    {
                        if (area.Rect.Contains(eventItem.X, eventItem.Y) && area.IsTouched)
                            area.Command.Invoke();
                        area.IsTouched = false;
                    }
                }
            }
        };

        // Handle the Draw Frame.
        window.RenderFrame += (o, args) =>
        {
            DrawBackground(api);

            if (isReady)
            {
                DrawStatus(api);

                DrawButtons(api);
            }
        };
        return window;
    }

    private void CloseWindow()
    {
        window.Close();
    }

    public void UpdateStatusString(string statusMsg)
    {
        statusString = statusMsg;
    }

    public void AddButton(Action action, string buttonText)
    {
        var drawingRect = new DrawingRect(20, 95 + ((touchAreas.Count - 1) * 130), 200, 45);
        touchAreas.Add(new TouchArea() { Text = buttonText, Command = action, Rect = drawingRect });
    }

    public void UpdateStatusText(string titleText, string statusText)
    {
        statusTextAreas[titleText] = statusText;
    }

    private void DrawBackground(IDrawingApi api)
    {
        // Clear Background
        api.Clear(backgroundColor);
        api.DrawRectangle(new DrawingRect(0, 0, api.ScreenWidth, 75), colorFill, colorFill, 0);

        defaultTextInfo.Color = backgroundColor;
        defaultTextInfo.TextSize = 28;
        defaultTextInfo.Alignment = DrawingTextAlignment.Left;
        api.DrawText(titleText, 10, 35, defaultTextInfo);

        defaultTextInfo.TextSize = 16;
        api.DrawText(statusString, 15, 60, defaultTextInfo);
    }

    private void DrawStatus(IDrawingApi api)
    {
        var drawingRect = new DrawingRect(150, 124, 100, 25);

        defaultTextInfo.TextSize = 18;
        defaultTextInfo.Alignment = DrawingTextAlignment.Left;
        defaultTextInfo.Color = colorFill;
        foreach (var item in statusTextAreas)
        {
            api.DrawText(item.Key, drawingRect.X, drawingRect.Y, defaultTextInfo);

            defaultTextInfo.TextWeight = DrawingTextWeight.Italic;
            api.DrawText(item.Value, drawingRect.X + 325, drawingRect.Y, defaultTextInfo);

            drawingRect.Offset(0, 65);
        }
        defaultTextInfo.TextWeight = DrawingTextWeight.Normal;
    }

    private void DrawButtons(IDrawingApi api)
    {
        defaultTextInfo.Color = backgroundColor;
        defaultTextInfo.TextSize = 18;
        defaultTextInfo.Alignment = DrawingTextAlignment.Center;
        foreach (var item in touchAreas)
        {
            if (item.IsTouched)
                api.DrawRectangle(item.Rect, colorStroke, colorFillTouched, 1);
            else
                api.DrawRectangle(item.Rect, colorStroke, colorFill, 1);

            api.DrawText(item.Text, item.Rect.X + item.Rect.Width / 2, item.Rect.Y + item.Rect.Height / 2 + 7, defaultTextInfo);
        }
    }

    class TouchArea
    {
        public DrawingRect Rect;
        public bool IsTouched;
        public Action Command;
        public string Text;
    }
}
