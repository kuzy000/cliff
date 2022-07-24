using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CliffTileSet", menuName = "Cliff/TileSet", order = 1)]
public class CliffTileSet : ScriptableObject
{
    public enum Shape
    {
        Plane, TwoSide, SideV, SideH, Hole, Unknown
    }

    [SerializeField] private Matrix4x4 _transform;
    [SerializeField] private Vector3 _blockSize;

    [SerializeField] private Mesh _plane1;
    [SerializeField] private Mesh _plane2;
    [SerializeField] private Mesh _plane3;
    [SerializeField] private Mesh _plane4;

    [SerializeField] private Mesh _twoSide1;
    [SerializeField] private Mesh _twoSide2;
    [SerializeField] private Mesh _twoSide3;
    [SerializeField] private Mesh _twoSide4;

    [SerializeField] private Mesh _sideH1;
    [SerializeField] private Mesh _sideH2;
    [SerializeField] private Mesh _sideH3;
    [SerializeField] private Mesh _sideH4;

    [SerializeField] private Mesh _sideV1;
    [SerializeField] private Mesh _sideV2;
    [SerializeField] private Mesh _sideV3;
    [SerializeField] private Mesh _sideV4;

    [SerializeField] private Mesh _hole1;
    [SerializeField] private Mesh _hole2;
    [SerializeField] private Mesh _hole3;
    [SerializeField] private Mesh _hole4;

    public Vector3 blockSize => _blockSize;
    public Mesh[] plane => new Mesh[] { _plane1, _plane2, _plane3, _plane4 };
    public Mesh[] twoSide => new Mesh[] { _twoSide1, _twoSide2, _twoSide3, _twoSide4 };
    public Mesh[] sideH => new Mesh[] { _sideH1, _sideH2, _sideH3, _sideH4 };
    public Mesh[] sideV => new Mesh[] { _sideV1, _sideV2, _sideV3, _sideV4 };
    public Mesh[] hole => new Mesh[] { _hole1, _hole2, _hole3, _hole4 };

    public Mesh GetMesh(Shape kind, int index)
    {
        switch (kind)
        {
            case Shape.Plane: return plane[index];
            case Shape.TwoSide: return twoSide[index];
            case Shape.SideH: return sideH[index];
            case Shape.SideV: return sideV[index];
            case Shape.Hole: return hole[index];
            case Shape.Unknown: return plane[index];
        }

        return null;
    }

    public Shape[] GetTileShapes(int x, int y, CliffTile[] tileNeighbors)
    {
        Debug.Assert(!tileNeighbors[4].isEmpty);

        var hs = new int[9];
        for (int i = 0; i < hs.Length; ++i)
        {
            hs[i] = !tileNeighbors[i].isEmpty ? tileNeighbors[i].height : 0;
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


    public CliffTileSet.Shape ShapeByNeighborsHeight(int n1, int n2, int n3)
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

    private void OnValidate()
    {
        Debug.Assert(blockSize.x > 0);
        Debug.Assert(blockSize.y > 0);
        Debug.Assert(blockSize.z > 0);
    }
}
