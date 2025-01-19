using ReactiveUI;
using System;
using System.Reactive;
using ChatAAC.Models.Obf;

namespace ChatAAC.ViewModels
{
    public class EditGridViewModel : ReactiveObject
    {
        private readonly Grid _gridData;
        private int _rows;
        private int _columns;
        private bool _sizeReduced;

        public bool IsConfirmed { get; private set; }

        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public EditGridViewModel(Grid gridData)
        {
            _gridData = gridData ?? throw new ArgumentNullException(nameof(gridData));
            _rows = gridData.Rows;
            _columns = gridData.Columns;

            ConfirmCommand = ReactiveCommand.Create(Confirm);
            CancelCommand = ReactiveCommand.Create(Cancel);
        }

        public int Rows
        {
            get => _rows;
            set
            {
                // If user picks a value < current .Rows,
                // we might set a flag for removing extra cells
                if (value < _rows) 
                    _sizeReduced = true;
                this.RaiseAndSetIfChanged(ref _rows, value);
            }
        }

        public int Columns
        {
            get => _columns;
            set
            {
                if (value < _columns)
                    _sizeReduced = true;
                this.RaiseAndSetIfChanged(ref _columns, value);
            }
        }

        private void Confirm()
        {
            if (_sizeReduced)
            {
                var confirmed = AskUserForSizeReduction();
                if (!confirmed)
                {
                    IsConfirmed = false;
                    CloseWindow();
                    return;
                }
            }

            // Actually apply the changes
            _gridData.Rows = _rows;
            _gridData.Columns = _columns;

            // Possibly remove references in .Order if they're outside new bounds
            TrimOrderIfNeeded();

            IsConfirmed = true;
            CloseWindow();
        }

        private bool AskUserForSizeReduction()
        {
            // Minimal approach: we don't have a built-in message box in Avalonia out-of-the-box
            // You might build your own small dialog. 
            // For now, let's assume user is always sure:
            return true;
        }

        private void TrimOrderIfNeeded()
        {
            // If user shrank row/column, remove references in .Order that are out of range

            // Suppose it's string?[][]. We'll do a simple approach:
            var newOrder = new string?[_rows][];
            for (var r = 0; r < _rows; r++)
            {
                newOrder[r] = new string?[_columns];
                for (var c = 0; c < _columns; c++)
                {
                    if (r < _gridData.Order.Length && c < _gridData.Order[r].Length)
                    {
                        newOrder[r][c] = _gridData.Order[r][c];
                    }
                    else
                    {
                        // If out of range, discard
                        newOrder[r][c] = null;
                    }
                }
            }
            _gridData.Order = newOrder;
        }

        private void Cancel()
        {
            IsConfirmed = false;
            CloseWindow();
        }

        private void CloseWindow()
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
            if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var topWindow = desktop.Windows[^1];
                topWindow.Close();
            }
        }
    }
}