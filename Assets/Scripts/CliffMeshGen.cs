using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CliffMeshGen
{
    public CliffMeshGen(CliffTileSet tileSet, int population)
    {
        this._tileSet = tileSet;
        this._combine = new CombineInstance[population];
    }

    public void Add(Vector3Int pos, CliffTileShape shape)
    {
        var posf = new Vector3(
            pos.x * _tileSet.blockSize.x,
            pos.z * _tileSet.blockSize.y,
            pos.y * _tileSet.blockSize.z);

        var transform = Matrix4x4.Translate(posf) * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, -180));

        _combine[_index].mesh = _tileSet.GetMesh(shape);
        _combine[_index].transform = transform;

        Debug.Assert(_combine[_index].mesh != null);

        _index += 1;
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
