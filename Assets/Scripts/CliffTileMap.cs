using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CliffTileMap : MonoBehaviour
{
    [SerializeField] private CliffTileSet _tileSet;
    [SerializeField] private MeshFilter _chunkPrefab;

    public struct Tile
    {
        public int height;

        public override bool Equals(object obj) =>
            obj is Tile tile
                && this.height == tile.height;

        public override int GetHashCode() => this.height.GetHashCode();
    }

    public delegate void ChunkHandler(int x, int y);
    public delegate void TileHandler(int x, int y, Tile? tile);

    public event ChunkHandler onCreateChunk;
    public event ChunkHandler onRemoveChunk;
    public event TileHandler onTileSet;

    public int chunkSize { get => 16; }

    public IEnumerator<(int, int)> chunkEnumerator => _chunks.Keys.GetEnumerator();

    public Vector3 blockSize => _tileSet.blockSize;

    public Tile? this[int x, int y]
    {
        get => GetTile(x, y);
        set => SetTile(x, y, value);
    }

    public (int, int) GetChunkCoord(int x, int y) => (
        (x - (x < 0 ? chunkSize - 1 : 0)) / chunkSize,
        (y - (y < 0 ? chunkSize - 1 : 0)) / chunkSize);

    // In-chunk coords
    public (int, int) GetLocalCoord(int x, int y) =>
        ((x % chunkSize + chunkSize) % chunkSize, (y % chunkSize + chunkSize) % chunkSize);

    public Tile? GetTile(int x, int y)
    {
        Chunk chunk;
        if (!_chunks.TryGetValue(GetChunkCoord(x, y), out chunk))
        {
            return null;
        }

        (int localX, int localY) = GetLocalCoord(x, y);
        return chunk.tiles[localX, localY];
    }

    public void SetTile(int x, int y, Tile? tile)
    {
        (int chunkX, int chunkY) = GetChunkCoord(x, y);

        Chunk chunk;
        if (!_chunks.TryGetValue((chunkX, chunkY), out chunk))
        {
            if (tile == null)
            {
                return;
            }

            var pos = Vector3.Scale(new Vector3(chunkX, 0, chunkY), blockSize) * chunkSize;
            var meshFilter = Instantiate<MeshFilter>(_chunkPrefab, pos, Quaternion.identity, transform);
            meshFilter.gameObject.name = $"Chunk {chunkX}x{chunkY}";
            chunk = new Chunk(chunkX, chunkY, meshFilter, chunkSize);

            _chunks.Add((chunkX, chunkY), chunk);

            onCreateChunk(chunkX, chunkY);
        }

        int localX = (x % chunkSize + chunkSize) % chunkSize;
        int localY = (y % chunkSize + chunkSize) % chunkSize;

        if (chunk.tiles[localX, localY] == null & tile != null)
        {
            chunk.population += 1;
        }

        if (chunk.tiles[localX, localY] != null & tile == null)
        {
            chunk.population -= 1;
        }

        if (!chunk.tiles[localX, localY].Equals(tile))
        {
            chunk.tiles[localX, localY] = tile;
            chunk.dirty = true;

            // Mark dirty neighbors
            if (localX == 0 || localY == 0 || localX == chunkSize - 1 || localY == chunkSize - 1)
            {
                For3x3Grid(x, y, (i, x, y) =>
                {
                    (int chunkX, int chunkY) = GetChunkCoord(x, y);

                    Chunk chunk;
                    if (_chunks.TryGetValue((chunkX, chunkY), out chunk))
                    {
                        (int localX, int localY) = GetLocalCoord(x, y);
                        if (chunk.tiles[localX, localY] != null)
                        {
                            chunk.dirty = true;
                        }
                    }
                });
            }

            onTileSet(x, y, tile);
        }

        if (chunk.population == 0)
        {
            onRemoveChunk(chunkX, chunkY);
            Destroy(chunk.meshFilter);
            _chunks.Remove((chunkX, chunkY));
        }
    }

    public bool CanSetTile(int x, int y, Tile? tile)
    {
        if (tile == null)
        {
            return true;
        }

        var tiles = new Tile?[9];
        For3x3Grid(x, y, (i, x, y) =>
        {
            tiles[i] = GetTile(x, y);
        });
        tiles[4] = tile;

        var shapes = GetShapes(x, y, tiles);
        return Array.TrueForAll(shapes, shape => shape != CliffTileSet.Shape.Unknown);
    }

    public void UpdateChunks()
    {
        foreach (var chunk in _chunks.Values)
        {
            if (chunk.dirty)
            {
                GenChunkMesh(chunk);
                chunk.dirty = false;
            }
        }
    }

    private CliffTileSet.Shape[] GetShapes(int x, int y, Tile?[] tiles)
    {
        Debug.Assert(tiles[4] != null);

        var hs = new int[9];
        for (int i = 0; i < hs.Length; ++i)
        {
            hs[i] = tiles[i] != null ? tiles[i].Value.height : 0;
        }
        int h = hs[4];

        for (int i = 0; i < hs.Length; ++i)
        {
            hs[i] -= h;
        }

        return new CliffTileSet.Shape[]
        {
            ShapeByNeighborsHeight(hs[3], hs[6], hs[7]),
            ShapeByNeighborsHeight(hs[5], hs[8], hs[7]),
            ShapeByNeighborsHeight(hs[3], hs[0], hs[1]),
            ShapeByNeighborsHeight(hs[5], hs[2], hs[1]),
        };
    }

    private void GenChunkMesh(Chunk chunk)
    {
        var gen = new CliffMeshGen(_tileSet, chunk.population);
        OnChunkNeighbors(chunk, (x, y, tiles) =>
        {
            var tile = tiles[4];
            if (tile == null)
            {
                return;
            }

            gen.Add(new Vector3Int(x, y, tile.Value.height - 1), GetShapes(x, y, tiles));
        });

        var mesh = new Mesh { name = $"CliffChunk{chunk.x}x{chunk.y}" };
        gen.Generate(mesh);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        chunk.meshFilter.sharedMesh = mesh;
    }

    List<Mesh> meshes;

    private class Chunk
    {
        public Chunk(int x, int y, MeshFilter meshFilter, int chunkSize)
        {
            this.x = x;
            this.y = y;
            this.meshFilter = meshFilter;
            this.tiles = new Tile?[chunkSize, chunkSize];
            this.population = 0;
            this.dirty = false;
        }

        public readonly int x;
        public readonly int y;
        public MeshFilter meshFilter;
        public Tile?[,] tiles;
        public int population;
        public bool dirty;
    }

    private Dictionary<(int, int), Chunk> _chunks = new Dictionary<(int, int), Chunk>();

    private void OnChunkNeighbors(Chunk chunk, Action<int, int, Tile?[]> f)
    {
        int offsetX = chunk.x * chunkSize;
        int offsetY = chunk.y * chunkSize;

        for (int y = 0; y < chunkSize; ++y)
        {
            for (int x = 0; x < chunkSize; ++x)
            {
                var tiles = new Tile?[9];
                For3x3Grid(offsetX + x, offsetY + y, (i, x, y) =>
                {
                    tiles[i] = GetTile(x, y);
                });

                f(x, y, tiles);
            }
        }
    }

    private void For3x3Grid(int x, int y, Action<int, int, int> f)
    {
        int i = 0;
        for (int iy = -1; iy < 2; ++iy)
        {
            for (int ix = -1; ix < 2; ++ix)
            {
                f(i++, x + ix, y + iy);
            }
        }
    }

    private CliffTileSet.Shape ShapeByNeighborsHeight(int n1, int n2, int n3)
    {
        var v = new Vector3Int(n1, n2, n3);

        return v switch
        {
            { x: 0, y: 0, z: 0 } => CliffTileSet.Shape.Plane,
            { x: 1, y: 0, z: 0 } => CliffTileSet.Shape.Plane,
            { x: 0, y: 1, z: 0 } => CliffTileSet.Shape.Plane,
            { x: 0, y: 0, z: 1 } => CliffTileSet.Shape.Plane,
            { x: 1, y: 1, z: 0 } => CliffTileSet.Shape.Plane,
            { x: 0, y: 1, z: 1 } => CliffTileSet.Shape.Plane,
            { x: 1, y: 0, z: 1 } => CliffTileSet.Shape.Plane,
            { x: 1, y: 1, z: 1 } => CliffTileSet.Shape.Plane,

            { x: -1, y: -0, z: -0 } => CliffTileSet.Shape.SideV,
            { x: -0, y: -1, z: -0 } => CliffTileSet.Shape.Hole,
            { x: -0, y: -0, z: -1 } => CliffTileSet.Shape.SideH,

            { x: -1, y: -1, z: -0 } => CliffTileSet.Shape.SideV,
            { x: -1, y: -0, z: -1 } => CliffTileSet.Shape.TwoSide,
            { x: -0, y: -1, z: -1 } => CliffTileSet.Shape.SideH,

            { x: -1, y: -1, z: -1 } => CliffTileSet.Shape.TwoSide,
            _ => CliffTileSet.Shape.Unknown,
        };
    }
}
