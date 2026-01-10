using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Match3.Core;
using Match3.Core.Config;
using Match3.Core.Interfaces;
using Match3.Core.Logic;
using Match3.Core.Scenarios;
using Match3.Core.Structs;
using Match3.Core.Systems;
using Match3.Editor.Interfaces;
using Match3.Random;

namespace Match3.Editor.ViewModels
{
    public class LevelEditorViewModel : INotifyPropertyChanged
    {
        private readonly IPlatformService _platform;
        private readonly IFileSystemService _fileSystem;
        private readonly IJsonService _jsonService;
        private readonly IGameLogger _logger;

        // --- Core State ---
        public enum EditorMode { Level, Scenario }
        private EditorMode _currentMode = EditorMode.Level;
        public EditorMode CurrentMode
        {
            get => _currentMode;
            set { _currentMode = value; OnPropertyChanged(nameof(CurrentMode)); OnPropertyChanged(nameof(ActiveLevelConfig)); }
        }

        // --- Data Models ---
        public LevelConfig CurrentLevel { get; set; } = new LevelConfig();
        public ScenarioConfig CurrentScenario { get; set; } = new ScenarioConfig();
        public ScenarioMetadata CurrentScenarioMetadata { get; set; } = new ScenarioMetadata();
        
        // --- Editor UI State ---
        private int _editorWidth = 8;
        public int EditorWidth 
        { 
            get => _editorWidth; 
            set { _editorWidth = value; OnPropertyChanged(nameof(EditorWidth)); } 
        }

        private int _editorHeight = 8;
        public int EditorHeight 
        { 
            get => _editorHeight; 
            set { _editorHeight = value; OnPropertyChanged(nameof(EditorHeight)); } 
        }

