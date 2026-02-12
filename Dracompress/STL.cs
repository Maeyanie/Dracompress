using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dracompress
{
    class STL
    {
        public struct Triangle(float[] a, float[] b, float[] c)
        {
            public float[] A = a;
            public float[] B = b;
            public float[] C = c;
        }
        public List<Triangle> triangles = [];

        public void Read(string filename)
        {
            using FileStream fs = File.OpenRead(filename);
            using BinaryReader br = new(fs);
            byte[] header = br.ReadBytes(80);
            uint triangleCount = br.ReadUInt32();

            for (uint i = 0; i < triangleCount; i++)
            {
                br.ReadSingle();
                br.ReadSingle();
                br.ReadSingle();
                float AX = br.ReadSingle();
                float AY = br.ReadSingle();
                float AZ = br.ReadSingle();
                float BX = br.ReadSingle();
                float BY = br.ReadSingle();
                float BZ = br.ReadSingle();
                float CX = br.ReadSingle();
                float CY = br.ReadSingle();
                float CZ = br.ReadSingle();
                br.ReadUInt16();
                triangles.Add(new Triangle([AX, AY, AZ], [BX, BY, BZ], [CX, CY, CZ]));
            }
        }

        public void Write(string filename)
        {
            using FileStream fs = File.Create(filename);
            using BinaryWriter bw = new(fs);
            byte[] header = new byte[80];
            bw.Write(header);
            bw.Write((uint)triangles.Count);
            foreach (Triangle tri in triangles)
            {
                bw.Write(0.0f);
                bw.Write(0.0f);
                bw.Write(0.0f);
                bw.Write(tri.A[0]);
                bw.Write(tri.A[1]);
                bw.Write(tri.A[2]);
                bw.Write(tri.B[0]);
                bw.Write(tri.B[1]);
                bw.Write(tri.B[2]);
                bw.Write(tri.C[0]);
                bw.Write(tri.C[1]);
                bw.Write(tri.C[2]);
                bw.Write((ushort)0);
            }
        }
    }
}
