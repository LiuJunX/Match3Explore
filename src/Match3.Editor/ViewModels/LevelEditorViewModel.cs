using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Match3.Core.Config;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Gameplay;
using Match3.Core.Scenarios;
using Match3.Editor.Interfaces;
using Match3.Editor.Logic;
using Match3.Editor.Helpers;

namespace Match3.Editor.ViewModels
{
    public enum EditorLayer
    {
        Tiles,
        Covers,
        Grounds
    }

    public class LevelEditorViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IPlatformService _platform;
        private readonly IJsonService _jsonService;
        private readonly IScenarioService _scenarioService;
        private readonly IFileSystemService _fileSystem;

        private readonly EditorSession _session;
        private readonly GridManipulator _gridManipulator;

        // --- Core State (Delegated to Session) ---
        public EditorMode CurrentMode
        {
            get => _session.CurrentMode;
            set => _session.CurrentMode = value;
        }

        public LevelConfig CurrentLevel
        {
            get => _session.CurrentLevel;
            set => _session.CurrentLevel = value;
        }
        
        public ScenarioConfig CurrentScenario
        {
            get => _session.CurrentScenario;
            set => _session.CurrentScenario = value;
        }

        public ScenarioMetadata CurrentScenarioMetadata { get; set; } = new ScenarioMetadata();

        public string ScenarioName
        {
            get => _session.ScenarioName;
            set => _session.ScenarioName = value;
        }

        public string ScenarioDescription
        {
            get => _session.ScenarioDescription;
            set => _session.ScenarioDescription = value;
        }
        
        public bool IsDirty
        {
            get => _session.IsDirty;
            set => _session.IsDirty = value;
        }

