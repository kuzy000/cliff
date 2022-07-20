using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CliffMeshGen
{
    public CliffMeshGen(CliffTileSet tileSet, int population)
    {
        this._tileSet = tileSet;
        this._combine = new CombineInstance[population * 4];
    }

    public void Add(Vector3Int pos, CliffTileSet.Shape s1, CliffTileSet.Shape s2, CliffTileSet.Shape s3, CliffTileSet.Shape s4)
    {
        var posf = new Vector3(
            pos.x * _tileSet.blockSize.x,
            pos.z * _tileSet.blockSize.y,
            pos.y * _tileSet.blockSize.z);

        var transform = Matrix4x4.Translate(posf) * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, -180));

        _combine[_index + 0].mesh = _tileSet.GetMesh(s1, 0);
        _combine[_index + 1].mesh = _tileSet.GetMesh(s2, 1);
        _combine[_index + 2].mesh = _tileSet.GetMesh(s3, 2);
        _combine[_index + 3].mesh = _tileSet.GetMesh(s4, 3);

        _combine[_index + 0].transform = transform;
        _combine[_index + 1].transform = transform;
        _combine[_index + 2].transform = transform;
        _combine[_index + 3].transform = transform;

        _index += 4;
    }

    public void Generate(Mesh mesh)
    {
        Debug.Assert(_index == _combine.Length);
        mesh.CombineMeshes(_combine, true, true, false);
    }

    private int _index = 0;
    private CombineInstance[] _combine;
    private CliffTileSet _tileSet;
}
