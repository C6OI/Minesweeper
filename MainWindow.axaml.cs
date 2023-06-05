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
    public readonly DiscordRPCManager DiscordManager;
    public DateTimeOffset StartTime;
    public int FieldHeight;
    public int FieldWidth;
    public int MinesCount;
    public int FlaggedCount;
    public int QuestionsCount;
    public bool GameStarted;
    public bool GameOver;
    public List<CellButton> AllCells = new();
    public List<CellButton> MineCells = new();
    public List<CellButton> ClearCells = new();
    public GameMode GameMode = GameMode.Beginner;

    public int MinesRemaining => MinesCount - FlaggedCount;

    public MainWindow() {
        DiscordManager = new DiscordRPCManager(this);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DiscordManager.Dispose();

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
        DiscordManager.SetActivity("Starting the game...", $"{GameMode}", smallImage: Assets.waiting);
        
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (GameMode) {
            case GameMode.Beginner:
                FieldWidth = 9;
                FieldHeight = 9;
                MinesCount = 10;
                break;

            case GameMode.Amateur:
                FieldWidth = 16;
                FieldHeight = 16;
                MinesCount = 40;
                break;

            case GameMode.Professional:
                FieldWidth = 30;
                FieldHeight = 16;
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
        FlaggedCount = 0;
        QuestionsCount = 0;
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
        
        DiscordManager.SetActivity("Getting ready", $"{GameMode}", smallImage: Assets.waiting);
    }

    public void PlaceMines(short? count = null, short? ignoredX = null, short? ignoredY = null) {
        DiscordManager.SetActivity("Placing mines...", $"{GameMode}", smallImage: Assets.waiting);
    
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
        DateTimeOffset stopTime = DateTimeOffset.Now;
        GameOver = true;
        StopTime();

        if (lose) {
            MineCells.ForEach(c => c.Open(lose));
            DiscordManager.SetActivity("Lost the game", $"{GameMode}", StartTime, stopTime, Assets.lose_game);
        } else {
            DiscordManager.SetActivity("Win the game", $"{GameMode}", StartTime, stopTime, Assets.win_game);
        }
    }

    public void RestartTime() {
        Stopwatch.Restart();
        StartTime = DateTimeOffset.Now;

        DispatcherTimer.Run(() => {
            if (!Stopwatch.IsRunning) return false;
            
            Time.Text = Stopwatch.Elapsed.ToString(@"mm\:ss");
            return true;
        }, TimeSpan.Zero);
    }

    public void StopTime() => Stopwatch.Stop();
}