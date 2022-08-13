using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct CliffTileShape
{
    public static CliffTileShape invalid => new CliffTileShape(-1, false);

    public CliffTileShape(int value, bool isBorder)
    {
        this.value = value;
        this.isBorder = isBorder;
    }

    public bool isValid => value >= 0;

    public int value { get; private set; }
    public bool isBorder { get; private set; }
}


[CreateAssetMenu(fileName = "CliffTileSet", menuName = "Cliff/TileSet", order = 1)]
public class CliffTileSet : ScriptableObject
{
    [SerializeField] private Matrix4x4 _transform = Matrix4x4.identity;
    [SerializeField] private Vector3 _blockSize;

    [SerializeField] private Mesh _meshGround;
    [SerializeField] private MeshSet _cliff;
    [SerializeField] private MeshSet _ground;

    [Serializable]
    public struct MeshSet
    {
        [SerializeField] public Mesh mesh0001;
        [SerializeField] public Mesh mesh0010;
        [SerializeField] public Mesh mesh0011;
        [SerializeField] public Mesh mesh0100;
        [SerializeField] public Mesh mesh0101;
        [SerializeField] public Mesh mesh0110;
        [SerializeField] public Mesh mesh0111;
        [SerializeField] public Mesh mesh1000;
        [SerializeField] public Mesh mesh1001;
        [SerializeField] public Mesh mesh1010;
        [SerializeField] public Mesh mesh1011;
        [SerializeField] public Mesh mesh1100;
        [SerializeField] public Mesh mesh1101;
        [SerializeField] public Mesh mesh1110;
    }

    public List<(Mesh, bool)> GetMesh(CliffTileShape shape)
    {
        var r = new List<(Mesh, bool)>();

        if (!shape.isBorder)
        {
            r.Add((_meshGround, true));
        }

        switch (shape.value)
        {
            case 0b0000: { break; }
            case 0b0001: { r.Add((_cliff.mesh0001, false)); r.Add((_ground.mesh0001, true)); break; }
            case 0b0010: { r.Add((_cliff.mesh0010, false)); r.Add((_ground.mesh0010, true)); break; }
            case 0b0011: { r.Add((_cliff.mesh0011, false)); r.Add((_ground.mesh0011, true)); break; }
            case 0b0100: { r.Add((_cliff.mesh0100, false)); r.Add((_ground.mesh0100, true)); break; }
            case 0b0101: { r.Add((_cliff.mesh0101, false)); r.Add((_ground.mesh0101, true)); break; }
            case 0b0110: { r.Add((_cliff.mesh0110, false)); r.Add((_ground.mesh0110, true)); break; }
            case 0b0111: { r.Add((_cliff.mesh0111, false)); r.Add((_ground.mesh0111, true)); break; }
            case 0b1000: { r.Add((_cliff.mesh1000, false)); r.Add((_ground.mesh1000, true)); break; }
            case 0b1001: { r.Add((_cliff.mesh1001, false)); r.Add((_ground.mesh1001, true)); break; }
            case 0b1010: { r.Add((_cliff.mesh1010, false)); r.Add((_ground.mesh1010, true)); break; }
            case 0b1011: { r.Add((_cliff.mesh1011, false)); r.Add((_ground.mesh1011, true)); break; }
            case 0b1100: { r.Add((_cliff.mesh1100, false)); r.Add((_ground.mesh1100, true)); break; }
            case 0b1101: { r.Add((_cliff.mesh1101, false)); r.Add((_ground.mesh1101, true)); break; }
            case 0b1110: { r.Add((_cliff.mesh1110, false)); r.Add((_ground.mesh1110, true)); break; }
            default: { throw new InvalidOperationException($"shape.value = {shape.value}"); }
        };

        return r;
    }

    public Matrix4x4 transform => _transform;
    public Vector3 blockSize => _blockSize;

    public void GetHeightAndShape(CliffTile tile, out int height, out CliffTileShape shape)
    {
        if (tile.isAllEmpty)
        {
            height = 0;
            shape = CliffTileShape.invalid;
            return;
        }

        int heightMin = tile.Aggregate(int.MaxValue, (acc, e) => Math.Min(acc, e.height));
        int heightMax = tile.Aggregate(int.MinValue, (acc, e) => Math.Max(acc, e.height));

        height = heightMin;

        if (heightMax - height > 1)
        {
            shape = CliffTileShape.invalid;
            return;
        }

        int shapeValue
            = ((tile[0].height - height) << 0)
            | ((tile[1].height - height) << 1)
            | ((tile[2].height - height) << 2)
            | ((tile[3].height - height) << 3);

        shape = new CliffTileShape(shapeValue, tile.isAnyEmpty);
    }

    private void OnValidate()
    {
        Debug.Assert(blockSize.x > 0);
        Debug.Assert(blockSize.y > 0);
        Debug.Assert(blockSize.z > 0);
    }
}
