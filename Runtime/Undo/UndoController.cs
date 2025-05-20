using System.Collections.Generic;
using UI.Focusing;
using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Класс для работы с операцией отмены предыдущего действия
    /// </summary>
    public class UndoController : MonoBehaviour
    {
        [SerializeField] private SimpleContextLayer _context;

        private List<UndoAction> _undoList = new();
        private List<UndoAction> _redoList = new();

        private IUndoable _lockedUndoable;
    
        /// <summary>
        /// Блокирована ли операция отмены
        /// </summary>
        public bool IsLocked => _lockedUndoable != null;

        private void OnEnable()
        {
            _context.PushLayer();
        }

        private void OnDisable()
        {
            _context.RemoveLayer();
        }

        /// <summary>
        /// Создает предыдущее состояние для объекта, реализующего <see cref="IUndoable"/>
        /// </summary>
        /// <param name="undoable">Объект, для которого необходимо создать предыдущее состояние</param>
        /// <param name="clearRedu">Нужно ли очищать повторы предыдущих действий</param>
        public void CreateUndoState(IUndoable undoable, bool clearRedu = true)
        {
            _undoList.Add(new(undoable, undoable.GetUndoContext()));

            if (clearRedu)
            {
                _redoList.Clear();
            }
        }

        /// <summary>
        /// Выполняет операцию отмены предыдущего действия
        /// </summary>
        public void Undo()
        {
            if (_undoList.Count == 0)
            {
                return;
            }

            UndoAction undoAction = _undoList[_undoList.Count - 1];

            if (_lockedUndoable != null && undoAction.Undoable != _lockedUndoable)
            {
                return;
            }

            UndoAction redoAction = new (undoAction.Undoable, undoAction.Undoable.GetCurrentContext());
            _redoList.Add(redoAction);

            undoAction.Undoable.Undo(undoAction.Context);

            _undoList.RemoveAt(_undoList.Count - 1);
        }

        /// <summary>
        /// Выполняет операцию повтора предыдущего действия
        /// </summary>
        public void Redo()
        {
            if (_redoList.Count == 0)
            {
                return;
            }

            UndoAction undoAction = _redoList[_redoList.Count - 1];

            if (_lockedUndoable != null && undoAction.Undoable != _lockedUndoable)
            {
                return;
            }

            CreateUndoState(undoAction.Undoable, false);

            undoAction.Undoable.Redo(undoAction.Context);

            _redoList.RemoveAt(_redoList.Count - 1);
        }

        /// <summary>
        /// Удаляет все повторы операции предыдущего действия
        /// </summary>
        /// <param name="undoable">Опционально можно указать, для какого конкретного <see cref="IUndoable"/> будут удалены повторы</param>
        public void DeleteAllUndo(IUndoable undoable = null)
        {
            if (undoable == null)
            {
                _undoList.Clear();
                _redoList.Clear();

                return;
            }

            List<UndoAction> newList = new();
            foreach (UndoAction undoAction in _undoList)
            {
                if (undoAction.Undoable != undoable)
                {
                    newList.Add(undoAction);
                }
            }

            _undoList = newList;

            newList = new();
            foreach (UndoAction undoAction in _redoList)
            {
                if (undoAction.Undoable != undoable)
                {
                    newList.Add(undoAction);
                }
            }

            _redoList = newList;
        }

        /// <summary>
        /// Блокирует операцию отмены предыдущего действия для <see cref="IUndoable"/>
        /// </summary>
        /// <param name="undoable">Объект, для которого будет заблокирована операция отмены предыдущего действия</param>
        public void LockUndoable(IUndoable undoable)
        {
            _lockedUndoable = undoable;
        }

        /// <summary>
        /// Класс, представляющий отмену предыдущего действия
        /// </summary>
        class UndoAction
        {
            /// <summary>
            /// Источник контекста предыдущего состояния
            /// </summary>
            public IUndoable Undoable { get; }
            /// <summary>
            /// Контекст предыдущего состояния
            /// </summary>
            public string Context { get; }

            /// <summary>
            /// Конструктор <see cref="UndoAction"/>
            /// </summary>
            /// <param name="undoable">Источник контекста предыдущего состояния</param>
            /// <param name="context">Контекст предыдущего состояния</param>
            public UndoAction(IUndoable undoable, string context)
            {
                Undoable = undoable;
                Context = context;
            }
        }
    }
}
