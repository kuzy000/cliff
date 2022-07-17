using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TileMap : MonoBehaviour
{
    [SerializeField] private TileSet _tileSet;

    public struct Tile
    {
        public int height;
    }

    public int width => _width;
    public int height => _height;

    public Vector3 block => new Vector3(1, 0.5f, 1);

    public Tile[,] tiles => _tiles;

    public bool IsValid(int x, int y) => x >= 0 && x < _width && y >= 0 && y < _height;

    public void Create(int width, int height)
    {
        _width = width;
        _height = height;
        _tiles = new Tile[_width, _height];

        OnRect(0, 0, _width, _height, (x, y, h) => 0);
    }

    public void Generate()
    {
        var gen = new TileMapGenerator(_tileSet);
        FillGenerator(gen);

        var mesh = new Mesh { name = "Procedural" };
        gen.Generate(mesh);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public void OnRect(int x1, int y1, int x2, int y2, Func<int, int, int, int> f)
    {
        for (int y = y1; y < y2; ++y)
        {
            for (int x = x1; x < x2; ++x)
            {
                _tiles[x, y].height = f(x, y, _tiles[x, y].height);
            }
        }
    }

    private void OnRectNeighbors(int x1, int y1, int x2, int y2, Action<int, int, int[]> f)
    {
        for (int y = y1; y < y2; ++y)
        {
            for (int x = x1; x < x2; ++x)
            {
                var h = _tiles[x, y].height;

                var hs = new int[9];
                For3x3Grid(x, y, (i, x, y) =>
                {
                    hs[i] = IsValid(x, y) ? _tiles[x, y].height : h;
                });

                f(x, y, hs);
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

    private TileSet.TileKind KindByNeighbors(int n1, int n2, int n3)
    {
        var v = new Vector3Int(n1, n2, n3);

        return v switch
        {
            { x: -1, y: -0, z: -0 } => TileSet.TileKind.SideV,
            { x: -0, y: -1, z: -0 } => TileSet.TileKind.Hole,
            { x: -0, y: -0, z: -1 } => TileSet.TileKind.SideH,

            { x: -1, y: -1, z: -0 } => TileSet.TileKind.SideV,
            { x: -1, y: -0, z: -1 } => TileSet.TileKind.TwoSide,
            { x: -0, y: -1, z: -1 } => TileSet.TileKind.SideH,

            { x: -1, y: -1, z: -1 } => TileSet.TileKind.TwoSide,
            _ => TileSet.TileKind.Plane,
        };
    }

    private void FillGenerator(TileMapGenerator gen)
    {
        OnRectNeighbors(0, 0, _width, _height, (x, y, hs) =>
        {
            int h = hs[4];
            for (int i = 0; i < hs.Length; ++i)
            {
                hs[i] -= h;
            }

            var k1 = KindByNeighbors(hs[3], hs[6], hs[7]);
            var k2 = KindByNeighbors(hs[5], hs[8], hs[7]);
            var k3 = KindByNeighbors(hs[3], hs[0], hs[1]);
            var k4 = KindByNeighbors(hs[5], hs[2], hs[1]);

            gen.Add(new Vector3Int(x, y, h - 1), k1, k2, k3, k4);
        });
    }

    private int Index(int x, int y) => y * _width + x;

    private Tile[,] _tiles;
    private int _width = -1;
    private int _height = -1;
}
