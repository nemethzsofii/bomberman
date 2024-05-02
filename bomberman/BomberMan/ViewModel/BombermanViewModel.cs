using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Windows.Threading;
using Model.Board;
using Model.Entities;
using Model.EventsArgs;
using System.Diagnostics.Eventing.Reader;
using Model.Entities.Monsters;
using System.Threading;
using BomberMan.View;

namespace BomberMan.ViewModel
{
    public class BombermanViewModel : ViewModelBase
    {
        #region Fields

        private int width;
        private int height;
        private int counter = 0;
        private string gameOverText ="";
        private GameBoard board;        
        private DispatcherTimer? timer;
        private bool canPlay = false;
        private bool canStart = true;
        private int player1Wins = 0;
        private int player2Wins = 0;
        private Button[,] buttonGrid;
        private int game_counter = 0;
        public event EventHandler<BomberManEventArgs> ToEndView;

        #endregion

        #region Properties

        public int Width
        {
            get => width;
            set
            {
                width = value;
                OnPropertyChanged(nameof(Width));
            }
        }
        public int Height
        {
            get => height;
            set
            {
                height = value;
                OnPropertyChanged(nameof(Height));
            }
        }
        public Player Player1 => board.Players[0];
        public Player Player2 => board.Players[1];
        public int Player1Wins
        {
            get => player1Wins;
            set
            {
                player1Wins = value;
                OnPropertyChanged(nameof(Player1Wins));
            }
        }
        public int Player2Wins
        {
            get => player2Wins;
            set
            {
                player2Wins = value;
                OnPropertyChanged(nameof(Player2Wins));
            }
        }
        public string GameOverText
        {
            get => gameOverText;
            private set
            {
                gameOverText = value;
                OnPropertyChanged(nameof(gameOverText));
            }
        }
        public int Counter
        {
            get => counter;
            set
            {
                counter = value;
                OnPropertyChanged(nameof(Counter));
            }
        }
        public ObservableCollection<Button>? Buttons { get; private set; }
        public  ICommand StartGame { get; set; }
        public ICommand RestartGame {  get; set; }
         

        #endregion

        #region Constructor

        public BombermanViewModel(int nheight, int nwidth)
        {
            board = new GameBoard(nheight, nwidth);

            Height = nheight;
            Width = nwidth;

            buttonGrid = new Button[nheight, nwidth];

            InitializeBoard();

            StartGame = new DelegateCommand(param => Start());
            RestartGame = new DelegateCommand(param => Restart());

            board.GameOver += GameOverVM;
            board.PlayerMoved += UpdatePlayerMove;
            board.BombPlaced += BombPlaced;
            board.PlayerUpdate += UpdatePlayerStatus;
            board.MonsterMoved += UpdateMonsterMove;
            board.Explosion += UpdateExploded;
            board.ExplosionClear += ClearExploded;
            board.BarrierPlaced += BarrierPlaced;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        #endregion

        #region Private methods

        #region Board Updates

        private void InitializeBoard()
        {
            if (Buttons == null)
                Buttons = new ObservableCollection<Button>();

            Buttons.Clear();

            List<(int azon, int X, int Y)> monsterCoords = new();

            foreach (Monster m in board.Monsters)
                monsterCoords.Add((m.azon, m.X, m.Y));

            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    Button btn = new()
                    {
                        X = i,
                        Y = j,
                        ImageSource = GetBaseImage(i, j),
                        OverlaySource = null
                    };

                    var monsterAtPos = monsterCoords.FirstOrDefault(mc => mc.X == i && mc.Y == j);
                    if (monsterAtPos != default)
                        btn.OverlaySource = $"pack://application:,,,/Images/Monsters/monster{monsterAtPos.azon}.png";
                    if (btn.X == Player1.X && btn.Y == Player1.Y)
                        btn.OverlaySource = "pack://application:,,,/Images/Players/p1.png";
                    if (btn.X == Player2.X && btn.Y == Player2.Y)
                        btn.OverlaySource = "pack://application:,,,/Images/Players/p2.png";
                    Buttons.Add(btn);
                    buttonGrid[i, j] = btn;
                }
            }

