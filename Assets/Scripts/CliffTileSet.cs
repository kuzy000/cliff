using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct CliffTileShape
{
    public static CliffTileShape invalid => new CliffTileShape(-1);

    public CliffTileShape(int value)
    {
        this.value = value;
    }

    public bool isValid => value >= 0;

    public int value { get; private set; }
}

[CreateAssetMenu(fileName = "CliffTileSet", menuName = "Cliff/TileSet", order = 1)]
public class CliffTileSet : ScriptableObject
{
    [SerializeField] private Matrix4x4 _transform;
    [SerializeField] private Vector3 _blockSize;

    [SerializeField] private Mesh _cliff0000;
    [SerializeField] private Mesh _cliff0001;
    [SerializeField] private Mesh _cliff0010;
    [SerializeField] private Mesh _cliff0011;
    [SerializeField] private Mesh _cliff0100;
    [SerializeField] private Mesh _cliff0101;
    [SerializeField] private Mesh _cliff0110;
    [SerializeField] private Mesh _cliff0111;
    [SerializeField] private Mesh _cliff1000;
    [SerializeField] private Mesh _cliff1001;
    [SerializeField] private Mesh _cliff1010;
    [SerializeField] private Mesh _cliff1011;
    [SerializeField] private Mesh _cliff1100;
    [SerializeField] private Mesh _cliff1101;
    [SerializeField] private Mesh _cliff1110;
    [SerializeField] private Mesh _cliff1111;

    public Mesh GetMesh(CliffTileShape shape) => shape.value switch
    {
        0b0000 => _cliff0000,
        0b0001 => _cliff0001,
        0b0010 => _cliff0010,
        0b0011 => _cliff0011,
        0b0100 => _cliff0100,
        0b0101 => _cliff0101,
        0b0110 => _cliff0110,
        0b0111 => _cliff0111,
        0b1000 => _cliff1000,
        0b1001 => _cliff1001,
        0b1010 => _cliff1010,
        0b1011 => _cliff1011,
        0b1100 => _cliff1100,
        0b1101 => _cliff1101,
        0b1110 => _cliff1110,
        0b1111 => _cliff1111,
        _ => throw new InvalidOperationException($"shape.value = {shape.value}"),
    };

    public Vector3 blockSize => _blockSize;

    public void GetHeightAndShape(CliffTile tile, out int height, out CliffTileShape shape)
    {
        if (tile.isEmpty)
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

        shape = new CliffTileShape(shapeValue);
    }

    private void OnValidate()
    {
        Debug.Assert(blockSize.x > 0);
        Debug.Assert(blockSize.y > 0);
        Debug.Assert(blockSize.z > 0);
    }
}
