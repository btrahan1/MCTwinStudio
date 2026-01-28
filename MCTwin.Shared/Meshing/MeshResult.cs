using System.Collections.Generic;
using System.Numerics;

namespace MCTwin.Shared.Meshing
{
    public class MeshResult
    {
        public List<float> Vertices { get; set; } = new List<float>(); // Flat list x,y,z
        public List<int> Indices { get; set; } = new List<int>();
        public List<float> Normals { get; set; } = new List<float>(); // Flat list nx,ny,nz
        public List<float> Colors { get; set; } = new List<float>(); // Flat list r,g,b,a
    }
}
