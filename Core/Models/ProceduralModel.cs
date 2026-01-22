using System;
using System.Collections.Generic;
using MCTwinStudio.Core.Models;

namespace MCTwinStudio.Core.Models
{
    public class ProceduralModel : BaseModel
    {
        public string RawRecipeJson { get; set; } = "";
        public List<dynamic> Parts { get; set; } = new List<dynamic>();

        public ProceduralModel()
        {
            ModelType = "Procedural";
        }

        public override List<ModelPart> GetParts()
        {
            // Procedural models don't use the Voxel Part system
            return new List<ModelPart>();
        }
    }
}
