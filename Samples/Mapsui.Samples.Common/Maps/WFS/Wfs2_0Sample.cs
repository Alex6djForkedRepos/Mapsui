﻿using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Providers.Wfs;
using Mapsui.Styles;
using Mapsui.Widgets.InfoWidgets;
using System.Net;
using System.Threading.Tasks;

#pragma warning disable IDISP001 // Dispose created

namespace Mapsui.Samples.Common.Maps.DataFormats;

public class Wfs2_0Sample : ISample
{
    public string Name => "WFS 2.0";
    public string Category => "WFS";

    private const string crs = "EPSG:31254";

    public async Task<Map> CreateMapAsync()
    {
        try
        {
            var map = new Map { CRS = crs };
            // The default resolution was changed from 1 to 0 which broke this test so the resolution is set to one 1 explicitly.
            map.Navigator.SetViewport(map.Navigator.Viewport with { Resolution = 1 });
            var provider = await CreateWfsProviderAsync();
            map.Layers.Add(CreateWfsLayer(provider));

            map.Widgets.Add(new MapInfoWidget(map, l => l.Name == "Laser Points"));

            MRect bbox = new(
                -34900
                , 255900
                , -34800
                , 256000
            );

            map.Navigator.OverridePanBounds = bbox;
            map.Navigator.PanLock = true;
            map.Navigator.ZoomToPanBounds();

            return map;

        }
        catch (WebException ex)
        {
            Logger.Log(LogLevel.Warning, ex.Message, ex);
            throw;
        }
    }

    private static ILayer CreateWfsLayer(WFSProvider provider)
    {
        return new Layer("Laser Points")
        {
            Style = new SymbolStyle()
            {
                Outline = new Pen(Color.Gray, 1f),
                Fill = new Brush(Color.Red),
                SymbolScale = 1
            },
            DataSource = provider,
        };
    }

    private static async Task<WFSProvider> CreateWfsProviderAsync()
    {
        var provider = await WFSProvider.CreateAsync(
            "https://vogis.cnv.at/geoserver/vogis/laser_2002_04_punkte/ows",
            "vogis",
            "laser_2002_04_punkte",
            WFSProvider.WFSVersionEnum.WFS_2_0_0);

        provider.GetFeatureGetRequest = true;
        provider.CRS = crs;
        provider.AxisOrder = [0, 1];

        await provider.InitAsync();

        return provider;
    }
}
