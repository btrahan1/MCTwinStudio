using System;
using System.Collections.Generic;
using System.Numerics;
using MCTwin.Shared.Geometry;

namespace MCTwin.Shared.Meshing
{
    public static class MarchingCubes
    {
        // Re-implemented as Marching Tetrahedra to avoid massive lookup tables.
        // This produces slightly more triangles but is fully robust with minimal code.

        public static MeshResult Generate(Func<Vector3, float> sdf, Vector3 min, Vector3 max, int resolutionX, int resolutionY, int resolutionZ)
        {
            var result = new MeshResult();
            var step = (max - min) / new Vector3(resolutionX, resolutionY, resolutionZ);
            
            // Grid values
            float[,,] values = new float[resolutionX + 1, resolutionY + 1, resolutionZ + 1];

            for (int z = 0; z <= resolutionZ; z++)
            {
                for (int y = 0; y <= resolutionY; y++)
                {
                    for (int x = 0; x <= resolutionX; x++)
                    {
                        values[x, y, z] = sdf(min + new Vector3(x, y, z) * step);
                    }
                }
            }

            for (int z = 0; z < resolutionZ; z++)
            {
                for (int y = 0; y < resolutionY; y++)
                {
                    for (int x = 0; x < resolutionX; x++)
                    {
                        ProcessCell(result, x, y, z, values, min, step);
                    }
                }
            }

            return result; 
        }

        private static void ProcessCell(MeshResult mesh, int x, int y, int z, float[,,] values, Vector3 min, Vector3 step)
        {
            // Corners
            Vector3 p0 = min + new Vector3(x, y, z) * step;
            // Offsets
            Vector3 dx = new Vector3(step.X, 0, 0);
            Vector3 dy = new Vector3(0, step.Y, 0);
            Vector3 dz = new Vector3(0, 0, step.Z);

            // Points
            Vector3[] p = new Vector3[8];
            p[0] = p0;
            p[1] = p0 + dx;
            p[2] = p0 + dx + dz; // Note standard indexing variation, careful
            p[3] = p0 + dz;
            p[4] = p0 + dy;
            p[5] = p0 + dx + dy;
            p[6] = p0 + dx + dy + dz;
            p[7] = p0 + dy + dz;

            // Values
            float[] v = new float[8];
            v[0] = values[x, y, z];
            v[1] = values[x + 1, y, z]; // p1
            v[2] = values[x + 1, y, z + 1]; // p2 (match pos calc above)
            v[3] = values[x, y, z + 1]; // p3
            v[4] = values[x, y + 1, z]; // p4
            v[5] = values[x + 1, y + 1, z]; // p5
            v[6] = values[x + 1, y + 1, z + 1]; // p6
            v[7] = values[x, y + 1, z + 1]; // p7

            // Fix position mapping if it was wrong above?
            // p0=(x,y,z), p1=(x+1,y,z), p2=(x+1,y,z+1)? 
            // Usually p2 is (x+1,y,z+1) and p3 is (x,y,z+1).
            // Let's verify standard cube ordering:
            // 0:000, 1:100, 2:101, 3:001 ???
            // Let's stick to the v[] indices matching the p[] indices explicitly.
            
            p[2] = p0 + new Vector3(step.X, 0, step.Z);
            p[3] = p0 + new Vector3(0, 0, step.Z);
            p[6] = p0 + new Vector3(step.X, step.Y, step.Z);
            p[7] = p0 + new Vector3(0, step.Y, step.Z);

            // Decompose into 6 Tetrahedra
            // T1: 0, 5, 1, 6? No.
            // Standard decomposition:
            // T1: 0, 2, 3, 7
            // T2: 0, 6, 2, 7
            // T3: 0, 4, 6, 7
            // T4: 0, 6, 1, 2
            // T5: 0, 1, 6, 5
            // T6: 5, 6, 4, 0 (Wait, 0,4,5,6)
            
            // Correct robust decomposition (share diagonal 0-6):
            ProcessTetra(mesh, p[0], v[0], p[1], v[1], p[2], v[2], p[6], v[6]);
            ProcessTetra(mesh, p[0], v[0], p[2], v[2], p[3], v[3], p[6], v[6]);
            ProcessTetra(mesh, p[0], v[0], p[3], v[3], p[7], v[7], p[6], v[6]);
            ProcessTetra(mesh, p[0], v[0], p[7], v[7], p[4], v[4], p[6], v[6]);
            ProcessTetra(mesh, p[0], v[0], p[4], v[4], p[5], v[5], p[6], v[6]);
            ProcessTetra(mesh, p[0], v[0], p[5], v[5], p[1], v[1], p[6], v[6]);
        }

