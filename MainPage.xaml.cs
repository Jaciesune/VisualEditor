using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System;
using System.Collections.Generic;

namespace VisualEditor;

public partial class MainPage : ContentPage
{
    SKPaint brush;
    SKColor currentColor = SKColors.Black;
    bool isDrawing = false;
    readonly SKColor[] colorSlots = new SKColor[8];
    readonly List<BoxView> slotViews = new();
    readonly List<Border> slotBorders = new();
    int selectedSlotIndex = -1;
    bool isUpdatingSliders = false;

    List<Layer> layers = new();
    int activeLayerIndex = 0;

    public MainPage()
    {
        InitializeComponent();
        InitColorSlots();
        InitLayers();
        UpdateColorPreview();
    }

    void InitLayers()
    {
        layers.Clear();

        var baseLayer = new Layer
        {
            Name = "Background",
            Bitmap = new SKBitmap(800, 500)
        };
        baseLayer.Canvas = new SKCanvas(baseLayer.Bitmap);
        baseLayer.Canvas.Clear(SKColors.White);

        layers.Add(baseLayer);
        activeLayerIndex = 0;

        RefreshLayersUI();
        CanvasView.InvalidateSurface();
    }

    void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        foreach (var layer in layers)
        {
            canvas.DrawBitmap(layer.Bitmap, 0, 0);
        }
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

    void DrawAt(SKPoint point)
    {
        brush = new SKPaint
        {
            Color = currentColor,
            StrokeWidth = 10,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        layers[activeLayerIndex].Canvas.DrawCircle(point.X, point.Y, brush.StrokeWidth / 2, brush);
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

    void UpdateSlotHighlights()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            var color = colorSlots[i];
            slotViews[i].BackgroundColor = Color.FromRgb(color.Red, color.Green, color.Blue);
            slotBorders[i].Stroke = (i == selectedSlotIndex) ? Colors.Gold : Colors.Transparent;
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

            isUpdatingSliders = true;
            RSlider.Value = currentColor.Red;
            GSlider.Value = currentColor.Green;
            BSlider.Value = currentColor.Blue;
            isUpdatingSliders = false;

            UpdateColorPreview();
        }
    }

    async void OnSaveClicked(object sender, EventArgs e)
    {
        using var image = SKImage.FromBitmap(layers[0].Bitmap); // for now, only base layer
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        string path = Path.Combine(FileSystem.AppDataDirectory, $"image_{DateTime.Now.Ticks}.png");

        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);

        await DisplayAlert("Saved", $"Image saved to:\n{path}", "OK");
    }

    void AddLayer_Clicked(object sender, EventArgs e)
    {
        var newLayer = new Layer
        {
            Name = $"Layer {layers.Count}",
            Bitmap = new SKBitmap(800, 500)
        };
        newLayer.Canvas = new SKCanvas(newLayer.Bitmap);
        newLayer.Canvas.Clear(SKColors.Transparent);

        layers.Add(newLayer);
        activeLayerIndex = layers.Count - 1;

        RefreshLayersUI();
        CanvasView.InvalidateSurface();
    }

    void RemoveLayer_Clicked(object sender, EventArgs e)
    {
        if (layers.Count <= 1)
        {
            DisplayAlert("Error", "Cannot remove the last layer.", "OK");
            return;
        }

        layers.RemoveAt(activeLayerIndex);
        activeLayerIndex = Math.Max(0, activeLayerIndex - 1);

        RefreshLayersUI();
        CanvasView.InvalidateSurface();
    }

    void RefreshLayersUI()
    {
        LayersPanel.Children.Clear();

        for (int i = 0; i < layers.Count; i++)
        {
            int index = i;
            var button = new Button
            {
                Text = layers[i].Name + (i == activeLayerIndex ? " (active)" : ""),
                BackgroundColor = (i == activeLayerIndex) ? Colors.Gold : Colors.LightGray,
                FontSize = 14
            };

            button.Clicked += (s, e) =>
            {
                activeLayerIndex = index;
                RefreshLayersUI();
            };

            LayersPanel.Children.Add(button);
        }
    }

    class Layer
    {
        public string Name { get; set; } = "Layer";
        public SKBitmap Bitmap { get; set; }
        public SKCanvas Canvas { get; set; }
    }
}
