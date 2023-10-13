using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using Npgsql;
using Tile = NetTopologySuite.IO.VectorTiles.Tiles.Tile;

namespace VectorTileExample.Controllers;

public class TileController : Controller
{
    [Route("tile/test1/{z}/{x}/{y}")]
    public IActionResult Test1(int x, int y, int z)
    {
        var tile = new Tile(x, y, z);
        var vectorTile = new VectorTile(){ TileId = tile.Id };
        var layer = new Layer(){ Name = "points"};
        
        using var connection = new NpgsqlConnection("server=localhost;port=5432;username=example;password=example;database=vector_tile_example");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT ST_ASTEXT(location) AS geom, name FROM points WHERE ST_INTERSECTS(location, ST_TRANSFORM(ST_TILEENVELOPE(@z, @x, @y), 4326))";
        cmd.Parameters.Add(new NpgsqlParameter("x", x));
        cmd.Parameters.Add(new NpgsqlParameter("y", y));
        cmd.Parameters.Add(new NpgsqlParameter("z", z));
        
        var wktReader = new WKTReader();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var geom = wktReader.Read((string)reader["geom"]);
            var attribute = new AttributesTable(new Dictionary<string, object>()
            {
                { "name", (string)reader["name"] }
            });
            layer.Features.Add(new Feature(geom, attribute));
        }
        vectorTile.Layers.Add(layer);

        using var ms = new MemoryStream();
        vectorTile.Write(ms);

        return File(ms.ToArray(), "application/pbf", $"{z}.{x}.{y}.pbf");

    }
    
    [Route("tile/test2/{z}/{x}/{y}")]
    public IActionResult Test2(int x, int y, int z)
    {
        using var connection = new NpgsqlConnection("server=localhost;port=5432;username=example;password=example;database=vector_tile_example");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT ST_AsMvt(t.*, 'points') FROM (SELECT ST_AsMVTGeom(location, ST_TRANSFORM(ST_TILEENVELOPE(@z, @x, @y), 4326)) AS geom, name FROM points WHERE ST_INTERSECTS(location, ST_TRANSFORM(ST_TILEENVELOPE(@z, @x, @y), 4326))) AS t";
        cmd.Parameters.Add(new NpgsqlParameter("x", x));
        cmd.Parameters.Add(new NpgsqlParameter("y", y));
        cmd.Parameters.Add(new NpgsqlParameter("z", z));
        
        using var reader = cmd.ExecuteReader();
        var data = new List<byte>();
        while (reader.Read())
        {
            if (reader[0] is byte[] b)
            {
                data.AddRange(b);
            }
        }

        return File(data.ToArray(), "application/pbf", $"{z}.{x}.{y}.pbf");

    }
}