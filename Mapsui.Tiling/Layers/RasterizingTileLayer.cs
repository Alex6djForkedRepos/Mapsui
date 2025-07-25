﻿using System.Collections.Generic;
using System.Threading.Tasks;
using BruTile.Cache;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Manipulations;
using Mapsui.Projections;
using Mapsui.Rendering;
using Mapsui.Tiling.Fetcher;
using Mapsui.Tiling.Provider;
using Mapsui.Tiling.Rendering;

namespace Mapsui.Tiling.Layers;

/// <summary>
/// Rasterizing Tile Layer. A Layer that Rasterizes and Tiles the Layer. For Faster Performance.
/// It recreates the Tiles if Data is changed.
/// </summary>
public class RasterizingTileLayer : TileLayer, ISourceLayer, IFetchableSource, ILayerFeatureInfo
{
    /// <summary>
    ///     Creates a RasterizingTileLayer which rasterizes a layer for performance
    /// </summary>
    /// <param name="layer">The Layer to be rasterized</param>
    /// <param name="pixelDensity"></param>
    /// <param name="minTiles">Minimum number of tiles to cache</param>
    /// <param name="maxTiles">Maximum number of tiles to cache</param>
    /// <param name="dataFetchStrategy">Strategy to get list of tiles for given extent</param>
    /// <param name="renderFetchStrategy"></param>
    /// <param name="minExtraTiles">Number of minimum extra tiles for memory cache</param>
    /// <param name="maxExtraTiles">Number of maximum extra tiles for memory cache</param>
    /// <param name="persistentCache">Persistent Cache</param>
    /// <param name="projection">Projection</param>
    /// <param name="renderFormat">Format to Render To</param>
    public RasterizingTileLayer(
        ILayer layer,
        float pixelDensity = 1,
        int minTiles = 200,
        int maxTiles = 300,
        IDataFetchStrategy? dataFetchStrategy = null,
        IRenderFetchStrategy? renderFetchStrategy = null,
        int minExtraTiles = -1,
        int maxExtraTiles = -1,
        IPersistentCache<byte[]>? persistentCache = null,
        IProjection? projection = null,
        RenderFormat renderFormat = RenderFormat.Png) : base(
        new RasterizingTileSource(layer, pixelDensity, persistentCache, projection, renderFormat),
        minTiles,
        maxTiles,
        dataFetchStrategy,
        renderFetchStrategy ?? new TilingRenderFetchStrategy(null),
        minExtraTiles,
        maxExtraTiles)
    {
        SourceLayer = layer;
        Name = layer.Name;
        SourceLayer.DataChanged += (s, e) =>
        {
            ClearCache(); // It would cause less flicker if we could invalidate the tiles so that they could still be used by the renderer but would be replaced by the fetcher.
            DataHasChanged();
            OnFetchRequested();
        };
    }

    public ILayer SourceLayer { get; }
    private RasterizingTileSource RasterizingTileSource => (RasterizingTileSource)TileSource;
    public Task<IDictionary<string, IEnumerable<IFeature>>> GetFeatureInfoAsync(Viewport viewport, ScreenPosition screenPosition)
    {
        return RasterizingTileSource.GetFeatureInfoAsync(viewport, screenPosition);
    }
    public override void ViewportChanged(FetchInfo fetchInfo)
    {
        Busy = true;
        base.ViewportChanged(fetchInfo);
        if (SourceLayer is IFetchableSource fetchableSource)
            fetchableSource.ViewportChanged(fetchInfo);
    }
}
