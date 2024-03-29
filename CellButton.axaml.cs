﻿using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Minesweeper;

public partial class CellButton : UserControl {
    readonly Dictionary<int, IBrush> _numberColors = new() {
        { 1, new SolidColorBrush(0xFF0000FE) },
        { 2, new SolidColorBrush(0xFF186900) },
        { 3, new SolidColorBrush(0xFFAE0107) },
        { 4, new SolidColorBrush(0xFF000177) },
        { 5, new SolidColorBrush(0xFF8D0107) },
        { 6, new SolidColorBrush(0xFF007A7C) },
        { 7, new SolidColorBrush(0xFF902E90) },
        { 8, new SolidColorBrush(0xFF000000) }
    };

    public CellButton() => InitializeComponent();

    public CellButton(MainWindow window) {
        Window = window;

        InitializeComponent();

        Cell.Click += (_, _) => Open();

        Cell.PointerPressed += (_, e) => {
            switch (e.GetCurrentPoint(this).Properties.PointerUpdateKind) {
                case PointerUpdateKind.MiddleButtonPressed:
                    HighlightNeighbours();
                    break;

                case PointerUpdateKind.RightButtonPressed: {
                    if (IsOpened) return;

                    switch (State) {
                        case CellState.Closed:
                            SetFlagged();
                            break;

                        case CellState.Flagged:
                            if (Window.TagsEnabled.IsChecked ?? false)
                                SetQuestioned();
                            else SetClosed();
                            break;

                        case CellState.Question:
                            SetClosed();
                            break;
                    }

                    break;
                }
            }
        };

        // middle and right buttons click
        Cell.PointerReleased += (_, e) => {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.MiddleButtonReleased) return;

            LowlightNeighbours();

            if (!IsPointerOver || !IsOpened) return;

            if ((Window.AutoOpenEnabled.IsChecked ?? false) && 
                Neighbours.Count(c => c.IsFlagged) == Neighbours.Count(c => c.IsMine))
                Neighbours.ForEach(c => c.Open());

            if ((Window.AutoFlagsEnabled.IsChecked ?? false) &&
                Neighbours.Count(c => !c.IsOpened) == Neighbours.Count(c => c.IsMine))
                Neighbours.Where(c => c is { IsOpened: false, IsMine: true }).ToList().ForEach(c => c.SetFlagged());
        };

