using Talent.Graphs;
using UnityEngine;

namespace Talent.GraphEditor.Unity.Runtime.Demo
{
    /// <summary>
    /// Класс для инициализации тестового графа
    /// </summary>
    public class InitTestGraph : MonoBehaviour
    {
        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;

        private void Start()
        {
            CyberiadaGraphDocument graphDocument = new CyberiadaGraphDocument();
            graphDocument.RootGraph = new CyberiadaGraph("gMain", new GraphData());
            graphDocument.Name = "Test";
            var context = new TestExecutionContextSource();

            _runtimeGraphEditor.OpenGraphDocument(graphDocument, context);
        }
    }
}
