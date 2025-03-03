@page "/simon"
@using System.Timers
@using Microsoft.FluentUI.AspNetCore.Components
@inject IJSRuntime JSRuntime

<PageTitle>Simon Says Game</PageTitle>

<FluentCard Width="600px" Height="auto" Class="mx-auto my-4">
    <FluentStack Orientation="Orientation.Vertical" HorizontalAlignment="HorizontalAlignment.Center">
        <h1 class="text-center mb-4">Simon Says</h1>
        
        <div class="text-center mb-3">
            <FluentBadge Appearance="@(isGameActive ? Appearance.Filled : Appearance.Outline)">
                Level: @level
            </FluentBadge>
            <FluentBadge Appearance="Appearance.Filled">
                Score: @score
            </FluentBadge>
            <FluentBadge Appearance="@(highScore > 0 ? Appearance.Accent : Appearance.Outline)">
                High Score: @highScore
            </FluentBadge>
        </div>

        <div class="simon-board mb-4">
            <div class="simon-row">
                <div class="@GetTileClass(0)" @onclick="() => PlayerClick(0)">
                    <FluentIcon Name="@FluentIcons.Star" Size="IconSize.Size32" />
                </div>
                <div class="@GetTileClass(1)" @onclick="() => PlayerClick(1)">
                    <FluentIcon Name="@FluentIcons.Heart" Size="IconSize.Size32" />
                </div>
            </div>
            <div class="simon-row">
                <div class="@GetTileClass(2)" @onclick="() => PlayerClick(2)">
                    <FluentIcon Name="@FluentIcons.Diamond" Size="IconSize.Size32" />
                </div>
                <div class="@GetTileClass(3)" @onclick="() => PlayerClick(3)">
                    <FluentIcon Name="@FluentIcons.Trophy" Size="IconSize.Size32" />
                </div>
            </div>
        </div>

        <div class="text-center mb-4">
            @if (!isGameActive && !isShowingSequence)
            {
                <FluentButton Appearance="Appearance.Accent" @onclick="StartGame">Start Game</FluentButton>
            }
            else
            {
                <FluentProgressRing Visible="isShowingSequence" />
                <div class="status-message">
                    @if (isShowingSequence)
                    {
                        <span>Watch the sequence...</span>
                    }
                    else if (isPlayerTurn)
                    {
                        <span>Your turn! Repeat the sequence...</span>
                    }
                </div>
            }
        </div>
    </FluentStack>
</FluentCard>

@if (showGameOverDialog)
{
    <FluentDialog @bind-Visible="showGameOverDialog" Modal="true" TrapFocus="true">
        <FluentDialogHeader>
            <FluentDialogTitle>Game Over!</FluentDialogTitle>
        </FluentDialogHeader>
        <FluentDialogBody>
            <div>
                <p>Your final score: @score</p>
                @if (score > highScore)
                {
                    <p>New high score! 🎉</p>
                }
            </div>
        </FluentDialogBody>
        <FluentDialogFooter>
            <FluentButton Appearance="Appearance.Accent" @onclick="StartGame">Play Again</FluentButton>
            <FluentButton @onclick="CloseGameOverDialog">Close</FluentButton>
        </FluentDialogFooter>
    </FluentDialog>
}

<style>
    .simon-board {
        display: flex;
        flex-direction: column;
        gap: 10px;
        width: 300px;
        margin: 0 auto;
    }

    .simon-row {
        display: flex;
        gap: 10px;
    }

    .simon-tile {
        width: 145px;
        height: 145px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        transition: all 0.2s ease;
    }

    .simon-tile-red {
        background-color: #ffcccc;
        border: 3px solid #ff6666;
    }
    
    .simon-tile-green {
        background-color: #ccffcc;
        border: 3px solid #66cc66;
    }
    
    .simon-tile-blue {
        background-color: #ccccff;
        border: 3px solid #6666ff;
    }
    
    .simon-tile-yellow {
        background-color: #ffffcc;
        border: 3px solid #ffff66;
    }

    .simon-tile-active {
        transform: scale(0.95);
        box-shadow: 0 0 15px rgba(0, 0, 0, 0.3);
    }

    .simon-tile-red.simon-tile-active {
        background-color: #ff6666;
    }
    
    .simon-tile-green.simon-tile-active {
        background-color: #66cc66;
    }
    
    .simon-tile-blue.simon-tile-active {
        background-color: #6666ff;
    }
    
    .simon-tile-yellow.simon-tile-active {
        background-color: #ffff66;
    }

    .status-message {
        height: 24px;
        margin-top: 8px;
    }
</style>

