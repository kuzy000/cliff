using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cliff
{
    public class MeshGen
    {
        public MeshGen(TileSet tileSet)
        {
            this._tileSet = tileSet;
        }

        public void Add(Vector3Int pos, TileShape shape)
        {
            var posf = new Vector3(
                pos.x * _tileSet.blockSize.x,
                pos.z * _tileSet.blockSize.y,
                pos.y * _tileSet.blockSize.z);

            var transform = Matrix4x4.Translate(posf) * _tileSet.transform * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, -180));

            var meshes = _tileSet.GetMesh(shape);
            foreach (var (mesh, isGround) in meshes)
            {
                if (isGround)
                {
                    _combineGround.Add(new CombineInstance
                    {
                        mesh = mesh,
                        transform = transform,
                    });
                    _indicesSizeGround += (int)mesh.GetIndexCount(0);
                }
                else
                {
                    _combineCliff.Add(new CombineInstance
                    {
                        mesh = mesh,
                        transform = transform,
                    });
                    _indicesSizeCliff += (int)mesh.GetIndexCount(0);
                }
            }
        }

        public void Generate(Mesh mesh)
        {
            mesh.CombineMeshes(_combineCliff.Concat(_combineGround).ToArray(), true, true, false);


            mesh.SetSubMeshes(new List<SubMeshDescriptor> {
            new SubMeshDescriptor(0, _indicesSizeCliff),
            new SubMeshDescriptor(_indicesSizeCliff, _indicesSizeGround),
        });
        }

        private int _indicesSizeCliff = 0;
        private List<CombineInstance> _combineCliff = new List<CombineInstance>();

        private int _indicesSizeGround = 0;
        private List<CombineInstance> _combineGround = new List<CombineInstance>();

        private TileSet _tileSet;
    }
}