        private static void ProcessTetra(MeshResult mesh, Vector3 p0, float v0, Vector3 p1, float v1, Vector3 p2, float v2, Vector3 p3, float v3)
        {
            int index = 0;
            if (v0 < 0) index |= 1;
            if (v1 < 0) index |= 2;
            if (v2 < 0) index |= 4;
            if (v3 < 0) index |= 8;

            switch (index)
            {
                case 0:
                case 15:
                    return; // All inside or all outside
                
                // 1 vertex inside
                case 1:  AddTri(mesh, v0, p0, v1, p1, v2, p2, v3, p3); break; // 0 inside
                case 2:  AddTri(mesh, v1, p1, v0, p0, v3, p3, v2, p2); break; // 1 inside
                case 4:  AddTri(mesh, v2, p2, v0, p0, v1, p1, v3, p3); break; // 2 inside
                case 8:  AddTri(mesh, v3, p3, v0, p0, v2, p2, v1, p1); break; // 3 inside
                
                // 1 vertex OUTSIDE (inverse of 1 inside)
                case 14: AddTri(mesh, v0, p0, v2, p2, v1, p1, v3, p3); break; // 0 outside
                case 13: AddTri(mesh, v1, p1, v3, p3, v0, p0, v2, p2); break; // 1 outside
                case 11: AddTri(mesh, v2, p2, v1, p1, v0, p0, v3, p3); break; // 2 outside
                case 7:  AddTri(mesh, v3, p3, v2, p2, v0, p0, v1, p1); break; // 3 outside

                // 2 vertices inside (Quad -> 2 Tris)
                // Case 3 (0,1 inside)
                case 3:
                     AddQuad(mesh, v0, p0, v1, p1, v2, p2, v3, p3);
                     break;
                case 5: // 0,2 inside
                     AddQuad(mesh, v0, p0, v2, p2, v1, p1, v3, p3); 
                     break;
                case 6: // 1,2 inside
                     AddQuad(mesh, v1, p1, v2, p2, v0, p0, v3, p3);
                     break;
                case 9: // 0,3 inside
                     AddQuad(mesh, v0, p0, v3, p3, v1, p1, v2, p2);
                     break;
                case 10: // 1,3 inside
                     AddQuad(mesh, v1, p1, v3, p3, v0, p0, v2, p2);
                     break;
                case 12: // 2,3 inside
                     AddQuad(mesh, v2, p2, v3, p3, v0, p0, v1, p1);
                     break;
            }
        }

        // Adds a triangle where p0 is the only point of sign A, others B
        private static void AddTri(MeshResult mesh, float v0, Vector3 p0, float v1, Vector3 p1, float v2, Vector3 p2, float v3, Vector3 p3)
        {
            // Vertices are intersection on edges from p0 to p1, p0 to p2, p0 to p3
            Vector3 i1 = Interp(p0, v0, p1, v1);
            Vector3 i2 = Interp(p0, v0, p2, v2);
            Vector3 i3 = Interp(p0, v0, p3, v3);

            // Winding order? Assumes (i1, i2, i3)
            AddVertex(mesh, i1); AddVertex(mesh, i2); AddVertex(mesh, i3);
        }

        // Adds 2 triangles separating (p0,p1) group from (p2,p3) group
        private static void AddQuad(MeshResult mesh, float v0, Vector3 p0, float v1, Vector3 p1, float v2, Vector3 p2, Vector3 v3, Vector3 p3)
        {
            // Intersections
            Vector3 i02 = Interp(p0, v0, p2, v2);
            Vector3 i03 = Interp(p0, v0, p3, v3);
            Vector3 i12 = Interp(p1, v1, p2, v2);
            Vector3 i13 = Interp(p1, v1, p3, v3);

            AddVertex(mesh, i02); AddVertex(mesh, i03); AddVertex(mesh, i13);
            AddVertex(mesh, i02); AddVertex(mesh, i13); AddVertex(mesh, i12);
        }
        
        private static Vector3 Interp(Vector3 p1, float v1, Vector3 p2, float v2)
        {
            float t = (0 - v1) / (v2 - v1);
            return p1 + t * (p2 - p1);
        }

        private static void AddVertex(MeshResult mesh, Vector3 v)
        {
            mesh.Vertices.Add(v.X);
            mesh.Vertices.Add(v.Y);
            mesh.Vertices.Add(v.Z);
            mesh.Indices.Add(mesh.Indices.Count);
        }
    }
}
