using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System;

namespace VisualEditor;

public partial class MainPage : ContentPage
{
    SKBitmap bitmap;
    SKCanvas bitmapCanvas;
    SKPaint brush;
    SKColor currentColor = SKColors.Black;

    public MainPage()
    {
        InitializeComponent();
        InitCanvas();
        UpdateColorPreview();
    }

    void InitCanvas()
    {
        bitmap = new SKBitmap(800, 500);
        bitmapCanvas = new SKCanvas(bitmap);
        bitmapCanvas.Clear(SKColors.White);
        CanvasView.InvalidateSurface();
    }

    void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();
        if (bitmap != null)
            canvas.DrawBitmap(bitmap, 0, 0);
    }

    void OnCanvasTouched(object sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed || e.ActionType == SKTouchAction.Moved)
        {
            brush = new SKPaint
            {
                Color = currentColor,
                StrokeWidth = 10,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            bitmapCanvas.DrawCircle(e.Location.X, e.Location.Y, brush.StrokeWidth / 2, brush);
            CanvasView.InvalidateSurface();
            e.Handled = true;
        }
    }

    void RGBChanged(object sender, ValueChangedEventArgs e)
    {
        currentColor = new SKColor(
            (byte)RSlider.Value,
            (byte)GSlider.Value,
            (byte)BSlider.Value
        );
        UpdateColorPreview();
    }

    void UpdateColorPreview()
    {
        ColorPreview.BackgroundColor = Color.FromRgb(currentColor.Red, currentColor.Green, currentColor.Blue);
    }

    async void OnSaveClicked(object sender, EventArgs e)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        string path = Path.Combine(FileSystem.AppDataDirectory, $"obrazek_{DateTime.Now.Ticks}.png");

        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);

        await DisplayAlert("Zapisano", $"Zapisano plik:\n{path}", "OK");
    }
}