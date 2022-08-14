using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cliff
{
    public struct TileShape
    {
        public static TileShape invalid => new TileShape(-1, false);

        public TileShape(int value, bool isBorder)
        {
            this.value = value;
            this.isBorder = isBorder;
        }

        public bool isValid => value >= 0;

        public int value { get; private set; }
        public bool isBorder { get; private set; }
    }


    [CreateAssetMenu(fileName = "CliffTileSet", menuName = "Cliff/TileSet", order = 1)]
    public class TileSet : ScriptableObject
    {
        [SerializeField] private Matrix4x4 _transform = Matrix4x4.identity;
        [SerializeField] private Vector3 _blockSize;

        [SerializeField] private Mesh _meshGround;
        [SerializeField] private MeshSet _cliff;
        [SerializeField] private MeshSet _ground;

        [Serializable]
        public struct MeshSet
        {
            [SerializeField] public List<Mesh> _mesh0001;
            [SerializeField] public List<Mesh> _mesh0010;
            [SerializeField] public List<Mesh> _mesh0011;
            [SerializeField] public List<Mesh> _mesh0100;
            [SerializeField] public List<Mesh> _mesh0101;
            [SerializeField] public List<Mesh> _mesh0110;
            [SerializeField] public List<Mesh> _mesh0111;
            [SerializeField] public List<Mesh> _mesh1000;
            [SerializeField] public List<Mesh> _mesh1001;
            [SerializeField] public List<Mesh> _mesh1010;
            [SerializeField] public List<Mesh> _mesh1011;
            [SerializeField] public List<Mesh> _mesh1100;
            [SerializeField] public List<Mesh> _mesh1101;
            [SerializeField] public List<Mesh> _mesh1110;

            public Mesh GetMesh(int index, int variation)
            {
                var list = index switch
                {
                    0b0001 => _mesh0001,
                    0b0010 => _mesh0010,
                    0b0011 => _mesh0011,
                    0b0100 => _mesh0100,
                    0b0101 => _mesh0101,
                    0b0110 => _mesh0110,
                    0b0111 => _mesh0111,
                    0b1000 => _mesh1000,
                    0b1001 => _mesh1001,
                    0b1010 => _mesh1010,
                    0b1011 => _mesh1011,
                    0b1100 => _mesh1100,
                    0b1101 => _mesh1101,
                    0b1110 => _mesh1110,
                    _ => throw new InvalidOperationException($"shape.value = {index}"),
                };

                return list[variation % list.Count];
            }
        }

        public List<(Mesh, bool)> GetMesh(TileShape shape, int variation = 0)
        {
            var r = new List<(Mesh, bool)>();

            if (!shape.isBorder || shape.value == 0)
            {
                r.Add((_meshGround, true));
            }

            variation = UnityEngine.Random.Range(0, 256);

            if (shape.value != 0)
            {
                r.Add((_cliff.GetMesh(shape.value, variation), false));
                r.Add((_ground.GetMesh(shape.value, variation), true));
            }

            return r;
        }

        public Matrix4x4 transform => _transform;
        public Vector3 blockSize => _blockSize;

        public void GetHeightAndShape(Tile tile, out int height, out TileShape shape)
        {
            if (tile.isAllEmpty)
            {
                height = 0;
                shape = TileShape.invalid;
                return;
            }

            int heightMin = tile.Aggregate(int.MaxValue, (acc, e) => Math.Min(acc, e.height));
            int heightMax = tile.Aggregate(int.MinValue, (acc, e) => Math.Max(acc, e.height));

            height = heightMin;

            if (heightMax - height > 1)
            {
                shape = TileShape.invalid;
                return;
            }

            int shapeValue
                = ((tile[0].height - height) << 0)
                | ((tile[1].height - height) << 1)
                | ((tile[2].height - height) << 2)
                | ((tile[3].height - height) << 3);

            shape = new TileShape(shapeValue, tile.isAnyEmpty);
        }

        private void OnValidate()
        {
            Debug.Assert(blockSize.x > 0);
            Debug.Assert(blockSize.y > 0);
            Debug.Assert(blockSize.z > 0);
        }
    }
}