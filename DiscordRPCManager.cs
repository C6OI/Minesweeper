using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using Discord;

namespace Minesweeper; 

public class DiscordRPCManager : IDisposable {
    public const long ClientId = 1114858545142321245;
    Discord.Discord Discord { get; } = new(ClientId, (ulong)CreateFlags.Default);
    MainWindow Window { get; }
    
    public DiscordRPCManager(MainWindow window) {
        Window = window;
        
        DispatcherTimer.Run(Callback, TimeSpan.FromMilliseconds(100));
    }

    public void SetActivity(GameMode gameMode, string? state = null, DateTimeOffset? startTime = null, DateTimeOffset? stopTime = null) {
        string details = $"{gameMode}";

        Assets? smallImage = gameMode switch {
            GameMode.Beginner => Assets.one_star,
            GameMode.Amateur => Assets.two_stars,
            GameMode.Professional => Assets.three_stars,
            GameMode.Custom => Assets.custom,
            _ => null
        };
        
        SetActivity(state, details, startTime, stopTime, smallImage);
    }
    
    public void SetActivity(string? state = null, string? details = null, DateTimeOffset? startTime = null, DateTimeOffset? stopTime = null, Assets? smallImage = null) {
        ActivityAssets assets = new() { LargeImage = $"{Assets.game}", LargeText = "Minesweeper by C6OI#6060" };

        if (smallImage.HasValue) {
            assets.SmallImage = $"{smallImage.Value}";
            
            switch (smallImage.Value) {
                case Assets.waiting:
                    assets.SmallText = "Getting ready";
                    break;

                case Assets.lose_game:
                    assets.SmallText = $"Lost the game in {(stopTime - startTime)?.ToString(@"mm\:ss")}";
                    break;
                
                case Assets.win_game:
                    assets.SmallText = $"Win the game in {(stopTime - startTime)?.ToString(@"mm\:ss")}";
                    break;

                case Assets.one_star:
                    assets.SmallText = "Beginner mode";
                    break;

                case Assets.two_stars:
                    assets.SmallText = "Amateur mode";
                    break;

                case Assets.three_stars:
                    assets.SmallText = "Professional mode";
                    break;

                case Assets.custom:
                    assets.SmallText = "Custom mode";
                    break;

                case Assets.game:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        int mines = Window.MinesCount;
        int width = Window.FieldWidth;
        int height = Window.FieldHeight;
        
        Activity activity = new() {
            Assets = assets,
            State = state ?? "",
            Details = $"{details} ({width}x{height}, {mines} mines)"
        };
        
        if (startTime.HasValue && !stopTime.HasValue) activity.Timestamps = new ActivityTimestamps { Start = startTime.Value.ToUnixTimeSeconds() };

        Discord.GetActivityManager().UpdateActivity(activity, _ => { });
    }

    public void Dispose() => Discord.Dispose();

    bool Callback() {
        Discord.RunCallbacks();
        return true;
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming"), 
 SuppressMessage("ReSharper", "IdentifierTypo")]
public enum Assets {
    game,
    waiting,
    lose_game,
    win_game,
    one_star,
    two_stars,
    three_stars,
    custom
}