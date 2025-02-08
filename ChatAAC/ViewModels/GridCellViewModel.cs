using System;
using System.Reactive;
using ReactiveUI;
using ChatAAC.Models.Obf;

namespace ChatAAC.ViewModels
{
    public class GridCellViewModel : ReactiveObject
    {
        public int Row { get; }
        public int Column { get; }
        private Button? _button;
        public Button? Button
        {
            get => _button;
            set => this.RaiseAndSetIfChanged(ref _button, value);
        }

        // Komenda edycji komórki – jeżeli w komórce nie ma przycisku, tworzy nowy, w przeciwnym razie edytuje istniejący
        public ReactiveCommand<Unit, Unit> EditCellCommand { get; }

        private readonly MainViewModel _parent;

        public GridCellViewModel(int row, int column, Button? button, MainViewModel parent)
        {
            Row = row;
            Column = column;
            Button = button;
            _parent = parent;

            EditCellCommand = ReactiveCommand.Create(async () =>
            {
                if (Button == null)
                {
                    // Utwórz nowy przycisk z domyślnymi wartościami
                    var newButton = new Button
                    {
                        Id = Guid.NewGuid().ToString(),
                        Label = "New",
                        BorderColor = "#FF000000",
                        BackgroundColor = "#FFFFFFFF",
                        Vocalization = "",
                        Action = ""
                    };
                    Button = newButton;
                    // Dodaj nowy przycisk do modelu
                    _parent.ObfData?.Buttons.Add(newButton);
                    // Zaktualizuj siatkę – przypisz do komórki nowy identyfikator
                    _parent.UpdateGridOrderForCell(Row, Column, newButton.Id);
                }
                // Otwórz okno edycji przycisku
                await _parent.EditButtonAsync(Button);
            });
        }
    }
}