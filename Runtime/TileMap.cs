using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cliff
{
    public class TileMap : MonoBehaviour, ISerializationCallbackReceiver
    {
        public TileMapData tileMapData => _tileMapData;
        public TileSet tileSet => _tileSet;

        [SerializeField] private TileSet _tileSet;
        [SerializeField] private MeshFilter _chunkPrefab;
        [SerializeField] private TileMapData _tileMapData;

        [SerializeField]
        private List<ChunkData> _chunkDataList;

        private Dictionary<(int, int), ChunkData> _chunkDataDict = new Dictionary<(int, int), ChunkData>();

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            _chunkDataDict.Clear();
            foreach (var chunk in _chunkDataList)
            {
                _chunkDataDict[chunk.xy] = chunk;
            }
        }

        public void SyncWithData(bool regenerateAll = false)
        {
            foreach (var chunk in _tileMapData.chunks)
            {
                ChunkData chunkData;
                if (!_chunkDataDict.TryGetValue(chunk.xy, out chunkData))
                {
                    chunkData = new ChunkData(chunk.x, chunk.y);
                    _chunkDataList.Add(chunkData);
                    _chunkDataDict.Add(chunkData.xy, chunkData);

                    EditorUtility.SetDirty(this);
                }

                if (chunkData.meshFilter == null)
                {
                    var pos = Vector3.Scale(new Vector3(chunk.x, 0, chunk.y), _tileSet.blockSize) * tileMapData.chunkSize;
                    chunkData.meshFilter = RuntimeUtility.InstantiatePrefab<MeshFilter>(_chunkPrefab, pos, Quaternion.identity, transform);
                    chunkData.meshFilter.gameObject.name = $"Chunk {chunk.x}x{chunk.y}";
                    chunkData.nonce = 0;
                }

                if (regenerateAll || chunkData.nonce != chunk.nonce)
                {
                    GenChunkMesh(chunk, chunkData);
                    chunkData.nonce = chunk.nonce;
                }

                chunkData.visited = true;
            }

            for (int i = 0; i < _chunkDataList.Count;)
            {
                var chunkData = _chunkDataList[i];
                if (!chunkData.visited)
                {
                    _chunkDataList[i] = _chunkDataList[_chunkDataList.Count - 1];
                    _chunkDataList.RemoveAt(_chunkDataList.Count - 1);
                    _chunkDataDict.Remove(chunkData.xy);

                    DestroyImmediate(chunkData.meshFilter.gameObject);
                    EditorUtility.SetDirty(this);
                }
                else
                {
                    chunkData.visited = false;
                    i += 1;
                }
            }
        }

        private void OnValidate()
        {
            // TODO set all nonce to 0 if _tileMapData changed
        }

        private void GenChunkMesh(Chunk chunk, ChunkData chunkData)
        {
            var gen = new MeshGen(_tileSet);
            _tileMapData.ForChunkTiles(chunk, (x, y, tile) =>
            {
                if (tile.isAllEmpty)
                {
                    return;
                }

                int height;
                TileShape shape;
                _tileSet.GetHeightAndShape(tile, out height, out shape);

                if (!shape.isValid)
                {
                    return;
                }

                gen.Add(new Vector3Int(x, y, height), shape);
            });

            var mesh = new Mesh { name = $"CliffChunk{chunk.x}x{chunk.y}" };
            gen.Generate(mesh);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            chunkData.meshFilter.sharedMesh = mesh;
        }

        [Serializable]
        private class ChunkData
        {
            public ChunkData(int x, int y)
            {
                this._x = x;
                this._y = y;
                this.nonce = 0;
                this.visited = false;
            }

            public int x => _x;
            public int y => _y;
            public (int, int) xy => (_x, _y);

            [SerializeField] private int _x;
            [SerializeField] private int _y;

            [SerializeField]
            public MeshFilter meshFilter;

            [SerializeField]
            public uint nonce;

            [NonSerialized]
            public bool visited;
        }
    }
}