            UpdateLabels();
        }
        private void RedrawBoard()
        {
            if (Buttons == null) return;

            List<(int azon, int X, int Y)> monsterCoords = new();
            foreach (Monster m in board.Monsters)
                monsterCoords.Add((m.azon, m.X, m.Y));

            if (Buttons == null) return;
            foreach (Button btn in Buttons)
            {
                btn.ImageSource = GetBaseImage(btn.X, btn.Y);
                btn.OverlaySource = null;
                var monsterAtPos = monsterCoords.FirstOrDefault(mc => mc.X == btn.X && mc.Y == btn.Y);
                if (monsterAtPos != default)
                    btn.OverlaySource = $"pack://application:,,,/Images/Monsters/monster{monsterAtPos.azon}.png";
                if (btn.X == Player1.X && btn.Y == Player1.Y)
                    btn.OverlaySource = "pack://application:,,,/Images/Players/p1.png";
                if (btn.X == Player2.X && btn.Y == Player2.Y)
                    btn.OverlaySource = "pack://application:,,,/Images/Players/p2.png";
            }

            UpdateLabels();
        }
        private void UpdateMonsterMove(object? sender, MonsterMovedEventArgs e)
        {
            if (Buttons == null)
                return;
            var oldBtn = buttonGrid[e.OldCoords.Item1, e.OldCoords.Item2];
            if (oldBtn != null)
                UpdateCell(e.OldCoords.Item1, e.OldCoords.Item2);
            var newBtn = buttonGrid[e.NewCoords.Item1, e.NewCoords.Item2];
            if (newBtn != null)
                UpdateCell(e.NewCoords.Item1, e.NewCoords.Item2);
        }
        private void BombPlaced(object? sender, BombPlacedEventArgs e)
        {
            var btn = buttonGrid[e.X, e.Y];
            if (btn == null) return;
            btn.OverlaySource = "pack://application:,,,/Images/Bombs/bomb.png";
            UpdateLabels();
        }
        private void BarrierPlaced(object? sender, BarrierPlacedEventArgs e)
        {
            UpdateCell(e.X, e.Y);
            UpdateLabels();
        }
        private void UpdatePlayerStatus(object? sender, PlayerUpdateEventArgs e)
        {
            UpdatePlayerImage(e.Player, buttonGrid[e.Player.X, e.Player.Y]);
        }
        private void UpdateExploded(object? sender, ExplosionEventArgs e)
        {
            var btn = buttonGrid[e.X, e.Y];
            if (btn == null) return;
            btn.OverlaySource = "pack://application:,,,/Images/Bombs/explosion.png";
            //UpdateCell(e.X,e.Y);
        }
        private void ClearExploded(object? sender, ExplosionEventArgs e)
        {
            UpdateCell(e.X,e.Y);
            UpdateLabels();
        }
        private void UpdatePlayerMove(object? sender, PlayerMovedEventArgs e)
        {
            if (Buttons == null)
                return;

            var oldBtn = buttonGrid[e.OldCoords.Item1, e.OldCoords.Item2];
            if (oldBtn != null)
                UpdateCell(e.OldCoords.Item1, e.OldCoords.Item2);

            var newBtn = buttonGrid[e.NewCoords.Item1, e.NewCoords.Item2];
            if (newBtn != null)
            {
                Player playerToUpdate = e.PId == 0 ? Player1 : Player2;
                UpdatePlayerImage(playerToUpdate, newBtn);
            }
            UpdateLabels();
        }
        private void UpdateCell(int x, int y)
        {
            var btn = buttonGrid[x, y];
            if (btn == null) return;

            btn.ImageSource = GetBaseImage(x, y);

            if (IsBombAt(x, y))
            {
                btn.OverlaySource = "pack://application:,,,/Images/Bombs/bomb.png";
                return;
            }

            bool player1Present = x == Player1.X && y == Player1.Y;
            bool player2Present = x == Player2.X && y == Player2.Y;

            if (player1Present || player2Present)
            {
                Player playerToUpdate = player1Present ? Player1 : Player2;
                UpdatePlayerImage(playerToUpdate, btn);
                return;
            }

            var monster = board.Monsters.FirstOrDefault(m => m.X == x && m.Y == y);
            if (monster != null && monster.Alive)
            {
                btn.OverlaySource = $"pack://application:,,,/Images/Monsters/monster{monster.azon}.png";
                return;
            }

            if (board.Board[x, y].PowerUp != null)
            {
                btn.OverlaySource = GetPowerUpImage(board.Board[x, y].PowerUp);
                return;
            }
            btn.OverlaySource = null;
        }
        private bool IsBombAt(int x, int y)
        {
            return board.Bombs.Any(bomb => bomb.X == x && bomb.Y == y);
        }
        private static string GetPowerUpImage(Modifiers? powerUp)
        {
            switch (powerUp)
            {
                case Modifiers.PlusBombCapacity:
                    return "pack://application:,,,/Images/Modifiers/bombsup.png";

                case Modifiers.PlusBombRange:
                    return "pack://application:,,,/Images/Modifiers/rangeup.png";

                case Modifiers.Invincibility:
                    return "pack://application:,,,/Images/Modifiers/invincibility.png";

                case Modifiers.RollerSkates:
                    return "pack://application:,,,/Images/Modifiers/rollerblades.png";

                case Modifiers.Ghost:
                    return "pack://application:,,,/Images/Modifiers/ghost.png";

                case Modifiers.Barrier:
                    return "pack://application:,,,/Images/Modifiers/barrier.png";

                default:
                    return "pack://application:,,,/Images/Modifiers/debuff.png";
            }
        }
        private string GetBaseImage(int x, int y)
        {
            string path = "pack://application:,,,/Images/Base/";
            if (board.Board[x, y].Wall)
                path += "wall.png";
            else if (board.Board[x, y].Box)
                path += "box.png";
            else
                path += "road.png";
            return path;
        }
        private void UpdatePlayerImage(Player player, Button btn)
        {
            string imageSource;
            if (!player.Alive)
                imageSource = "pdead.png";
            else if (player.HasGhost)
            {
                if (player.HasInvincibility)
                    imageSource = player == Player1 ? "p1invincibleghost.png" : "p2invincibleghost.png";
                else
                    imageSource = player == Player1 ? "p1ghost.png" : "p2ghost.png";
            }
            else if (player.HasInvincibility)
                imageSource = player == Player1 ? "p1invincible.png" : "p2invincible.png";
            else
                imageSource = player == Player1 ? "p1.png" : "p2.png";

            btn.OverlaySource = "pack://application:,,,/Images/Players/" + imageSource;
        }
        private void UpdateLabels()
        {
            OnPropertyChanged(nameof(Player1));
            OnPropertyChanged(nameof(Player2));
        }

