using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMapGenerator
{
    public TileMapGenerator(TileSet tileSet)
    {
        _tileSet = tileSet;
    }

    public void Add(Vector3Int pos, TileSet.TileKind k1, TileSet.TileKind k2, TileSet.TileKind k3, TileSet.TileKind k4)
    {
        _blocks.Add(new Block
        {
            pos = pos,
            kind1 = k1,
            kind2 = k2,
            kind3 = k3,
            kind4 = k4,
        });
    }

    public void Generate(Mesh mesh)
    {
        var builder = new MeshBuilder();

        foreach (var block in _blocks)
        {
            var pos = new Vector3(block.pos.x * _step.x, block.pos.z * _step.z, block.pos.y * _step.y);

            var transform = Matrix4x4.TRS(pos, Quaternion.Euler(-90, 0, -180), Vector3.one);

            builder.Place(_tileSet.GetMesh(block.kind1, 0), transform);
            builder.Place(_tileSet.GetMesh(block.kind2, 1), transform);
            builder.Place(_tileSet.GetMesh(block.kind3, 2), transform);
            builder.Place(_tileSet.GetMesh(block.kind4, 3), transform);
        }

        builder.FillMesh(mesh);
    }

    struct Block
    {
        public Vector3Int pos;
        public TileSet.TileKind kind1;
        public TileSet.TileKind kind2;
        public TileSet.TileKind kind3;
        public TileSet.TileKind kind4;
    }

    class MeshBuilder
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> indices = new List<int>();

        public void Place(Mesh mesh, Matrix4x4 transform)
        {
            Debug.Assert(mesh.uv.Length == mesh.vertices.Length);
            int startIndex = vertices.Count;

            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                vertices.Add(transform.MultiplyPoint(mesh.vertices[i]));
                uv.Add(mesh.uv[i]);
            }

            foreach (int index in mesh.triangles)
            {
                indices.Add(index + startIndex);
            }
        }

        public void FillMesh(Mesh mesh)
        {
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = indices.ToArray();
        }
    }

    private Vector3 _step = new Vector3(1, 1, 0.5f);
    private List<Block> _blocks = new List<Block>();

    private TileSet _tileSet;
}
