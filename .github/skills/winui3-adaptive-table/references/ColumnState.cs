using System.Collections.Generic;
using System.Linq;

namespace WinUI3.AdaptiveTable
{
    /// <summary>
    /// Column show/hide state for the shared adaptive table. Pure BCL — no WinUI dependency.
    /// Wire it to checkable menu items in a column-picker flyout, and bind each column's
    /// Visibility to <see cref="IsVisible"/>.
    /// </summary>
    public sealed class ColumnState
    {
        private readonly HashSet<string> _hidden = new();

        public ColumnState(IEnumerable<string> allColumns)
        {
            AllColumns = allColumns.ToList();
        }

        /// <summary>All columns in declaration order.</summary>
        public IReadOnlyList<string> AllColumns { get; }

        public bool IsVisible(string column) => !_hidden.Contains(column);

        public void Show(string column) => _hidden.Remove(column);

        public void Hide(string column)
        {
            if (AllColumns.Contains(column))
            {
                _hidden.Add(column);
            }
        }

        public void Toggle(string column)
        {
            if (_hidden.Contains(column))
            {
                _hidden.Remove(column);
            }
            else
            {
                _hidden.Add(column);
            }
        }

        public IEnumerable<string> VisibleColumns => AllColumns.Where(IsVisible);
    }
}
