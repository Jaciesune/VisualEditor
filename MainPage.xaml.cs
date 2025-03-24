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
    bool isDrawing = false;
    readonly SKColor[] colorSlots = new SKColor[8];
    readonly List<BoxView> slotViews = new();
    int selectedSlotIndex = -1;
    bool isUpdatingSliders = false;
    readonly List<Border> slotBorders = new();

    public MainPage()
    {
        InitializeComponent();
        InitColorSlots();
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
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                isDrawing = true;
                break;

            case SKTouchAction.Moved:
                if (isDrawing)
                {
                    DrawAt(e.Location);
                }
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                isDrawing = false;
                break;
        }

        e.Handled = true;
    }

    void InitColorSlots()
    {
        for (int i = 0; i < 8; i++)
        {
            var box = new BoxView
            {
                WidthRequest = 30,
                HeightRequest = 30,
                BackgroundColor = Colors.White
            };

            var border = new Border
            {
                StrokeThickness = 2,
                Stroke = Colors.Transparent,
                Content = box,
                Padding = 1
            };

            int index = i;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                selectedSlotIndex = index;

                currentColor = colorSlots[index];

                isUpdatingSliders = true;
                RSlider.Value = currentColor.Red;
                GSlider.Value = currentColor.Green;
                BSlider.Value = currentColor.Blue;
                isUpdatingSliders = false;

                UpdateColorPreview();
                UpdateSlotHighlights();
            };

            border.GestureRecognizers.Add(tap);
            slotViews.Add(box);
            slotBorders.Add(border);
            ColorSlotsPanel.Add(border);
        }
    }

    void OnPredefinedColorTapped(object sender, EventArgs e)
    {
        if (sender is BoxView box)
        {
            var color = box.Color;
            currentColor = new SKColor(
                (byte)(color.Red * 255),
                (byte)(color.Green * 255),
                (byte)(color.Blue * 255)
            );

            RSlider.Value = currentColor.Red;
            GSlider.Value = currentColor.Green;
            BSlider.Value = currentColor.Blue;
            UpdateColorPreview();
        }
    }

    void UpdateSlotHighlights()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            var color = colorSlots[i];
            slotViews[i].BackgroundColor = Color.FromRgb(color.Red, color.Green, color.Blue);
            slotBorders[i].Stroke = (i == selectedSlotIndex) ? Colors.Gold : Colors.Transparent;
        }
    }

    void DrawAt(SKPoint point)
    {
        brush = new SKPaint
        {
            Color = currentColor,
            StrokeWidth = 10,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        bitmapCanvas.DrawCircle(point.X, point.Y, brush.StrokeWidth / 2, brush);
        CanvasView.InvalidateSurface();
    }

    void RGBChanged(object sender, ValueChangedEventArgs e)
    {
        if (isUpdatingSliders)
            return;

        currentColor = new SKColor(
            (byte)RSlider.Value,
            (byte)GSlider.Value,
            (byte)BSlider.Value
        );
        UpdateColorPreview();

        // Aktualizacja aktywnego slotu
        if (selectedSlotIndex >= 0)
        {
            colorSlots[selectedSlotIndex] = currentColor;
            UpdateSlotHighlights();
        }
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