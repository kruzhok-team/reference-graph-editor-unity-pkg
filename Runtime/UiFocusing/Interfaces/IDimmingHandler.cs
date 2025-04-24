using System.Collections.Generic;
using UnityEngine;

namespace UI.Focusing
{
    public interface IDimmingHandler
    {
        void EnableDimming(IEnumerable<GameObject> FocusedElements);
        void DisableDimming();
    }
}
