using ReactiveUI;
using System;
using System.Reactive;
using ChatAAC.Models.Obf;
using Avalonia;

namespace ChatAAC.ViewModels
{
    public class EditGridViewModel : ReactiveObject
    {
        private readonly Grid _gridData;
        private int _rows;
        private int _columns;
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
            set => this.RaiseAndSetIfChanged(ref _rows, value);
        }

        public int Columns
        {
            get => _columns;
            set => this.RaiseAndSetIfChanged(ref _columns, value);
        }

        private void Confirm()
        {
            if (_rows < _gridData.Rows || _columns < _gridData.Columns)
            {
                TrimOrder();
            }
            _gridData.Rows = _rows;
            _gridData.Columns = _columns;
            IsConfirmed = true;
            CloseWindow();
        }

        private void TrimOrder()
        {
            var newOrder = new string?[_rows][];
            for (var r = 0; r < _rows; r++)
            {
                newOrder[r] = new string?[_columns];
                for (var c = 0; c < _columns; c++)
                {
                    if (r < _gridData.Order.Length && c < _gridData.Order[r].Length)
                        newOrder[r][c] = _gridData.Order[r][c];
                    else
                        newOrder[r][c] = null;
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
            if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Windows[^1].Close();
            }
        }
    }
}