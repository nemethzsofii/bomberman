using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Model.Entities;
using Model.Entities.Monsters;
using Model.EventsArgs;

namespace Model.Board
{
    public class GameBoard
    {
        #region Fields

        private Player[] players = new Player[2];
        private List<Bomb> bombs;
        private List<Monster> monsters;
        private List<(int, int, int)> barriers;
        private System.Timers.Timer monsterMoveTimer;
        private System.Timers.Timer? gameOverTimer;

        #endregion

        #region Properties

        public int Height { get; private set; }
        public int Width { get; private set; }
        public Cell[,] Board { get; private set; }
        public Player[] Players { get { return players; } }
        public List<Bomb> Bombs { get { return bombs; } }
        public List<Monster> Monsters {  get { return monsters; } }
        public List<(int, int, int)> Barriers { get { return barriers; } }
        public bool RestartRequested { get; set; } = false;
        public bool Over { get; private set; }

        #endregion

        #region Constructor

        public GameBoard(int height, int width)
        {
            Width = width;
            Height = height;
            Board = new Cell[Height, Width];
            Over = false;

            Random random = new();
            int boxNum = 30;

            int monsterCount = 4;


            List<(int, int)> posAv = new();
            bombs = new();
            monsters = new();
            barriers = new();

            players[0] = new Player(1, Width / 2, this);
            players[1] = new Player(height - 2, Width / 2, this);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Board[i, j] = new Cell(false, false);
                    if (i == 0 || i == height - 1 || j == 0 || j == width - 1 || i % 2 == 0 && j % 2 == 0)
                        Board[i, j].Wall = true;
                    else if (!((i == 1 || i == 2 || i == 3 || i == Height - 4 || i == Height - 3 || i == Height - 2) && (j == (Width / 2) - 2 || j == (Width / 2) - 1 || j == Width / 2 || j == (Width / 2) + 1 || j == (Width / 2) + 2)))
                        posAv.Add((i, j));
                }
            }

            for (int i = 0; i < boxNum; i++)
            {
                int posInd = random.Next(posAv.Count);
                (int x, int y) = posAv[posInd];
                Board[x, y].Box = true;
                posAv.RemoveAt(posInd);
            }

            for (int i = 0; i < monsterCount; i++)
            {
                int posInd = random.Next(posAv.Count);
                (int x, int y) = posAv[posInd];
                int ir = random.Next(0,4);
                if (ir == 0)
                {
                    monsters.Add(new DefaultMonster(i, x, y, this, 0));
                }
                else if (ir == 1)
                {
                    monsters.Add(new Specter(i, x, y, this, 1));
                }
                else if (ir == 2)
                {
                    monsters.Add(new Stalker(i, x, y, this, 2));
                }
                else if (ir == 3)
                {
                    monsters.Add(new Wonderer(i, x, y, this, 3));
                }
                posAv.RemoveAt(posInd);
            }
            
            monsterMoveTimer = new System.Timers.Timer(1000);
            monsterMoveTimer.Elapsed += OnMonsterMoveTimerElapsed;
            monsterMoveTimer.AutoReset = true;