@code {
    private List<int> gameSequence = new List<int>();
    private List<int> playerSequence = new List<int>();
    private Timer sequenceTimer = new Timer();
    private Timer displayTimer = new Timer();
    private Random random = new Random();
    
    private bool isGameActive = false;
    private bool isShowingSequence = false;
    private bool isPlayerTurn = false;
    private bool showGameOverDialog = false;
    
    private int level = 1;
    private int score = 0;
    private int highScore = 0;
    private int currentSequenceIndex = 0;
    private int[] activeTiles = new int[4] { -1, -1, -1, -1 };

    protected override void OnInitialized()
    {
        sequenceTimer.Interval = 1000;
        sequenceTimer.Elapsed += ShowNextInSequence;
        
        displayTimer.Interval = 500;
        displayTimer.Elapsed += ResetActiveTile;
    }

    private string GetTileClass(int tileIndex)
    {
        string baseClass = tileIndex switch
        {
            0 => "simon-tile simon-tile-red",
            1 => "simon-tile simon-tile-green",
            2 => "simon-tile simon-tile-blue",
            3 => "simon-tile simon-tile-yellow",
            _ => "simon-tile"
        };

        if (activeTiles[tileIndex] == 1)
        {
            baseClass += " simon-tile-active";
        }

        return baseClass;
    }

    private async Task StartGame()
    {
        // Reset game state
        gameSequence.Clear();
        playerSequence.Clear();
        level = 1;
        score = 0;
        isGameActive = true;
        showGameOverDialog = false;
        
        // Start the first round
        await AddToSequence();
    }

    private async Task AddToSequence()
    {
        isShowingSequence = true;
        isPlayerTurn = false;
        playerSequence.Clear();
        
        // Add a new random element to the sequence
        int nextInSequence = random.Next(0, 4);
        gameSequence.Add(nextInSequence);
        
        // Allow the UI to update before starting the sequence
        await InvokeAsync(StateHasChanged);
        await Task.Delay(500);
        
        // Start showing the sequence
        currentSequenceIndex = 0;
        sequenceTimer.Start();
    }

    private async void ShowNextInSequence(object? sender, ElapsedEventArgs e)
    {
        sequenceTimer.Stop();
        
        if (currentSequenceIndex < gameSequence.Count)
        {
            await InvokeAsync(() =>
            {
                // Activate the tile
                activeTiles[gameSequence[currentSequenceIndex]] = 1;
                StateHasChanged();
                
                // Play sound (could be implemented with JS)
                PlayTileSound(gameSequence[currentSequenceIndex]);
                
                // Set timer to deactivate the tile
                displayTimer.Start();
            });
        }
        else
        {
            // We've shown the complete sequence
            await InvokeAsync(() =>
            {
                isShowingSequence = false;
                isPlayerTurn = true;
                StateHasChanged();
            });
        }
    }

    private async void ResetActiveTile(object? sender, ElapsedEventArgs e)
    {
        displayTimer.Stop();
        
        await InvokeAsync(() =>
        {
            // Reset all active tiles
            for (int i = 0; i < activeTiles.Length; i++)
            {
                activeTiles[i] = -1;
            }
            
            StateHasChanged();
        });
        
        // Move to the next item in the sequence
        currentSequenceIndex++;
        
        // Wait before showing the next item
        await Task.Delay(300);
        
        if (isShowingSequence && currentSequenceIndex < gameSequence.Count)
        {
            sequenceTimer.Start();
        }
        else if (isShowingSequence)
        {
            await InvokeAsync(() =>
            {
                isShowingSequence = false;
                isPlayerTurn = true;
                StateHasChanged();
            });
        }
    }

    private async Task PlayerClick(int tileIndex)
    {
        if (!isPlayerTurn || isShowingSequence)
            return;
            
        // Visual feedback
        activeTiles[tileIndex] = 1;
        await InvokeAsync(StateHasChanged);
        
        // Play sound
        PlayTileSound(tileIndex);
        
        // Add to player sequence
        playerSequence.Add(tileIndex);
        
        // Check if the move is correct
        int currentMove = playerSequence.Count - 1;
        if (playerSequence[currentMove] != gameSequence[currentMove])
        {
            // Wrong move
            await GameOver();
            return;
        }
        
        // Reset tile after a delay
        await Task.Delay(300);
        activeTiles[tileIndex] = -1;
        await InvokeAsync(StateHasChanged);
        
        // Check if the player completed the sequence
        if (playerSequence.Count == gameSequence.Count)
        {
            // Player completed the sequence correctly
            level++;
            score += level * 10;
            
            // Wait a bit before starting the next sequence
            await Task.Delay(500);
            await AddToSequence();
        }
    }

    private async Task GameOver()
    {
        isGameActive = false;
        isPlayerTurn = false;
        
        // Update high score if needed
        if (score > highScore)
        {
            highScore = score;
        }
        
        // Reset active tiles
        for (int i = 0; i < activeTiles.Length; i++)
        {
            activeTiles[i] = -1;
        }
        
        await InvokeAsync(() => {
            showGameOverDialog = true;
            StateHasChanged();
        });
    }

    private void CloseGameOverDialog()
    {
        showGameOverDialog = false;
    }

    private async void PlayTileSound(int tileIndex)
    {
        // This would be implemented with JS interop to play different sounds for each tile
        // For now, we'll just have a placeholder method
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        sequenceTimer?.Dispose();
        displayTimer?.Dispose();
    }
}
