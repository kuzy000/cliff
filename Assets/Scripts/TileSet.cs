using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileSet", menuName = "TileMap/TileSet", order = 1)]
public class TileSet : ScriptableObject
{
    public enum TileKind
    {
        Plane, TwoSide, SideV, SideH, Hole
    }

    [SerializeField] private Mesh plane1;
    [SerializeField] private Mesh plane2;
    [SerializeField] private Mesh plane3;
    [SerializeField] private Mesh plane4;

    [SerializeField] private Mesh twoSide1;
    [SerializeField] private Mesh twoSide2;
    [SerializeField] private Mesh twoSide3;
    [SerializeField] private Mesh twoSide4;

    [SerializeField] private Mesh sideH1;
    [SerializeField] private Mesh sideH2;
    [SerializeField] private Mesh sideH3;
    [SerializeField] private Mesh sideH4;

    [SerializeField] private Mesh sideV1;
    [SerializeField] private Mesh sideV2;
    [SerializeField] private Mesh sideV3;
    [SerializeField] private Mesh sideV4;

    [SerializeField] private Mesh hole1;
    [SerializeField] private Mesh hole2;
    [SerializeField] private Mesh hole3;
    [SerializeField] private Mesh hole4;

    public Mesh[] plane => new Mesh[] { plane1, plane2, plane3, plane4 };
    public Mesh[] twoSide => new Mesh[] { twoSide1, twoSide2, twoSide3, twoSide4 };
    public Mesh[] sideH => new Mesh[] { sideH1, sideH2, sideH3, sideH4 };
    public Mesh[] sideV => new Mesh[] { sideV1, sideV2, sideV3, sideV4 };
    public Mesh[] hole => new Mesh[] { hole1, hole2, hole3, hole4 };

    public Mesh GetMesh(TileKind kind, int index)
    {
        switch (kind)
        {
            case TileKind.Plane: return plane[index];
            case TileKind.TwoSide: return twoSide[index];
            case TileKind.SideH: return sideH[index];
            case TileKind.SideV: return sideV[index];
            case TileKind.Hole: return hole[index];
        }

        return null;
    }
}
