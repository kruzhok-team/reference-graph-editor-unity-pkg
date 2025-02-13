using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент, контролирующий рендеринг, частоту обновления кадров.
    /// </summary>
    public class RenderController : MonoBehaviour
    {
        [SerializeField] private int _lockedFps = 60;

        private bool _inFullFps = false;

        public void EnableFullRendering(float forSeconds)
        {
            StopAllCoroutines();
            StartCoroutine(DisableFor(forSeconds));
        }

        private IEnumerator DisableFor(float second)
        {
            _inFullFps = true;

            SetFpsLock(false);

            yield return new WaitForSeconds(second);

            _inFullFps = false;
        }

        private void Start()
        {
            SetFpsLock(true);
        }

        private void OnDisable()
        {
            SetFpsLock(false);
        }

        private void SetFpsLock(bool isLock)
        {
            OnDemandRendering.renderFrameInterval = isLock ? 3 : 1;
            Application.targetFrameRate = isLock ? _lockedFps : int.MaxValue;
        }

        private void Update()
        {
            if (_inFullFps)
            {
                return;
            }

            SetFpsLock(!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2));
        }
    }
}
