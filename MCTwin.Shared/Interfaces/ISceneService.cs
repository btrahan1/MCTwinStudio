using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCTwinStudio.Services
{
    public interface ISceneService
    {
        Task SaveScene(string name, string json);
        Task<string> LoadScene();
        Task<string[]> ListScenes();
    }
}
