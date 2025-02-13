using System.IO;
using Talent.Graphs;
using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс, представляющий загрузчик документов CyberiadaGraphML
    /// </summary>
    public class GraphImporter : MonoBehaviour
    {
        [SerializeField] private RuntimeGraphEditor _runtimeGraphEditor;
        private CyberiadaGraphMLConverter _converter;

        private void Awake()
        {
            _converter = new CyberiadaGraphMLConverter(Application.productName, Application.version);
        }

        /// <summary>
        /// Импортирует документ
        /// </summary>
        public void Import()
        {
            CyberiadaGraphDocument graphDocument = _converter.DeserializeFromFile(Path.Combine(Application.dataPath, "../", "test.xml"));
            _runtimeGraphEditor.SetGraphDocument(graphDocument);
            _runtimeGraphEditor.UndoController.DeleteAllUndo();
        }

        /// <summary>
        /// Сохраняет документ в файл
        /// </summary>
        public void Export()
        {
            _converter.SerializeToFile(_runtimeGraphEditor.GraphDocument, Path.Combine(Application.dataPath, "../", "test.xml"));
        }
    }
}
