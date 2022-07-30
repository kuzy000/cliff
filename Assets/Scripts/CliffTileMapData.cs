using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CliffVertex
{
    public static CliffVertex empty => new CliffVertex(int.MinValue);
    public bool isEmpty => Equals(empty);
    public int height { get => isEmpty ? 0 : _height; set => _height = value; }

    public CliffVertex(int height)
    {
        this._height = height;
    }

    [SerializeField] private int _height;

    public override bool Equals(object obj) =>
        obj is CliffVertex tile
            && this._height == tile._height;

    public override int GetHashCode() => this._height.GetHashCode();
}

public struct CliffTile : IEnumerable<CliffVertex>
{
    public CliffTile(CliffVertex bl, CliffVertex br, CliffVertex tr, CliffVertex tl)
    {
        this.bl = bl;
        this.br = br;
        this.tr = tr;
        this.tl = tl;
    }

    public CliffVertex bl { get; set; }
    public CliffVertex br { get; set; }
    public CliffVertex tr { get; set; }
    public CliffVertex tl { get; set; }

    public bool isEmpty
        => bl.isEmpty && br.isEmpty && tr.isEmpty && tl.isEmpty;

    public CliffVertex this[int index] => index switch
    {
        0 => bl,
        1 => br,
        2 => tr,
        3 => tl,
        _ => throw new InvalidOperationException($"index = {index}"),
    };

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<CliffVertex> GetEnumerator()
    {
        yield return bl;
        yield return br;
        yield return tr;
        yield return tl;
    }
}

[Serializable]
public class CliffChunk
{
    public int x => _x;
    public int y => _y;
    public (int, int) xy => (_x, _y);
    public int population => _population;
    public uint nonce => _nonce;

    public CliffVertex this[int x, int y]
    {
        get => _vertices[x + y * _chunkSize];
        set
        {
            int index = x + y * _chunkSize;
            if (_vertices[index].isEmpty && !value.isEmpty)
            {
                _population += 1;
            }

            if (!_vertices[index].isEmpty && value.isEmpty)
            {
                _population -= 1;
            }

            if (!_vertices.Equals(value))
            {
                _vertices[index] = value;
                UpdateNonce();
            }
        }
    }

    public CliffChunk(int x, int y, int chunkSize)
    {
        this._x = x;
        this._y = y;
        this._population = 0;
        this._chunkSize = chunkSize;
        this._nonce = 1;
        this._vertices = new CliffVertex[chunkSize * chunkSize];
        Array.Fill(this._vertices, CliffVertex.empty);
    }

    public void UpdateNonce()
    {
        _nonce += 1;
    }

    [SerializeField] private int _x;
    [SerializeField] private int _y;
    [SerializeField] private uint _nonce;
    [SerializeField] private CliffVertex[] _vertices;

    // TODO infer it from above
    [SerializeField] private int _population;
    [SerializeField] private int _chunkSize;
}

[CreateAssetMenu(fileName = "CliffTileMapData", menuName = "Cliff/TileMapData", order = 2)]
public class CliffTileMapData : ScriptableObject, ISerializationCallbackReceiver
{
    public int chunkSize { get => 16; }
    public ReadOnlyCollection<CliffChunk> chunks => new ReadOnlyCollection<CliffChunk>(_chunksList);

    [SerializeField] private List<CliffChunk> _chunksList = new List<CliffChunk>();
    private Dictionary<(int, int), CliffChunk> _chunks = new Dictionary<(int, int), CliffChunk>();

    public CliffVertex this[int x, int y]
    {
        get => GetVertex(x, y);
        set => SetVertex(x, y, value);
    }

    public (int, int) GetChunkCoord(int x, int y) => (
        (x - (x < 0 ? chunkSize - 1 : 0)) / chunkSize,
        (y - (y < 0 ? chunkSize - 1 : 0)) / chunkSize);

    // In-chunk coords
    public (int, int) GetLocalCoord(int x, int y) =>
        ((x % chunkSize + chunkSize) % chunkSize, (y % chunkSize + chunkSize) % chunkSize);

    public CliffChunk GetChunk(int chunkX, int chunkY)
    {
        CliffChunk chunk;
        if (_chunks.TryGetValue((chunkX, chunkY), out chunk))
        {
            return chunk;
        }
        return null;
    }

    public CliffChunk GetChunkOfVertex(int x, int y)
    {
        (int chunkX, int chunkY) = GetChunkCoord(x, y);
        return GetChunk(chunkX, chunkY);
    }

    public CliffVertex GetVertex(int x, int y)
    {
        CliffChunk chunk;
        if (!_chunks.TryGetValue(GetChunkCoord(x, y), out chunk))
        {
            return CliffVertex.empty;
        }

        (int localX, int localY) = GetLocalCoord(x, y);
        return chunk[localX, localY];
    }

    public void SetVertex(int x, int y, CliffVertex vertex)
    {
        (int chunkX, int chunkY) = GetChunkCoord(x, y);

        CliffChunk chunk;
        if (!_chunks.TryGetValue((chunkX, chunkY), out chunk))
        {
            if (vertex.isEmpty)
            {
                return;
            }

            chunk = new CliffChunk(chunkX, chunkY, chunkSize);
            _chunks.Add((chunkX, chunkY), chunk);
            _chunksList.Add(chunk);
        }

        int localX = (x % chunkSize + chunkSize) % chunkSize;
        int localY = (y % chunkSize + chunkSize) % chunkSize;

        if (!chunk[localX, localY].Equals(vertex))
        {
            var oldVertex = chunk[localX, localY];
            chunk[localX, localY] = vertex;

            UnityEditor.EditorUtility.SetDirty(this);

            // Mark dirty neighbors
            if (localX == 0 || localY == 0 || localX == chunkSize - 1 || localY == chunkSize - 1)
            {
                ForVertexNeighbors(x, y, (i, x, y) =>
                {
                    var chunk = GetChunkOfVertex(x, y);
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

    public CliffTile GetTile(int x, int y)
    {
        return new CliffTile(
            GetVertex(x + 0, y + 0),
            GetVertex(x + 1, y + 0),
            GetVertex(x + 1, y + 1),
            GetVertex(x + 0, y + 1)
        );
    }

    public void ForChunkTiles(CliffChunk chunk, Action<int, int, CliffTile> f)
    {
        int offsetX = chunk.x * chunkSize;
        int offsetY = chunk.y * chunkSize;

        for (int y = 0; y < chunkSize; ++y)
        {
            for (int x = 0; x < chunkSize; ++x)
            {
                f(x, y, GetTile(offsetX + x, offsetY + y));
            }
        }
    }

    public void ForVertexNeighbors(int x, int y, Action<int, int, int> f)
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
