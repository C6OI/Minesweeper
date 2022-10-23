using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Minesweeper;

public partial class MainWindow : Window {
    public readonly List<StackPanel> CellColumns = new();
    public readonly Stopwatch Stopwatch = new();
    public int FieldHeight;
    public int FieldWidth;
    public int MinesCount;
    public int FlaggedCells;
    public int Questions;
    public bool GameStarted;
    public bool GameOver;
    public List<CellButton> AllCells = new();
    public List<CellButton> MineCells = new();
    public List<CellButton> ClearCells = new();
    public GameMode GameMode = GameMode.Beginner;

    public MainWindow() {
        StartGame();

        InitializeComponent();

        NewGame.Click += (_, _) => StartGame();
        
        Beginner.Click += (_, _) => {
            GameMode = GameMode.Beginner;
            StartGame();
        };
        
        Amateur.Click += (_, _) => {
            GameMode = GameMode.Amateur;
            StartGame();
        };
        
        Professional.Click += (_, _) => {
            GameMode = GameMode.Professional;
            StartGame();
        };

        Custom.Click += async (_, _) => {
            Field customField = await new CustomGame().ShowDialog<Field>(this);

            if (customField.Equals(default(Field))) {
                StartGame();
                return;
            }

            GameMode = GameMode.Custom;

            FieldHeight = customField.Height;
            FieldWidth = customField.Width;
            MinesCount = customField.Mines;
            
            StartGame();
        }; 
            
        Tags.Click += (_, _) => TagsEnabled.IsChecked = !TagsEnabled.IsChecked;
        Sound.Click += (_, _) => SoundEnabled.IsChecked = !SoundEnabled.IsChecked;

        AutoFlags.Click += (_, _) => AutoFlagsEnabled.IsChecked = !AutoFlagsEnabled.IsChecked;
        AutoOpen.Click += (_, _) => AutoOpenEnabled.IsChecked = !AutoOpenEnabled.IsChecked;

        Exit.Click += (_, _) => Environment.Exit(0);

        Mines.Text = $"{MinesCount}";
        MinesField.Children.AddRange(CellColumns);
    }

    void StartGame() {
        switch (GameMode) {
            case GameMode.Beginner:
                FieldHeight = 9;
                FieldWidth = 9;
                MinesCount = 10;
                break;

            case GameMode.Amateur:
                FieldHeight = 16;
                FieldWidth = 16;
                MinesCount = 40;
                break;

            case GameMode.Professional:
                FieldHeight = 16;
                FieldWidth = 30;
                MinesCount = 99;
                break;
        }
        
        StopTime();
        if (Time?.Text != null) Time.Text = "00:00";
        AllCells = new List<CellButton>(FieldWidth * FieldHeight);
        ClearCells = new List<CellButton>(FieldWidth * FieldHeight - MinesCount);
        MineCells = new List<CellButton>(MinesCount);
        MinesField?.Children?.Clear();
        CellColumns.Clear();
        FlaggedCells = 0;
        Questions = 0;
        GameStarted = false;
        GameOver = false;

        for (short x = 0; x < FieldWidth; x++) {
            StackPanel column = new() { Orientation = Orientation.Vertical };

            for (short y = 0; y < FieldHeight; y++) {
                CellButton cell = new(this) { X = x, Y = y };

                AllCells.Add(cell);
                ClearCells.Add(cell);
                column.Children.Add(cell);
            }
            
            CellColumns.Add(column);
        }
        
        MinesField?.Children?.AddRange(CellColumns);
        
        PlaceMines();
    }

    public void PlaceMines(short? count = null, short? ignoredX = null, short? ignoredY = null) {
        short placedMines = 0;
        short mines = count ?? 0;

        if (count == null) mines = (short)MinesCount;

        while (placedMines < mines) {
            Random xRand = new();
            Random yRand = new();
            
            int x = xRand.Next(0, FieldWidth);
            int y = yRand.Next(0, FieldHeight);
            
            if (ignoredX != null) while (x == ignoredX) x = xRand.Next(0, FieldWidth);
            if (ignoredY != null) while (y == ignoredY) y = yRand.Next(0, FieldHeight);

            CellButton? cell = AllCells.FirstOrDefault(c => c.X == x && c.Y == y);

            if (cell?.IsMine ?? false) continue;

            cell!.Type = CellType.Mine;
            ClearCells.Remove(cell);
            MineCells.Add(cell);
            placedMines++;
        }
        
        if (Mines?.Text != null) Mines.Text = $"{MinesCount}";
    }

    public void FinishGame(bool lose) {
        GameOver = true;
        StopTime();
        
        if (lose) MineCells.ForEach(c => c.Open(true));
    }

    public void RestartTime() {
        Stopwatch.Restart();

        DispatcherTimer.Run(() => {
            if (!Stopwatch.IsRunning) return false;
            
            Time.Text = Stopwatch.Elapsed.ToString(@"mm\:ss");
            return true;
        }, TimeSpan.Zero);
    }

    public void StopTime() => Stopwatch.Stop();
}