            gameOverTimer = new System.Timers.Timer(2000);
            gameOverTimer.Elapsed += GetResult;
            gameOverTimer.Enabled = false;
        }

        #endregion

        #region Private methods

        private void AffectPlayersAt(int x, int y)
        {
            foreach (Player p in players)
            {
                if (p.X == x && p.Y == y && p.Alive)
                {
                    p.Kill();
                }
            }
        }
        private void AffectBombsAt(int x, int y)
        {
            List<Bomb> marked = new List<Bomb>();
            foreach (Bomb b in bombs)
            {
                if (b.X == x && b.Y == y)
                {
                    marked.Add(b);
                }
            }
            foreach (Bomb b in marked)
            {
                ExplodeBomb(b);
            }
        }
        private void AffectMonstersAt(int x, int y)
        {
            foreach (Monster m in monsters)
            {
                if (m.X == x && m.Y == y && m.Alive)
                {
                    m.Kill();
                }
            }
        }
        private void OnMonsterMoveTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            foreach (Monster m in monsters)
                if (m.Alive)
                {
                    var oldCoords = (m.X, m.Y);
                    m.Move();
                    OnMonsterMoved(new MonsterMovedEventArgs(oldCoords, (m.X, m.Y), m.Id));
                }
        }
        private void GetResult(object? sender, ElapsedEventArgs e)
        {
            if (gameOverTimer == null)
                return;

            Over = true;
            List<Player> deadPlayers = new();
            gameOverTimer.Enabled = false;
            foreach (Player p in players)
                if (!p.Alive)
                    deadPlayers.Add(p);
            monsterMoveTimer.Enabled = false;
            OnGameOver(new GameOverEventArgs(deadPlayers));
        }

        #endregion

        #region Public methods

        public void AffectEntitiesAt(int x, int y)
        {
            AffectPlayersAt(x, y);
            AffectMonstersAt(x, y);
            AffectBombsAt(x, y);
            if (Board[x, y].Box)
                Board[x, y].OnBoxExplosion(x, y, this);

            OnExplosion(new ExplosionEventArgs(x, y));
        }
        public void CleanBomb(List<(int,int)> coords)
        {
            foreach(var c in coords)
            {
                OnExplosionClear(new ExplosionEventArgs(c.Item1, c.Item2));
            }
        }
        public void Dispose()
        {
            monsterMoveTimer?.Stop();
            monsterMoveTimer?.Dispose();
            gameOverTimer?.Stop();
            gameOverTimer?.Dispose();
            gameOverTimer = null;
        }
        public void TurnOnMonsterMoves()
        {
            monsterMoveTimer.Enabled = true;
        }
        public void MovePlayer(int p, int dir)
        {
            var oldCoords = (players[p].X, players[p].Y);
            if (players[p].Move(dir))
                OnPlayerMoved(new PlayerMovedEventArgs(oldCoords, (players[p].X, players[p].Y), p));
        }
        public void PlaceBomb(int p)
        {
            int px = players[p].X;
            int py = players[p].Y;
            if(players[p].PlaceBomb())
                OnBombPlaced(new BombPlacedEventArgs(px, py));
        }
        public void PlaceBarrier(int p)
        {
            int px = players[p].X;
            int py = players[p].Y;
            if (players[p].PlaceBarrier(p))
                OnBarrierPlaced(new BarrierPlacedEventArgs(px, py));
        }
        public void ExplodeBomb(Bomb b)
        {
            List<Explosion> Explosions = new List<Explosion>();
            Bombs.Remove(b);
            for(int i = 0; i <= 3; i++)
            {
                Explosions.Add(new Explosion(b.X, b.Y, b.Range, i, this));
            }
            List<(int, int)> coorrds = new List<(int, int)>();
            foreach (Explosion item in Explosions)
            {
                foreach(var c in item.coords)
                {
                    coorrds.Add(c);
                }
            }
            CleanBomb(coorrds);
        }
        public void StartGameOverTimer()
        {
            if (gameOverTimer != null)
                gameOverTimer.Enabled = true;
        }

        #endregion

        #region Events

        public event EventHandler<GameOverEventArgs>? GameOver;
        public event EventHandler<PlayerMovedEventArgs>? PlayerMoved;
        public event EventHandler<BombPlacedEventArgs>? BombPlaced;
        public event EventHandler<PlayerUpdateEventArgs>? PlayerUpdate;
        public event EventHandler<MonsterMovedEventArgs>? MonsterMoved;
        public event EventHandler<ExplosionEventArgs>? Explosion;
        public event EventHandler<ExplosionEventArgs>? ExplosionClear;
        public event EventHandler<BarrierPlacedEventArgs>? BarrierPlaced;
        public void OnGameOver(GameOverEventArgs e)
        {
            if (!RestartRequested)
            {
                GameOver?.Invoke(this, e);
            }
        }
        public void OnPlayerMoved(PlayerMovedEventArgs e)
        {
            PlayerMoved?.Invoke(this, e);
        }
        public void OnBombPlaced(BombPlacedEventArgs e)
        {
            BombPlaced?.Invoke(this, e);
        }
        public void OnPlayerUpdate(PlayerUpdateEventArgs e)
        {
            PlayerUpdate?.Invoke(this, e);
        }
        public void OnMonsterMoved(MonsterMovedEventArgs e)
        {
            MonsterMoved?.Invoke(this, e);
        }
        public void OnExplosion(ExplosionEventArgs e)
        {
            Explosion?.Invoke(this, e);
        }
        public void OnExplosionClear(ExplosionEventArgs e)
        {
            ExplosionClear?.Invoke(this, e);
        }
        public void OnBarrierPlaced(BarrierPlacedEventArgs e)
        {
            BarrierPlaced?.Invoke(this, e);
        }

        #endregion
    }
}