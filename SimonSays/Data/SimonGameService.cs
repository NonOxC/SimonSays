using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace SimonSays.Data
{
    public class SimonGameService
    {
        private readonly List<int> _sequence = new();
        private readonly Random _random = new();
        private int _currentStep = 0;
        private CancellationTokenSource? _cancellationTokenSource;

        public bool IsPlaying { get; private set; }
        public bool IsPlayerTurn { get; private set; }
        public int CurrentLevel => _sequence.Count;
        public int HighScore { get; private set; }

        public event Action? OnStateChanged;
        public event Action<int>? OnButtonHighlight;
        public event Action? OnButtonClear;

        private readonly object _lock = new(); // Ensures thread safety

        private const int ColorCount = 4; // Number of colors (0-3)

        public async Task StartGame()
        {
            // Cancel any ongoing sequence playback
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // Lock only for modifying `_sequence`, but do not await inside lock
            lock (_lock)
            {
                _sequence.Clear();
                IsPlaying = true;
                _currentStep = 0;
            }

            NotifyStateChanged();
            await Task.Delay(500, _cancellationTokenSource.Token);
            await AddToSequence(_cancellationTokenSource.Token);
        }

        public async Task AddToSequence(CancellationToken cancellationToken)
        {
            int newColor;
            lock (_lock)
            {
                newColor = _random.Next(ColorCount);
                _sequence.Add(newColor);
            }

            NotifyStateChanged();
            await Task.Delay(500, cancellationToken);
            await PlaySequence(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                IsPlayerTurn = true;
                NotifyStateChanged();
            }
        }

        public async Task PlaySequence(CancellationToken cancellationToken)
        {
            IsPlayerTurn = false;
            NotifyStateChanged();

            List<int> sequenceCopy;
            lock (_lock)
            {
                sequenceCopy = new List<int>(_sequence); // Copy the list before iterating
            }

            foreach (int color in sequenceCopy)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                OnButtonHighlight?.Invoke(color);
                await Task.Delay(600, cancellationToken);
                OnButtonClear?.Invoke();
                await Task.Delay(200, cancellationToken);
            }
        }

        public async Task ButtonPressed(int colorIndex)
        {
            if (!IsPlaying || !IsPlayerTurn) return;

            OnButtonHighlight?.Invoke(colorIndex);
            await Task.Delay(300);
            OnButtonClear?.Invoke();

            bool isCorrect;
            lock (_lock)
            {
                isCorrect = _sequence[_currentStep] == colorIndex;
            }

            if (isCorrect)
            {
                _currentStep++;

                if (_currentStep >= _sequence.Count)
                {
                    _currentStep = 0;
                    IsPlayerTurn = false;
                    NotifyStateChanged();
                    await Task.Delay(500);
                    await AddToSequence(_cancellationTokenSource!.Token);
                }
            }
            else
            {
                lock (_lock)
                {
                    if (CurrentLevel > HighScore)
                    {
                        HighScore = CurrentLevel;
                    }

                    IsPlaying = false;
                    IsPlayerTurn = false;
                    _currentStep = 0;
                }

                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}