        Cell.PointerEnter += (_, e) => {
            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed) HighlightNeighbours();
        };

        Cell.PointerLeave += (_, _) => LowlightNeighbours();
    }

    void HighlightNeighbours() =>
        Neighbours.Where(c => !c.IsOpened)
                  .ToList()
                  .ForEach(c => c.Cell.BorderThickness = new Thickness(8));

    void LowlightNeighbours() =>
        Neighbours.Where(c => !c.IsOpened)
                  .ToList()
                  .ForEach(c => c.Cell.BorderThickness = new Thickness(1));

    void SetFlagged() {
        if (Window.GameOver) return;
        
        if (!Window.GameStarted) {
            Window.GameStarted = true;
            Window.RestartTime();
        }

        if (IsFlagged) return;

        if (Window.FlaggedCount >= Window.MinesCount)
            if (Window.TagsEnabled.IsChecked ?? false) {
                SetQuestioned();
                return;
            } else return;

        State = CellState.Flagged;
        Window.FlaggedCount++;
        Window.Mines.Text = $"{Window.MinesRemaining}";
        
        Window.DiscordManager.SetActivity(Window.GameMode, $"{Window.MinesRemaining}/{Window.MinesCount} mines left", Window.StartTime);
        
        CheckForWin();
    }

    void SetQuestioned() {
        if (Window.GameOver) return;
        
        if (!Window.GameStarted) {
            Window.GameStarted = true;
            Window.RestartTime();
        }

        if (IsQuestioned) return;

        State = CellState.Question;
        Window.QuestionsCount++;
        Window.FlaggedCount--;
        Window.Mines.Text = $"{Window.MinesRemaining}";
        
        Window.DiscordManager.SetActivity(Window.GameMode, $"{Window.MinesRemaining}/{Window.MinesCount} mines left", Window.StartTime);
        
        CheckForWin();
    }

    void SetClosed() {
        if (IsClosed || Window.GameOver) return;

        State = CellState.Closed;
        Window.QuestionsCount--;
        Window.Mines.Text = $"{Window.MinesRemaining}";
        
        Window.DiscordManager.SetActivity(Window.GameMode, $"{Window.MinesRemaining}/{Window.MinesCount} mines left", Window.StartTime);
        
        CheckForWin();
    }

    public void Open(bool lose = false) {
        if (IsOpened || IsFlagged || (Window.GameOver && !lose)) return;

        if (!Window.GameStarted) {
            if (IsMine) {
                Type = CellType.Regular;
                Window.MineCells.Remove(this);
                Window.PlaceMines(1, X, Y);
            }

            Window.GameStarted = true;
            Window.RestartTime();
        }

        State = CellState.Opened;

        if (IsMine) {
            CellInfo.Text = "ж";
            TextColor = Brushes.Black;
            BackColor = Brushes.Red;

            if (!Window.GameOver) Window.FinishGame(true);
        } else {
            Cell.BorderThickness = new Thickness(5);
            BackColor = Brushes.DimGray;
            int neighbourMines = Neighbours.Count(c => c.IsMine);

            Neighbours.Where(c => c is { HasMineNeighbours: false, IsMine: false })
                      .ToList()
                      .ForEach(c => c.Open());

            if (neighbourMines != 0) {
                TextColor = GetNumberColor(neighbourMines);
                CellInfo.Text = $"{neighbourMines}";
            } else {
                CellInfo.Text = "";
                Neighbours.ForEach(c => c.Open());
            }
        }

        if (Window.GameOver) return;

        Window.DiscordManager.SetActivity(Window.GameMode, $"{Window.MinesRemaining}/{Window.MinesCount} mines left", Window.StartTime);
        CheckForWin();
    }

    void CheckForWin() {
        if (Window.MinesCount != Window.FlaggedCount || !Window.MineCells.All(c => c.IsFlagged) && !Window.ClearCells.All(c => c.IsOpened)) 
            return;

        Window.ClearCells.ForEach(c => c.Open());
        Window.FinishGame(false);
    }

    public short X { get; init; }

    public short Y { get; init; }

    public bool IsOpened => State == CellState.Opened;

    public bool IsFlagged => State == CellState.Flagged;

    public bool IsQuestioned => State == CellState.Question;

    public bool IsClosed => State == CellState.Closed;
    
    public bool IsMine => Type == CellType.Mine;

    public bool HasMineNeighbours => Neighbours.Any(c => c.IsMine);

    public CellState State {
        get => _currentState;
        private set {
            switch (value) {
                case CellState.Opened:
                    CellInfo.IsVisible = true;
                    break;

                case CellState.Flagged:
                    CellInfo.Text = "🚩";
                    CellInfo.IsVisible = true;
                    break;

                case CellState.Question:
                    CellInfo.Text = "❔";
                    CellInfo.IsVisible = true;
                    break;

                case CellState.Closed:
                    CellInfo.Text = IsMine ? "ж" : "";
                    TextColor = Brushes.Black;
                    CellInfo.IsVisible = false;
                    break;
            }

            _currentState = value;
        }
    }

    public CellType Type { get; set; }

    public IBrush TextColor {
        get => CellInfo.Foreground;
        set => CellInfo.Foreground = value;
    }

    public IBrush? BackColor {
        get => Cell.Background;
        set => Cell.Background = value;
    }

    IBrush GetNumberColor(int num) => _numberColors.GetValueOrDefault(num)!;

    public List<CellButton> Neighbours {
        get {
            if (_neighbourCells != null) return _neighbourCells;

            _neighbourCells = new List<CellButton>();

            for (short x = (short)(X - 1); x <= X + 1; x++) {
                if (x < 0 || x > Window.FieldWidth - 1) continue;

                for (short y = (short)(Y - 1); y <= Y + 1; y++) {
                    if (y < 0 || y > Window.FieldHeight - 1 || (x == X && y == Y)) continue;

                    _neighbourCells.Add(Window.AllCells.First(c => c.X == x && c.Y == y));
                }
            }

            return _neighbourCells;
        }
    }

    MainWindow Window { get; } = null!;

    List<CellButton>? _neighbourCells;
    CellState _currentState = CellState.Closed;
}
