#pragma warning disable IDISP001 // Dispose created

using Eto.Drawing;
using Eto.Forms;
using Mapsui.Extensions;
using Mapsui.Samples.Common;
using Mapsui.Samples.Common.Extensions;
using Mapsui.UI.Eto;
using System;
using System.Linq;

namespace Mapsui.Samples.Eto;

public class MainForm : Form
{
    static MainForm()
    {
        Mapsui.Tests.Common.Samples.Register();
        Mapsui.Samples.Common.Samples.Register();
    }

    DropDown CategoryComboBox = new() { Width = 200 };
    StackLayout SampleList = new();
    MapControl MapControl = new();
    StackLayout LayerList = new() { HorizontalContentAlignment = HorizontalAlignment.Right };
    Slider RotationSlider = new() { Width = 200 };
    public MainForm()
    {
        MinimumSize = new Size(3, 2) * 100;
        Size = MinimumSize * 4;
        Padding = 10;

        var eto_platform = global::Eto.Platform.Instance.ToString();
        var os_platform = Environment.OSVersion.ToString();
        Title = $"Mapsui SampleApp - {eto_platform} - {os_platform}";

        MapControl.Map.Navigator.RotationLock = false;
        RotationSlider.ValueChanged += RotationSliderChanged;

        MapControl.ZoomButton = MouseButtons.Alternate;
        MapControl.ZoomModifier = Keys.None;

        CategoryComboBox.SelectedValueChanged += CategoryComboBox_SelectedValueChanged;

        FillComboBoxWithCategories();
        FillListWithSamples();

        //

        var sample_layout = new StackLayout(CategoryComboBox, SampleList);

        var map_layout = new PixelLayout();
        map_layout.SizeChanged += MapLayoutSizeChanged;
        map_layout.Add(MapControl, Point.Empty);
        map_layout.Add(LayerList, Point.Empty);

        Content = new DynamicLayout(new DynamicRow(sample_layout, map_layout)) { Spacing = new Size(4, 4) };
    }
    private void MapLayoutSizeChanged(object? sender, EventArgs e)
    {
        if (sender is PixelLayout layout)
        {
            MapControl.Size = layout.Size;
            layout.Move(LayerList, layout.Width - LayerList.Width, 0);
        }
    }

    private void FillListWithSamples()
    {
        var selectedCategory = CategoryComboBox.SelectedValue?.ToString() ?? "";
        SampleList.Items.Clear();
        foreach (var sample in AllSamples.GetSamples().Where(s => s.Category == selectedCategory))
            SampleList.Items.Add(CreateRadioButton(sample));

        if (SampleList.Items.First().Control is RadioButton radioButton)
        {
            radioButton.Checked = true;
        }
    }
    private void CategoryComboBox_SelectedValueChanged(object? sender, EventArgs e)
    {
        FillListWithSamples();
    }
    private void FillComboBoxWithCategories()
    {
        Mapsui.Tests.Common.Samples.Register();
        Mapsui.Samples.Common.Samples.Register();

        var categories = AllSamples.GetSamples().Select(s => s.Category).Distinct().OrderBy(c => c);
        foreach (var category in categories)
        {
            CategoryComboBox.Items.Add(category);
        }

        CategoryComboBox.SelectedIndex = 0;
    }
    private RadioButton CreateRadioButton(ISampleBase sample)
    {
        var radioButton = new RadioButton(SampleList.Items.FirstOrDefault()?.Control as RadioButton)
        {
            Font = Fonts.Cursive(12),
            Text = sample.Name,
        };

        radioButton.CheckedChanged += (s, e) =>
        {
            Catch.Exceptions(async () =>
            {
                MapControl.Map?.Layers.ClearAllGroups();

                await sample.SetupAsync(MapControl);

                LayerList.Items.Clear();
                if (MapControl.Map != null)
                    foreach (var layer in MapControl.Map.Layers)
                        LayerList.Items.Add(new LayerListItem(layer));
                LayerList.Items.Add(RotationSlider);
            });
        };
        return radioButton;
    }

    private void RotationSliderChanged(object? sender, EventArgs e)
    {
        var percent = (double)RotationSlider.Value / (RotationSlider.MaxValue - RotationSlider.MinValue);
        MapControl.Map.Navigator.RotateTo(percent * 360);
    }
}