        // --- Editor UI State ---
        private EditorLayer _activeLayer = EditorLayer.Tiles;
        public EditorLayer ActiveLayer
        {
            get => _activeLayer;
            set { _activeLayer = value; OnPropertyChanged(nameof(ActiveLayer)); }
        }

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
            set 
            { 
                if (_selectedType != value)
                {
                    _selectedType = value; 
                    OnPropertyChanged(nameof(SelectedType));
                }
            }
        }

        private BombType _selectedBomb = BombType.None;
        public BombType SelectedBomb
        {
            get => _selectedBomb;
            set 
            { 
                if (_selectedBomb != value)
                {
                    _selectedBomb = value; 
                    OnPropertyChanged(nameof(SelectedBomb));
                }
            }
        }

        private GroundType _selectedGround = GroundType.None;
        public GroundType SelectedGround
        {
            get => _selectedGround;
            set { _selectedGround = value; OnPropertyChanged(nameof(SelectedGround)); }
        }

        private CoverType _selectedCover = CoverType.None;
        public CoverType SelectedCover
        {
            get => _selectedCover;
            set { _selectedCover = value; OnPropertyChanged(nameof(SelectedCover)); }
        }

        private bool _assertColor = true;
        public bool AssertColor
        {
            get => _assertColor;
            set { _assertColor = value; OnPropertyChanged(nameof(AssertColor)); }
        }

        private bool _assertBomb = true;
        public bool AssertBomb
        {
            get => _assertBomb;
            set { _assertBomb = value; OnPropertyChanged(nameof(AssertBomb)); }
        }

        private string _jsonOutput = "";
        public string JsonOutput
        {
            get => _jsonOutput;
            set { _jsonOutput = value; OnPropertyChanged(nameof(JsonOutput)); }
        }

        private bool _isAssertionMode;
        public bool IsAssertionMode
        {
            get => _isAssertionMode;
            set { _isAssertionMode = value; OnPropertyChanged(nameof(IsAssertionMode)); }
        }

        // --- Tab State ---
        private int _activeTabIndex = 0;
        public int ActiveTabIndex
        {
            get => _activeTabIndex;
            set
            {
                if (_activeTabIndex != value)
                {
                    _activeTabIndex = value;
                    OnPropertyChanged(nameof(ActiveTabIndex));
                }
            }
        }

        // --- File Browser State ---
        public ScenarioFolderNode? RootFolderNode { get; private set; }
        public List<ScenarioFileEntry> SearchResults { get; private set; } = new List<ScenarioFileEntry>();
        public string CurrentFilePath { get; set; } = "";
        public HashSet<string> ExpandedPaths { get; } = new HashSet<string>();

        public void SetRootFolder(ScenarioFolderNode root)
        {
            RootFolderNode = root;
            OnPropertyChanged(nameof(RootFolderNode));
        }

        // --- Computed Properties ---
        public LevelConfig ActiveLevelConfig => _session.ActiveLevelConfig;

        public BombType[] ActiveBombs => ActiveLevelConfig.Bombs;

        private static readonly TileType[] _tilePaletteTypes =
        {
            TileType.Red,
            TileType.Green,
            TileType.Blue,
            TileType.Yellow,
            TileType.Purple,
            TileType.Orange,
            TileType.Rainbow,
            TileType.None
        };

        private static readonly GroundType[] _groundPaletteTypes = (GroundType[])Enum.GetValues(typeof(GroundType));
        private static readonly CoverType[] _coverPaletteTypes = (CoverType[])Enum.GetValues(typeof(CoverType));
        private static readonly BombType[] _bombPaletteTypes = (BombType[])Enum.GetValues(typeof(BombType));

        public static IReadOnlyList<TileType> TilePaletteTypes => _tilePaletteTypes;
        public static IReadOnlyList<GroundType> GroundPaletteTypes => _groundPaletteTypes;
        public static IReadOnlyList<CoverType> CoverPaletteTypes => _coverPaletteTypes;
        public static IReadOnlyList<BombType> BombPaletteTypes => _bombPaletteTypes;

        public static string GetGroundName(GroundType g) => g.ToString();
        public static string GetCoverName(CoverType c) => c.ToString();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? OnRequestRepaint;

        public LevelEditorViewModel(
            IPlatformService platform,
            IJsonService jsonService,
            IScenarioService scenarioService,
            IFileSystemService fileSystem)
        {
            _platform = platform;
            _jsonService = jsonService;
            _scenarioService = scenarioService;
            _fileSystem = fileSystem;

            _session = new EditorSession();
            _gridManipulator = new GridManipulator();

            _session.PropertyChanged += OnSessionPropertyChanged;

            // Initialize default state
            _session.EnsureDefaultLevel();
            GenerateRandomLevel();
        }

        public void Dispose()
        {
            _session.PropertyChanged -= OnSessionPropertyChanged;
        }

        private void OnSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected void RequestRepaint() => OnRequestRepaint?.Invoke();

        // --- Actions ---

        public void SetScenarioName(string name)
        {
            _session.ScenarioName = name;
        }

        public void SwitchMode(EditorMode mode)
        {
            _session.CurrentMode = mode;
            if (mode == EditorMode.Scenario)
            {
                EditorWidth = _session.CurrentScenario.InitialState.Width;
                EditorHeight = _session.CurrentScenario.InitialState.Height;
                if (_session.CurrentScenario.InitialState.Grid.All(t => t == TileType.None))
                {
                    GenerateRandomLevel();
                }
            }
            else
            {
                _session.EnsureDefaultLevel();
                EditorWidth = _session.CurrentLevel.Width;
                EditorHeight = _session.CurrentLevel.Height;
            }
            IsAssertionMode = false;
        }

        public void GenerateRandomLevel()
        {
            _gridManipulator.GenerateRandomLevel(_session.ActiveLevelConfig, Environment.TickCount);
            RequestRepaint();
            _session.IsDirty = true;
        }

        public void ResizeGrid()
        {
            var newConfig = _gridManipulator.ResizeGrid(_session.ActiveLevelConfig, EditorWidth, EditorHeight);

            if (CurrentMode == EditorMode.Level)
            {
                _session.CurrentLevel = newConfig;
            }
            else
            {
                _session.CurrentScenario.InitialState = newConfig;
                _session.CurrentScenario.Operations.Clear();
                _session.CurrentScenario.ExpectedState = new LevelConfig();
            }
            RequestRepaint();
            _session.IsDirty = true;
        }

        public void ToggleAssertionMode()
        {
            IsAssertionMode = !IsAssertionMode;
        }

        public void HandleGridClick(int index)
        {
            if (IsAssertionMode)
            {
                var w = ActiveLevelConfig.Width;
                var x = index % w;
                var y = index / w;

                var existing = _session.CurrentScenario.Assertions.FirstOrDefault(a => a.X == x && a.Y == y);
                if (existing != null)
                {
                    _session.CurrentScenario.Assertions.Remove(existing);
                }
                else
                {
                    var type = AssertColor ? SelectedType : (TileType?)null;
                    var bomb = AssertBomb ? SelectedBomb : (BombType?)null;

                    _session.CurrentScenario.Assertions.Add(new ScenarioAssertion
                    {
                        X = x, Y = y,
                        Type = type,
                        Bomb = bomb
                    });
                }
                _session.IsDirty = true;
                RequestRepaint();
                return;
            }

            PaintAt(index);
        }

    public void PaintAt(int index)
    {
        switch (ActiveLayer)
        {
            case EditorLayer.Tiles:
                _gridManipulator.PaintTile(_session.ActiveLevelConfig, index, SelectedType, SelectedBomb);
                break;
            case EditorLayer.Covers:
                if (SelectedCover == CoverType.None)
                    _gridManipulator.ClearCover(_session.ActiveLevelConfig, index);
                else
                    _gridManipulator.PaintCover(_session.ActiveLevelConfig, index, SelectedCover);
                break;
            case EditorLayer.Grounds:
                if (SelectedGround == GroundType.None)
                    _gridManipulator.ClearGround(_session.ActiveLevelConfig, index);
                else
                    _gridManipulator.PaintGround(_session.ActiveLevelConfig, index, SelectedGround);
                break;
        }
        RequestRepaint();
        _session.IsDirty = true;
    }

    public void PaintTile(int index) => PaintAt(index);

    // --- IO & Export ---
        
        public void ExportJson()
        {
            if (CurrentMode == EditorMode.Level)
                JsonOutput = _jsonService.Serialize(_session.CurrentLevel);
            else
                JsonOutput = _jsonService.Serialize(_session.CurrentScenario);
        }

        public void ImportJson(bool keepScenarioMode = false)
        {
            if (string.IsNullOrWhiteSpace(JsonOutput)) return;
            try
            {
                if (JsonOutput.Contains("Operations"))
                {
                    _session.CurrentScenario = _jsonService.Deserialize<ScenarioConfig>(JsonOutput);
                    _session.CurrentMode = EditorMode.Scenario;
                    EditorWidth = _session.CurrentScenario.InitialState.Width;
                    EditorHeight = _session.CurrentScenario.InitialState.Height;
                }
                else
                {
                    var level = _jsonService.Deserialize<LevelConfig>(JsonOutput);

                    if (keepScenarioMode || CurrentMode == EditorMode.Scenario)
                    {
                        _session.CurrentScenario = new ScenarioConfig 
                        { 
                            InitialState = level,
                            Operations = new List<MoveOperation>() 
                        };
                        _session.CurrentMode = EditorMode.Scenario;
                        EditorWidth = level.Width;
                        EditorHeight = level.Height;
                    }
                    else
                    {
                        _session.CurrentLevel = level;
                        _session.CurrentMode = EditorMode.Level;
                        EditorWidth = _session.CurrentLevel.Width;
                        EditorHeight = _session.CurrentLevel.Height;
                    }
                }
                
                _session.EnsureDefaultLevel();
                RequestRepaint();
                _session.IsDirty = false;
            }
            catch (Exception ex)
            {
                _ = _platform.ShowAlertAsync($"Import Error: {ex.Message}");
            }
        }

        // --- Scenario Management ---

        public void RefreshScenarioList()
        {
            RootFolderNode = _scenarioService.BuildTree();
        }

        public async Task LoadScenarioAsync(string path)
        {
            if (IsDirty)
            {
                var confirm = await _platform.ConfirmAsync("Unsaved Changes", "You have unsaved changes. Do you want to save them before switching?");
                if (confirm)
                {
                    if (!string.IsNullOrEmpty(CurrentFilePath))
                    {
                        await SaveScenarioAsync();
                    }
                    else
                    {
                        var discard = await _platform.ConfirmAsync("Cannot Save", "File has no path. Discard changes?");
                        if (!discard) return;
                    }
                }
                else
                {
                    var discard = await _platform.ConfirmAsync("Discard Changes?", "Are you sure you want to discard unsaved changes?");
                    if (!discard) return;
                }
            }

            try
            {
                var json = _scenarioService.ReadScenarioJson(path);
                JsonOutput = json;
                ImportJson(keepScenarioMode: true);
                CurrentFilePath = path;
                SetScenarioName(_fileSystem.GetFileNameWithoutExtension(path));
            }
            catch(Exception ex)
            {
                await _platform.ShowAlertAsync("Error", "Failed to load file: " + ex.Message);
            }
        }

        public async Task SaveScenarioAsync()
        {
            if (string.IsNullOrEmpty(CurrentFilePath)) return;

            try
            {
                var currentName = _fileSystem.GetFileNameWithoutExtension(CurrentFilePath);
                if (!string.Equals(currentName, ScenarioName, StringComparison.Ordinal))
                {
                     _scenarioService.RenameScenario(CurrentFilePath, ScenarioName);

                     var stem = ScenarioFileName.SanitizeFileStem(ScenarioName);
                     SetScenarioName(stem);

                     var dir = _fileSystem.GetDirectoryName(CurrentFilePath);
                     var newPath = string.IsNullOrEmpty(dir)
                         ? stem + ".json"
                         : _fileSystem.CombinePath(dir, stem + ".json");
                     CurrentFilePath = _fileSystem.NormalizePath(newPath);
                }

                ExportJson();
                _scenarioService.WriteScenarioJson(CurrentFilePath, JsonOutput);
                _session.IsDirty = false;
                RefreshScenarioList();
            }
            catch (Exception ex)
            {
                await _platform.ShowAlertAsync("Error", "Failed to save: " + ex.Message);
            }
        }

        public async Task CreateNewScenarioAsync(string folderPath)
        {
            try 
            {
                var newPath = _scenarioService.CreateNewScenario(folderPath, "New Scenario", "{}");
                RefreshScenarioList();
            }
            catch(Exception ex) 
            { 
                await _platform.ShowAlertAsync("Error", "Failed to create scenario: " + ex.Message);
            }
        }

        public async Task CreateNewFolderAsync(string parentPath)
        {
            try 
            {
                _scenarioService.CreateFolder(parentPath, "New Folder");
                RefreshScenarioList();
            }
            catch(Exception ex) 
            { 
                await _platform.ShowAlertAsync("Error", "Failed to create folder: " + ex.Message);
            }
        }

        public async Task DuplicateScenarioAsync(string path)
        {
            try
            {
                _scenarioService.DuplicateScenario(path, _fileSystem.GetFileNameWithoutExtension(path) + "_Copy");
                RefreshScenarioList();
            }
            catch(Exception ex) 
            { 
                await _platform.ShowAlertAsync("Error", "Failed to duplicate: " + ex.Message);
            }
        }

        public async Task DeleteFileAsync(string path, bool isFolder)
        {
            var confirm = await _platform.ConfirmAsync("Delete", "Are you sure you want to delete '" + _fileSystem.GetFileName(path) + "'?");
            if (!confirm) return;

            try
            {
                if (isFolder)
                {
                    _scenarioService.DeleteFolder(path);
                }
                else
                {
                    _scenarioService.DeleteScenario(path);
                }
                RefreshScenarioList();
            }
            catch(Exception ex)
            {
                await _platform.ShowAlertAsync("Error", "Failed to delete: " + ex.Message);
            }
        }
    }
}