        private TileType _selectedType = TileType.Red;
        public TileType SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(nameof(SelectedType)); }
        }

        private BombType _selectedBomb = BombType.None;
        public BombType SelectedBomb
        {
            get => _selectedBomb;
            set { _selectedBomb = value; OnPropertyChanged(nameof(SelectedBomb)); }
        }

        private string _jsonOutput = "";
        public string JsonOutput
        {
            get => _jsonOutput;
            set { _jsonOutput = value; OnPropertyChanged(nameof(JsonOutput)); }
        }

        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            set { _isRecording = value; OnPropertyChanged(nameof(IsRecording)); }
        }

        private bool _isAssertionMode;
        public bool IsAssertionMode
        {
            get => _isAssertionMode;
            set { _isAssertionMode = value; OnPropertyChanged(nameof(IsAssertionMode)); }
        }

        // --- File Browser State ---
        public ScenarioFolderNode RootFolderNode { get; private set; }
        public List<ScenarioFileEntry> SearchResults { get; private set; } = new List<ScenarioFileEntry>();
        public string CurrentFilePath { get; set; } = "";
        public HashSet<string> ExpandedPaths { get; } = new HashSet<string>();

        // --- Computed Properties ---
        public LevelConfig ActiveLevelConfig => CurrentMode == EditorMode.Level ? CurrentLevel : CurrentScenario.InitialState;
        public BombType[] LevelBombs { get; set; } = new BombType[8 * 8];
        public BombType[] ScenarioBombs { get; set; } = new BombType[8 * 8];
        public BombType[] ActiveBombs => CurrentMode == EditorMode.Level ? LevelBombs : ScenarioBombs;

        // --- Simulation ---
        public Match3Controller SimulationController { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action OnRequestRepaint;

        public LevelEditorViewModel(
            IPlatformService platform, 
            IFileSystemService fileSystem, 
            IJsonService jsonService,
            IGameLogger logger)
        {
            _platform = platform;
            _fileSystem = fileSystem;
            _jsonService = jsonService;
            _logger = logger;
            
            EnsureDefaultLevel();
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected void RequestRepaint() => OnRequestRepaint?.Invoke();

        // --- Actions ---

        public void SwitchMode(EditorMode mode)
        {
            CurrentMode = mode;
            if (mode == EditorMode.Scenario)
            {
                EditorWidth = CurrentScenario.InitialState.Width;
                EditorHeight = CurrentScenario.InitialState.Height;
                if (CurrentScenario.InitialState.Grid.All(t => t == TileType.None))
                {
                    GenerateRandomLevel();
                }
            }
            else
            {
                EnsureDefaultLevel();
                EditorWidth = CurrentLevel.Width;
                EditorHeight = CurrentLevel.Height;
            }
            IsRecording = false;
            SimulationController = null;
        }

        public void EnsureDefaultLevel()
        {
            if (CurrentLevel.Grid == null || CurrentLevel.Grid.Length == 0)
            {
                CurrentLevel = new LevelConfig(8, 8);
                LevelBombs = new BombType[64];
                GenerateRandomLevel();
            }
        }

        public void GenerateRandomLevel()
        {
            var config = ActiveLevelConfig;
            var rng = new SeedManager(Environment.TickCount).GetRandom(RandomDomain.Refill);
            var types = new[] { TileType.Red, TileType.Green, TileType.Blue, TileType.Yellow, TileType.Purple, TileType.Orange };

            for (int i = 0; i < config.Grid.Length; i++)
            {
                config.Grid[i] = types[rng.Next(0, types.Length)];
            }
            
            if (CurrentMode == EditorMode.Level) Array.Clear(LevelBombs, 0, LevelBombs.Length);
            else Array.Clear(ScenarioBombs, 0, ScenarioBombs.Length);
            
            RequestRepaint();
        }

        public void ResizeGrid()
        {
            var oldConfig = ActiveLevelConfig;
            var oldBombs = ActiveBombs;
            
            var newConfig = new LevelConfig(EditorWidth, EditorHeight);
            var newBombs = new BombType[EditorWidth * EditorHeight];

            // Copy logic
            int w = Math.Min(oldConfig.Width, newConfig.Width);
            int h = Math.Min(oldConfig.Height, newConfig.Height);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int oldIdx = y * oldConfig.Width + x;
                    int newIdx = y * newConfig.Width + x;
                    if (oldIdx < oldConfig.Grid.Length && newIdx < newConfig.Grid.Length)
                    {
                        newConfig.Grid[newIdx] = oldConfig.Grid[oldIdx];
                        newBombs[newIdx] = oldBombs[oldIdx];
                    }
                }
            }

            if (CurrentMode == EditorMode.Level)
            {
                newConfig.MoveLimit = CurrentLevel.MoveLimit;
                CurrentLevel = newConfig;
                LevelBombs = newBombs;
            }
            else
            {
                CurrentScenario.InitialState = newConfig;
                ScenarioBombs = newBombs;
                CurrentScenario.Operations.Clear();
                CurrentScenario.ExpectedState = new LevelConfig();
            }
            RequestRepaint();
        }

        public void HandleGridClick(int index)
        {
            if (IsAssertionMode)
            {
                // Assertion logic (simplified for now)
                return;
            }

            if (IsRecording && SimulationController != null)
            {
                var w = ActiveLevelConfig.Width;
                SimulationController.OnTap(new Position(index % w, index / w));
            }
            else
            {
                PaintTile(index);
            }
        }

        public void PaintTile(int index)
        {
            if (index < 0 || index >= ActiveLevelConfig.Grid.Length) return;
            
            if (SelectedBomb == BombType.Color)
                ActiveLevelConfig.Grid[index] = TileType.Rainbow;
            else
                ActiveLevelConfig.Grid[index] = SelectedType;

            ActiveBombs[index] = SelectedBomb;
            RequestRepaint();
        }

        // --- IO & Export ---
        
        public void ExportJson()
        {
            if (CurrentMode == EditorMode.Level)
                JsonOutput = _jsonService.Serialize(CurrentLevel);
            else
                JsonOutput = _jsonService.Serialize(CurrentScenario);
        }

        public void ImportJson()
        {
            if (string.IsNullOrWhiteSpace(JsonOutput)) return;
            try
            {
                if (JsonOutput.Contains("Operations"))
                {
                    CurrentScenario = _jsonService.Deserialize<ScenarioConfig>(JsonOutput);
                    CurrentMode = EditorMode.Scenario;
                    EditorWidth = CurrentScenario.InitialState.Width;
                    EditorHeight = CurrentScenario.InitialState.Height;
                }
                else
                {
                    CurrentLevel = _jsonService.Deserialize<LevelConfig>(JsonOutput);
                    CurrentMode = EditorMode.Level;
                    EditorWidth = CurrentLevel.Width;
                    EditorHeight = CurrentLevel.Height;
                }
                RequestRepaint();
            }
            catch (Exception ex)
            {
                _ = _platform.ShowAlertAsync($"Import Error: {ex.Message}");
            }
        }

        // --- Simulation ---

        public void StartRecording()
        {
            IsRecording = true;
            CurrentScenario.Operations.Clear();
            
            var seed = CurrentScenario.Seed;
            var seedManager = new SeedManager(seed);
            
            // Create Simulation Components
            var view = new EditorGameView(this);
            var config = new Match3Config(ActiveLevelConfig.Width, ActiveLevelConfig.Height, 6);
            
            SimulationController = new Match3Controller(
                config, 
                seedManager.GetRandom(RandomDomain.Main),
                view,
                new ClassicMatchFinder(),
                new StandardMatchProcessor(new StandardScoreSystem()),
                new StandardGravitySystem(new StandardTileGenerator(seedManager.GetRandom(RandomDomain.Refill))),
                new PowerUpHandler(new StandardScoreSystem()),
                new StandardTileGenerator(seedManager.GetRandom(RandomDomain.Refill)),
                _logger,
                new StandardScoreSystem(),
                new StandardInputSystem(),
                ActiveLevelConfig
            );
            
            // Apply Bombs to Sim State
            ApplyBombsToSimulation();
        }

        public void StopRecording()
        {
            IsRecording = false;
            // SimulationController stays alive for inspection
        }

        public void UpdateSimulation(float dt)
        {
            if (SimulationController != null)
            {
                SimulationController.Update(dt);
                RequestRepaint();
            }
        }
        
        private void ApplyBombsToSimulation()
        {
            var bombs = ActiveBombs;
            for(int i=0; i<bombs.Length; i++)
            {
                if (bombs[i] != BombType.None)
                {
                    var x = i % ActiveLevelConfig.Width;
                    var y = i / ActiveLevelConfig.Width;
                    var type = SimulationController.State.GetTile(x, y).Type;
                    if (bombs[i] == BombType.Color) type = TileType.Rainbow;
                    SimulationController.SetTileWithBomb(x, y, type, bombs[i]);
                }
            }
        }

        public void RecordMove(Position a, Position b)
        {
            CurrentScenario.Operations.Add(new MoveOperation(a.X, a.Y, b.X, b.Y));
        }

        private class EditorGameView : IGameView
        {
            private readonly LevelEditorViewModel _vm;
            public EditorGameView(LevelEditorViewModel vm) => _vm = vm;
            public void RenderBoard(TileType[,] board) { _vm.RequestRepaint(); }
            public void ShowSwap(Position a, Position b, bool success) 
            {
                if(success) _vm.RecordMove(a, b);
                _vm.RequestRepaint();
            }
            public void ShowMatches(IReadOnlyCollection<Position> matched) { }
            public void ShowGravity(IEnumerable<TileMove> moves) { }
            public void ShowRefill(IEnumerable<TileMove> moves) { }
        }
    }
}
