using System;
using Avalonia.Controls;

namespace Minesweeper; 

public partial class CustomGame : Window {
    public CustomGame() {
        InitializeComponent();

        Ok.Click += (_, _) => {
            Field field = new() {
                Height = (int)FieldHeight.Value,
                Width = (int)FieldWidth.Value,
                Mines = (int)MinesOnField.Value
            };
            
            Close(field);
        };

        Cancel.Click += (_, _) => Close();
        
        MinesOnField.LostFocus += (_, _) => {
            double maxMines = (int)Math.Round(FieldWidth.Value * FieldHeight.Value * 85 / 100);
            
            if (MinesOnField.Value > maxMines) MinesOnField.Value = maxMines;
        };
    }
}

public struct Field {
    public int Height { get; init; }
    public int Width { get; init; }
    public int Mines { get; init; }
}