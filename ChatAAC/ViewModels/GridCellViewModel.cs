using System;
using System.Reactive;
using System.Threading.Tasks;
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

        // Zmieniono typ na ReactiveCommand<Unit, Task>
        public ReactiveCommand<Unit, Task> EditCellCommand { get; }

        public GridCellViewModel(int row, int column, Button? button, MainViewModel parent)
        {
            Row = row;
            Column = column;
            Button = button;
            var parent1 = parent;

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
                    parent1.ObfData?.Buttons.Add(newButton);
                    // Zaktualizuj siatkę – przypisz do komórki nowy identyfikator
                    parent1.UpdateGridOrderForCell(Row, Column, newButton.Id);
                }
                await parent1.EditButtonAsync(Button);
            });
        }
    }
}