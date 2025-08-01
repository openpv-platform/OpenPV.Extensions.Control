﻿using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Cairo;
using Ahsoka.Dispatch;
using System;

namespace Ahsoka.CS.CAN;

internal class CairoUI
{
    // Const Colors for our Drawing
    static readonly DrawingColor backgroundColor = new(0xFF4682B4);
    static readonly DrawingColor colorFill = new(0xFFFFFFFF);
    static readonly DrawingColor colorFillTouched = new(0xFF444444);
    static readonly DrawingColor colorStroke = new(0xFF111111);

    /// <summary>
    /// Simple Example of Drawing with AhsokaDrawing
    /// </summary>
    /// <param name="args"></param>
    public void StartAndRun(Dispatcher defaultDispatcher)
    {
        // Create the Ahsoka Cairo Drawing API and matching Window
        CairoDrawingWindow window = new(OperatingSystem.IsLinux());
        CairoDrawingApi api = new(window.Bounds);

        // Setup Drawing Objects and Values
        var cloudTemp = api.CreateImage("Cloud.png");

        // This is our screen bounds
        DrawingRect bounds = window.Bounds;

        // This is the start position of our Circle / Cloud
        DrawingRect cloudPosition = new((bounds.Width / 2.0f - cloudTemp.Width / 2.0f),
                            (bounds.Height / 2.0f - cloudTemp.Height / 2.0f),
                            cloudTemp.Width, cloudTemp.Height);


        // Load our Font.  These can be used for any Text Infos your app creates
        // and disposed when no longer used.
        var typeface = api.LoadTypeface("Font.ttf", "Droid Sans");

        // Create a Text Info to describe our font / text attributes
        DrawingTextInfo textInfo = new()
        {
            Alignment = DrawingTextAlignment.Center,
            Color = backgroundColor,
            TextSize = 28,
            Typeface = typeface
        };

        // Create a Text Info to describe our font / text attributes
        DrawingTextInfo titleTextInfo = new()
        {
            Alignment = DrawingTextAlignment.Center,
            Color = colorFill,
            TextSize = 42,
            Typeface = typeface
        };


        float velocity = 8.0f;
        float angle = (float)Math.PI * 4;
        bool touched = false;

        // Handle the Touch Recieved Event
        // here we will simply change the color of the background on the touch
        window.TouchReceived += (o, args) =>
        {
            if (args.Event == TouchEvent.Pressed)
                touched = true;
            else if (args.Event == TouchEvent.Released)
                touched = false;
        };

        // Handle the Draw Frame.
        window.DrawFrame += (o, args) =>
        {
            // Initialize the API for each Frame
            api.StartFrame(args.Context);

            // Clear Background
            api.Clear(backgroundColor);


            // Draw the current time in our circle
            api.DrawText("OpenPV 1.0 CAN Demo",
                bounds.Width / 2, 45,
                titleTextInfo);

            api.DrawCircle(cloudPosition.X + cloudPosition.Width / 2,
                cloudPosition.Y + cloudPosition.Width / 2,
                cloudPosition.Width / 2,
                colorStroke,
                touched ? colorFillTouched : colorFill,
                2);

            // Draw the Cloud Icon at its selected Position
            api.DrawBitmap(cloudTemp, cloudPosition);

            // Draw the current time in our circle
            api.DrawText(DateTime.Now.ToString("hh:MM tt"),
                cloudPosition.X + cloudPosition.Width / 2,
                cloudPosition.Y + cloudPosition.Height / 2 + 32,
                textInfo);

            // Calculate new Cloud Position
            float x = cloudPosition.X + (velocity * (float)Math.Cos(angle * Math.PI / 180f));
            float y = cloudPosition.Y + velocity * (float)Math.Sin(angle * Math.PI / 180f);

            // Collision Detection.
            if (x < 0 || x > bounds.Width - cloudPosition.Height)
                angle = 180 - angle;
            else if (y < 0 || y > bounds.Height - cloudPosition.Height)
                angle = 360 - angle;

            cloudPosition = new DrawingRect(x, y, cloudPosition.Width, cloudPosition.Height);

            api.EndFrame();

            //Normally we would only draw if there is a change to our model or data
            // however here we are simply drawing at max framerate to bound our ball around
            // the screen similar to many game loops
            window.RequestDraw();
        };

        // Start the Dispatcher and Use Our Windows Invoker
        defaultDispatcher.StartAndRun(window.Invoke);

        // Now start the Main Drawing Loop 
        // we will block here until the app shuts down.
        window.ShowAndRun();
    }
}
