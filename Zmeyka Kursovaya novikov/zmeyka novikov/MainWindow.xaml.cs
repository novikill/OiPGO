using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SnakeGame
{
    public class HighScoreData
    {
        public int ClassicModeScore { get; set; }
        public int HardModeScore { get; set; }
        public DateTime LastPlayed { get; set; }

        public HighScoreData()
        {
            ClassicModeScore = 0;
            HardModeScore = 0;
            LastPlayed = DateTime.Now;
        }
    }

    public partial class MainWindow : Window
    {
        private int gridSize = 20;
        private int cellSize;
        private int currentScore = 0;
        private bool isGameRunning = false;
        private bool isPaused = false;
        private bool isHardMode = false;

        private List<Point> snake = new List<Point>();
        private Point food;
        private List<Point> obstacles = new List<Point>();

        private Point direction = new Point(1, 0);
        private Point nextDirection = new Point(1, 0);

        private DispatcherTimer gameTimer;
        private int baseSpeed = 150;
        private int currentSpeed;

        private Brush snakeBrush = new SolidColorBrush(Color.FromRgb(0, 255, 100));
        private Brush foodBrush = new SolidColorBrush(Color.FromRgb(255, 50, 50));
        private Brush obstacleBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        private Brush gridLineBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70));

        private HighScoreData highScores;
        private string saveFilePath = "snake_scores.json";

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
            LoadHighScores();
            UpdateHighScoreDisplay();
            this.KeyDown += MainWindow_KeyDown;
        }

        private void InitializeGame()
        {
            if (GridSizeComboBox.SelectedItem is ComboBoxItem selected)
            {
                gridSize = int.Parse(selected.Tag.ToString());
            }

            cellSize = 450 / gridSize;
            GameCanvas.Children.Clear();
            DrawGrid();

            snake.Clear();
            int startX = gridSize / 2;
            int startY = gridSize / 2;
            snake.Add(new Point(startX, startY));
            snake.Add(new Point(startX - 1, startY));
            snake.Add(new Point(startX - 2, startY));

            direction = new Point(1, 0);
            nextDirection = new Point(1, 0);

            currentScore = 0;
            ScoreText.Text = "0";

            GenerateFood();

            isHardMode = HardModeCheckBox.IsChecked == true;
            if (isHardMode)
            {
                GenerateObstacles();
            }
            else
            {
                obstacles.Clear();
            }

            UpdateSpeed();
            DrawGame();

            if (gameTimer == null)
            {
                gameTimer = new DispatcherTimer();
                gameTimer.Tick += GameTimer_Tick;
            }
            gameTimer.Interval = TimeSpan.FromMilliseconds(currentSpeed);
            gameTimer.Stop();

            isGameRunning = false;
            isPaused = false;
            UpdateUIState();
        }

        private void DrawGrid()
        {
            for (int i = 0; i <= gridSize; i++)
            {
                Line vLine = new Line
                {
                    X1 = i * cellSize,
                    Y1 = 0,
                    X2 = i * cellSize,
                    Y2 = 450,
                    Stroke = gridLineBrush,
                    StrokeThickness = 0.5
                };
                GameCanvas.Children.Add(vLine);

                Line hLine = new Line
                {
                    X1 = 0,
                    Y1 = i * cellSize,
                    X2 = 450,
                    Y2 = i * cellSize,
                    Stroke = gridLineBrush,
                    StrokeThickness = 0.5
                };
                GameCanvas.Children.Add(hLine);
            }
        }

        private void DrawGame()
        {
            var itemsToRemove = GameCanvas.Children.Cast<UIElement>()
                .Where(x => !(x is Line)).ToList();
            foreach (var item in itemsToRemove)
            {
                GameCanvas.Children.Remove(item);
            }

            Rectangle foodRect = new Rectangle
            {
                Width = cellSize - 2,
                Height = cellSize - 2,
                Fill = foodBrush,
                RadiusX = cellSize / 4,
                RadiusY = cellSize / 4
            };
            Canvas.SetLeft(foodRect, food.X * cellSize + 1);
            Canvas.SetTop(foodRect, food.Y * cellSize + 1);
            GameCanvas.Children.Add(foodRect);

            foreach (var obstacle in obstacles)
            {
                Rectangle obstacleRect = new Rectangle
                {
                    Width = cellSize - 2,
                    Height = cellSize - 2,
                    Fill = obstacleBrush,
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(obstacleRect, obstacle.X * cellSize + 1);
                Canvas.SetTop(obstacleRect, obstacle.Y * cellSize + 1);
                GameCanvas.Children.Add(obstacleRect);
            }

            for (int i = 0; i < snake.Count; i++)
            {
                Rectangle snakeRect = new Rectangle
                {
                    Width = cellSize - 2,
                    Height = cellSize - 2,
                    Fill = snakeBrush,
                    RadiusX = cellSize / 4,
                    RadiusY = cellSize / 4
                };

                if (i == 0)
                {
                    snakeRect.Fill = new SolidColorBrush(Color.FromRgb(50, 255, 100));
                    snakeRect.Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    snakeRect.StrokeThickness = 2;
                }

                Canvas.SetLeft(snakeRect, snake[i].X * cellSize + 1);
                Canvas.SetTop(snakeRect, snake[i].Y * cellSize + 1);
                GameCanvas.Children.Add(snakeRect);
            }
        }

        private void GenerateFood()
        {
            Random rand = new Random();
            bool foodPlaced = false;

            while (!foodPlaced)
            {
                Point newFood = new Point(rand.Next(0, gridSize), rand.Next(0, gridSize));

                bool onSnake = false;
                foreach (var segment in snake)
                {
                    if (segment.X == newFood.X && segment.Y == newFood.Y)
                    {
                        onSnake = true;
                        break;
                    }
                }

                bool onObstacle = false;
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.X == newFood.X && obstacle.Y == newFood.Y)
                    {
                        onObstacle = true;
                        break;
                    }
                }

                if (!onSnake && !onObstacle)
                {
                    food = newFood;
                    foodPlaced = true;
                }
            }
        }

        private void GenerateObstacles()
        {
            Random rand = new Random();
            int obstacleCount = gridSize / 2;
            obstacles.Clear();

            for (int i = 0; i < obstacleCount; i++)
            {
                bool obstaclePlaced = false;
                int attempts = 0;

                while (!obstaclePlaced && attempts < 100)
                {
                    Point newObstacle = new Point(rand.Next(0, gridSize), rand.Next(0, gridSize));

                    bool onSnake = false;
                    foreach (var segment in snake)
                    {
                        if (segment.X == newObstacle.X && segment.Y == newObstacle.Y)
                        {
                            onSnake = true;
                            break;
                        }
                    }

                    bool onFood = (newObstacle.X == food.X && newObstacle.Y == food.Y);
                    bool onOtherObstacle = obstacles.Any(o => o.X == newObstacle.X && o.Y == newObstacle.Y);

                    if (!onSnake && !onFood && !onOtherObstacle)
                    {
                        obstacles.Add(newObstacle);
                        obstaclePlaced = true;
                    }
                    attempts++;
                }
            }
        }

        private void UpdateSpeed()
        {
            int speedLevel = currentScore / 5;
            int newSpeed = Math.Max(50, baseSpeed - speedLevel * 5);
            currentSpeed = newSpeed;

            SpeedText.Text = (baseSpeed - speedLevel * 5).ToString();
            if (currentSpeed < 50) SpeedText.Text = "MAX";

            if (gameTimer != null && isGameRunning && !isPaused)
            {
                gameTimer.Interval = TimeSpan.FromMilliseconds(currentSpeed);
            }
        }

        private void MoveSnake()
        {
            direction = nextDirection;
            Point newHead = new Point(snake[0].X + direction.X, snake[0].Y + direction.Y);
            bool ateFood = (newHead.X == food.X && newHead.Y == food.Y);

            snake.Insert(0, newHead);

            if (!ateFood)
            {
                snake.RemoveAt(snake.Count - 1);
            }
            else
            {
                currentScore++;
                ScoreText.Text = currentScore.ToString();
                UpdateHighScore();
                GenerateFood();
                UpdateSpeed();

                if (isHardMode && currentScore % 5 == 0)
                {
                    GenerateObstacles();
                }
            }

            if (CheckCollision())
            {
                GameOver();
                return;
            }

            DrawGame();
        }

        private bool CheckCollision()
        {
            Point head = snake[0];

            if (head.X < 0 || head.X >= gridSize || head.Y < 0 || head.Y >= gridSize)
            {
                return true;
            }

            for (int i = 1; i < snake.Count; i++)
            {
                if (head.X == snake[i].X && head.Y == snake[i].Y)
                {
                    return true;
                }
            }

            foreach (var obstacle in obstacles)
            {
                if (head.X == obstacle.X && head.Y == obstacle.Y)
                {
                    return true;
                }
            }

            return false;
        }

        private void GameOver()
        {
            isGameRunning = false;
            gameTimer.Stop();
            GameStatusText.Text = $"💀 ИГРА ОКОНЧЕНА! Счет: {currentScore} 💀";
            SaveHighScores();

            MessageBox.Show($"Игра окончена!\nВаш счет: {currentScore}\n" +
                           $"Рекорд в этом режиме: {(isHardMode ? highScores.HardModeScore : highScores.ClassicModeScore)}",
                           "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);

            UpdateUIState();
        }

        private void UpdateHighScore()
        {
            if (isHardMode)
            {
                if (currentScore > highScores.HardModeScore)
                {
                    highScores.HardModeScore = currentScore;
                    highScores.LastPlayed = DateTime.Now;
                    SaveHighScores();
                }
            }
            else
            {
                if (currentScore > highScores.ClassicModeScore)
                {
                    highScores.ClassicModeScore = currentScore;
                    highScores.LastPlayed = DateTime.Now;
                    SaveHighScores();
                }
            }
            UpdateHighScoreDisplay();
        }

        private void UpdateHighScoreDisplay()
        {
            int displayScore = isHardMode ? highScores.HardModeScore : highScores.ClassicModeScore;
            HighScoreText.Text = displayScore.ToString();
        }

        private void LoadHighScores()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    highScores = JsonSerializer.Deserialize<HighScoreData>(json);
                }
                else
                {
                    highScores = new HighScoreData();
                }
            }
            catch (Exception)
            {
                highScores = new HighScoreData();
            }
        }

        private void SaveHighScores()
        {
            try
            {
                string json = JsonSerializer.Serialize(highScores);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения рекордов: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateUIState()
        {
            StartButton.IsEnabled = !isGameRunning;
            PauseButton.IsEnabled = isGameRunning;
            ResetButton.IsEnabled = true;
            HardModeCheckBox.IsEnabled = !isGameRunning;
            GridSizeComboBox.IsEnabled = !isGameRunning;

            if (isPaused && isGameRunning)
            {
                PauseButton.Content = "▶ ПРОДОЛЖИТЬ";
            }
            else
            {
                PauseButton.Content = "⏸ ПАУЗА";
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (isGameRunning && !isPaused)
            {
                MoveSnake();

                if (gameTimer.Interval.TotalMilliseconds != currentSpeed)
                {
                    gameTimer.Interval = TimeSpan.FromMilliseconds(currentSpeed);
                }
            }
        }

        private void StartGame()
        {
            isGameRunning = true;
            isPaused = false;
            gameTimer.Interval = TimeSpan.FromMilliseconds(currentSpeed);
            gameTimer.Start();
            GameStatusText.Text = "🎮 Игра идет...";
            UpdateUIState();
        }

        private void ResetGame()
        {
            if (gameTimer != null && gameTimer.IsEnabled)
            {
                gameTimer.Stop();
            }
            InitializeGame();
            GameStatusText.Text = "Готов к игре! Нажмите СТАРТ";
            UpdateUIState();
        }

        private void TogglePause()
        {
            if (!isGameRunning) return;

            isPaused = !isPaused;
            if (isPaused)
            {
                GameStatusText.Text = "⏸ ПАУЗА";
            }
            else
            {
                GameStatusText.Text = "🎮 Игра идет...";
            }
            UpdateUIState();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePause();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (isGameRunning && !isPaused)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        if (direction.Y != 1)
                            nextDirection = new Point(0, -1);
                        break;
                    case Key.Down:
                        if (direction.Y != -1)
                            nextDirection = new Point(0, 1);
                        break;
                    case Key.Left:
                        if (direction.X != 1)
                            nextDirection = new Point(-1, 0);
                        break;
                    case Key.Right:
                        if (direction.X != -1)
                            nextDirection = new Point(1, 0);
                        break;
                }
            }

            if (e.Key == Key.Space && isGameRunning)
            {
                TogglePause();
            }

            if (e.Key == Key.R)
            {
                ResetGame();
            }

            if (e.Key == Key.Enter && !isGameRunning)
            {
                StartGame();
            }
        }
    }
}