        #endregion

        private void Start()
        {
            game_counter++;
            if (canStart && timer != null)
            {
                canStart = false;
                canPlay = true;
                timer.Start();
                board.TurnOnMonsterMoves();
            }
        }
        private void Restart()
        {
            board.RestartRequested = true;
            timer?.Stop();
            Counter = 0;
            GameOverText = "";
            canPlay = false;
            canStart = true;

            board.GameOver -= GameOverVM;
            board.PlayerMoved -= UpdatePlayerMove;
            board.BombPlaced -= BombPlaced;
            board.PlayerUpdate -= UpdatePlayerStatus;
            board.MonsterMoved -= UpdateMonsterMove;
            board.Explosion -= UpdateExploded;
            board.BarrierPlaced -= BarrierPlaced;

            board.Dispose();
            board = new GameBoard(Height, Width);
            board.GameOver += GameOverVM;
            board.PlayerMoved += UpdatePlayerMove;
            board.BombPlaced += BombPlaced;
            board.PlayerUpdate += UpdatePlayerStatus;
            board.MonsterMoved += UpdateMonsterMove;
            board.Explosion += UpdateExploded;
            board.ExplosionClear += ClearExploded;
            board.BarrierPlaced += BarrierPlaced;

            RedrawBoard();
            board.RestartRequested = false;
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            Counter++;
        }
        private void GameOverVM(object? sender, GameOverEventArgs e)
        {
            if(game_counter == 3)
            {
                ToEndView?.Invoke(this, new BomberManEventArgs(player1Wins, player2Wins));
                game_counter = 0;


                player1Wins = 0;
                player2Wins = 0;

                counter = 0;
                Restart();
            }
            else
            {
                if (e.DeadPlayerList != null && timer != null)
                {
                    UpdateCell(Player1.X, Player1.Y);
                    UpdateCell(Player2.X, Player2.Y);
                    if (e.DeadPlayerList.Count == 2)
                        GameOverText = "Tie!";
                    else if (e.DeadPlayerList.Contains(Player1))
                    {
                        GameOverText = "Player2 Wins!";
                        Player2Wins++;
                    }
                    else
                    {
                        GameOverText = "Player1 Wins!";
                        Player1Wins++;
                    }
                    Counter = 0;
                    timer.IsEnabled = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(GameOverText, "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    Restart();
                }
            }
            
        }

        #endregion

        #region Public methods

        public void MovePlayer(int p, int dir)
        {
            if (canPlay)
                board.MovePlayer(p, dir);
        }
        public void PlaceBomb(int p)
        {
            if (canPlay)
                board.PlaceBomb(p);
        }
        public void PlaceBarrier(int p)
        {
            if (canPlay)
                board.PlaceBarrier(p);
        }

        #endregion
    }
}