using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace Cliff
{
    [Serializable]
    public struct Vertex
    {
        public static Vertex empty => new Vertex(int.MinValue);
        public bool isEmpty => Equals(empty);
        public int height { get => isEmpty ? 0 : _height; set => _height = value; }

        public Vertex(int height)
        {
            this._height = height;
        }

        [SerializeField] private int _height;

        public override bool Equals(object obj) =>
            obj is Vertex tile
                && this._height == tile._height;

        public override int GetHashCode() => this._height.GetHashCode();
    }

    public struct Tile : IEnumerable<Vertex>
    {
        public Tile(Vertex bl, Vertex br, Vertex tr, Vertex tl)
        {
            this.bl = bl;
            this.br = br;
            this.tr = tr;
            this.tl = tl;
        }

        public Vertex bl { get; set; }
        public Vertex br { get; set; }
        public Vertex tr { get; set; }
        public Vertex tl { get; set; }

        public bool isAllEmpty
            => bl.isEmpty && br.isEmpty && tr.isEmpty && tl.isEmpty;

        public bool isAnyEmpty
            => bl.isEmpty || br.isEmpty || tr.isEmpty || tl.isEmpty;

        public Vertex this[int index] => index switch
        {
            0 => bl,
            1 => br,
            2 => tr,
            3 => tl,
            _ => throw new InvalidOperationException($"index = {index}"),
        };

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<Vertex> GetEnumerator()
        {
            yield return bl;
            yield return br;
            yield return tr;
            yield return tl;
        }
    }

    [Serializable]
    public class Chunk
    {
        public int x => _x;
        public int y => _y;
        public (int, int) xy => (_x, _y);
        public int population => _population;
        public uint nonce => _nonce;

        public Vertex this[int x, int y]
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

        public Chunk(int x, int y, int chunkSize)
        {
            this._x = x;
            this._y = y;
            this._population = 0;
            this._chunkSize = chunkSize;
            this._nonce = 1;
            this._vertices = new Vertex[chunkSize * chunkSize];
            Array.Fill(this._vertices, Vertex.empty);
        }

        public void UpdateNonce()
        {
            _nonce += 1;
        }

        [SerializeField] private int _x;
        [SerializeField] private int _y;
        [SerializeField] private uint _nonce;
        [SerializeField] private Vertex[] _vertices;

        // TODO infer it from above
        [SerializeField] private int _population;
        [SerializeField] private int _chunkSize;
    }

    [CreateAssetMenu(fileName = "CliffTileMapData", menuName = "Cliff/TileMapData", order = 2)]
    public class TileMapData : ScriptableObject, ISerializationCallbackReceiver
    {
        public int chunkSize { get => 16; }
        public ReadOnlyCollection<Chunk> chunks => new ReadOnlyCollection<Chunk>(_chunksList);

        [SerializeField] private List<Chunk> _chunksList = new List<Chunk>();
        private Dictionary<(int, int), Chunk> _chunks = new Dictionary<(int, int), Chunk>();

        public Vertex this[int x, int y]
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

        public Chunk GetChunk(int chunkX, int chunkY)
        {
            Chunk chunk;
            if (_chunks.TryGetValue((chunkX, chunkY), out chunk))
            {
                return chunk;
            }
            return null;
        }

        public Chunk GetChunkOfVertex(int x, int y)
        {
            (int chunkX, int chunkY) = GetChunkCoord(x, y);
            return GetChunk(chunkX, chunkY);
        }

        public Vertex GetVertex(int x, int y)
        {
            Chunk chunk;
            if (!_chunks.TryGetValue(GetChunkCoord(x, y), out chunk))
            {
                return Vertex.empty;
            }

            (int localX, int localY) = GetLocalCoord(x, y);
            return chunk[localX, localY];
        }

        public void SetVertex(int x, int y, Vertex vertex)
        {
            (int chunkX, int chunkY) = GetChunkCoord(x, y);

            Chunk chunk;
            if (!_chunks.TryGetValue((chunkX, chunkY), out chunk))
            {
                if (vertex.isEmpty)
                {
                    return;
                }

                chunk = new Chunk(chunkX, chunkY, chunkSize);
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

        public Tile GetTile(int x, int y)
        {
            return new Tile(
                GetVertex(x + 0, y + 0),
                GetVertex(x + 1, y + 0),
                GetVertex(x + 1, y + 1),
                GetVertex(x + 0, y + 1)
            );
        }

        public void ForChunkTiles(Chunk chunk, Action<int, int, Tile> f)
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
}