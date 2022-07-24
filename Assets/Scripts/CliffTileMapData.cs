using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CliffTile
{
    public static CliffTile empty => new CliffTile(int.MinValue);
    public bool isEmpty => Equals(empty);

    public CliffTile(int height)
    {
        this.height = height;
    }

    [SerializeField] public int height;

    public override bool Equals(object obj) =>
        obj is CliffTile tile
            && this.height == tile.height;

    public override int GetHashCode() => this.height.GetHashCode();
}

[Serializable]
public class CliffTileChunk
{
    public int x => _x;
    public int y => _y;
    public (int, int) xy => (_x, _y);
    public int population => _population;
    public uint nonce => _nonce;

    public CliffTile this[int x, int y]
    {
        get => _tiles[x + y * _chunkSize];
        set
        {
            int index = x + y * _chunkSize;
            if (_tiles[index].isEmpty && !value.isEmpty)
            {
                _population += 1;
            }

            if (!_tiles[index].isEmpty && value.isEmpty)
            {
                _population -= 1;
            }

            if (!_tiles.Equals(value))
            {
                _tiles[index] = value;
                UpdateNonce();
            }
        }
    }

    public CliffTileChunk(int x, int y, int chunkSize)
    {
        this._x = x;
        this._y = y;
        this._population = 0;
        this._chunkSize = chunkSize;
        this._nonce = 1;
        this._tiles = new CliffTile[chunkSize * chunkSize];
        Array.Fill(this._tiles, CliffTile.empty);

    }

    public void UpdateNonce()
    {
        _nonce += 1;
    }

    [SerializeField] private int _x;
    [SerializeField] private int _y;
    [SerializeField] private uint _nonce;
    [SerializeField] private CliffTile[] _tiles;

    // TODO infer it from above
    [SerializeField] private int _population;
    [SerializeField] private int _chunkSize;
}

[CreateAssetMenu(fileName = "CliffTileMapData", menuName = "Cliff/TileMapData", order = 2)]
public class CliffTileMapData : ScriptableObject, ISerializationCallbackReceiver
{
    public int chunkSize { get => 16; }
    public ReadOnlyCollection<CliffTileChunk> chunks => new ReadOnlyCollection<CliffTileChunk>(_chunksList);

    [SerializeField] private List<CliffTileChunk> _chunksList = new List<CliffTileChunk>();
    private Dictionary<(int, int), CliffTileChunk> _chunks = new Dictionary<(int, int), CliffTileChunk>();

    public CliffTile this[int x, int y]
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

    public CliffTileChunk GetChunk(int chunkX, int chunkY)
    {
        CliffTileChunk chunk;
        if (_chunks.TryGetValue((chunkX, chunkY), out chunk))
        {
            return chunk;
        }
        return null;
    }

    public CliffTileChunk GetChunkOfTile(int x, int y)
    {
        (int chunkX, int chunkY) = GetChunkCoord(x, y);
        return GetChunk(chunkX, chunkY);
    }

    public CliffTile GetTile(int x, int y)
    {
        CliffTileChunk chunk;
        if (!_chunks.TryGetValue(GetChunkCoord(x, y), out chunk))
        {
            return CliffTile.empty;
        }

        (int localX, int localY) = GetLocalCoord(x, y);
        return chunk[localX, localY];
    }

    public void SetTile(int x, int y, CliffTile tile)
    {
        (int chunkX, int chunkY) = GetChunkCoord(x, y);

        CliffTileChunk chunk;
        if (!_chunks.TryGetValue((chunkX, chunkY), out chunk))
        {
            if (tile.isEmpty)
            {
                return;
            }

            chunk = new CliffTileChunk(chunkX, chunkY, chunkSize);
            _chunks.Add((chunkX, chunkY), chunk);
            _chunksList.Add(chunk);
        }

        int localX = (x % chunkSize + chunkSize) % chunkSize;
        int localY = (y % chunkSize + chunkSize) % chunkSize;

        if (!chunk[localX, localY].Equals(tile))
        {
            var oldTile = chunk[localX, localY];
            chunk[localX, localY] = tile;

            UnityEditor.EditorUtility.SetDirty(this);

            // Mark dirty neighbors
            if (localX == 0 || localY == 0 || localX == chunkSize - 1 || localY == chunkSize - 1)
            {
                ForTileNeighbors(x, y, (i, x, y) =>
                {
                    var chunk = GetChunkOfTile(x, y);
                    if (chunk != null)
                    {
                        chunk.UpdateNonce();
                    }
                });
            }
        }

        if (chunk.population == 0)
        {
            _chunks.Remove((chunkX, chunkY));
            // TODO fix search
            _chunksList.Remove(chunk);
        }
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        _chunks.Clear();
        foreach (var chunk in _chunksList)
        {
            _chunks.Add((chunk.x, chunk.y), chunk);
        }
    }

    public void ForChunkTileWithNeighbors(CliffTileChunk chunk, Action<int, int, CliffTile[]> f)
    {
        int offsetX = chunk.x * chunkSize;
        int offsetY = chunk.y * chunkSize;

        for (int y = 0; y < chunkSize; ++y)
        {
            for (int x = 0; x < chunkSize; ++x)
            {
                var tiles = new CliffTile[9];
                ForTileNeighbors(offsetX + x, offsetY + y, (i, x, y) =>
                {
                    tiles[i] = GetTile(x, y);
                });

                f(x, y, tiles);
            }
        }
    }

    public void ForTileNeighbors(int x, int y, Action<int, int, int> f)
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
}
