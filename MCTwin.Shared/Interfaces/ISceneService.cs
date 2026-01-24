using System.Collections.Generic;

namespace MCTwinStudio.Services
{
    public interface ISceneService
    {
        void SaveScene(string name, string json);
        string LoadScene();
        string[] ListScenes();
    }
}
