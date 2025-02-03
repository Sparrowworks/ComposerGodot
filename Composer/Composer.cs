using Godot;
using System.Threading.Tasks;
using Array = Godot.Collections.Array;

public partial class Composer : Node
{
    [Signal]
    public delegate void FinishedInitializingEventHandler();

    [Signal]
    public delegate void InvalidSceneEventHandler(string path);

    [Signal]
    public delegate void FailedLoadingEventHandler(string path);

    [Signal]
    public delegate void UpdatedLoadingEventHandler(string path, int progress);

    [Signal]
    public delegate void FinishedLoadingEventHandler(Node scene);

    [Signal]
    public delegate void LoadingActivatedEventHandler();

    private bool _hasInitialized = false;
    public bool HasInitialized {
        get => _hasInitialized;
        private set
        {
            _hasInitialized = value;
            if (_hasInitialized)
                EmitSignal(SignalName.FinishedInitializing);
        }
    }

    public bool IsUsingSubthreads {get; set;} = false;
    public ResourceLoader.CacheMode CacheMode {get; set;} = ResourceLoader.CacheMode.Reuse;

    private float _loadingTimerDelay = 0.1f;
    public float LoadingTimerDelay {
        get => _loadingTimerDelay;
        set
        {
            _loadingTimerDelay = value;
            if (LoadingTimer != null)
                LoadingTimer.WaitTime = _loadingTimerDelay;
        }
    }

    public Node Root {get; set;}

    private bool IsLoading = false;
    private bool HasLoadingScreen = false;
    private Timer LoadingTimer;
    private string CurrentLoadingPath = "";
    private Node CurrentLoadScreen = null;
    private Node CurrentScene = null;

    public override void _EnterTree()
    {
        InvalidScene += OnInvalidScene;
        FailedLoading += OnFailedLoading;
        FinishedLoading += OnFinishedLoading;

        Root = GetTree().Root;
        SetupTimer();
    }

    public async void LoadScene(string path)
    {
        if (IsLoading)
            return;

        if (!HasInitialized)
            await ToSignal(this, SignalName.FinishedInitializing);

        Error loader = ResourceLoader.LoadThreadedRequest(path, "", IsUsingSubthreads, CacheMode);
        if (!ResourceLoader.Exists(path) || loader != Error.Ok)
        {
            EmitSignal(SignalName.InvalidScene, path);
            return;
        }

        IsLoading = true;

        if (CurrentScene != null)
        {
            CurrentScene.QueueFree();
            CurrentScene = null;
        }

        CurrentLoadingPath = path;
        LoadingTimer.Start();
    }

    public Node SetupLoadScreen(string path)
    {
        if (HasLoadingScreen)
            return CurrentLoadScreen;

        HasLoadingScreen = true;
        CurrentLoadScreen = GD.Load<PackedScene>(path).Instantiate();

        Root.CallDeferred(MethodName.AddChild, CurrentLoadScreen);
        Root.CallDeferred(MethodName.MoveChild, CurrentLoadScreen, Root.GetChildCount()-1);

        return CurrentLoadScreen;
    }

    public void ClearLoadScreen()
    {
        CurrentLoadScreen.QueueFree();
        CurrentLoadScreen = null;
        HasLoadingScreen = false;
    }

    private void CheckLoadingStatus()
    {
        Array loadProgress = new();
        ResourceLoader.ThreadLoadStatus LoadStatus = ResourceLoader.LoadThreadedGetStatus(CurrentLoadingPath, loadProgress);

        switch (LoadStatus)
        {
            case ResourceLoader.ThreadLoadStatus.InvalidResource:
            {
                EmitSignal(SignalName.InvalidScene, CurrentLoadingPath);
                LoadingTimer.Stop();
                break;
            }
            case ResourceLoader.ThreadLoadStatus.Failed:
            {
                EmitSignal(SignalName.FailedLoading, CurrentLoadingPath);
                LoadingTimer.Stop();
                break;
            }
            case ResourceLoader.ThreadLoadStatus.InProgress:
            {
                EmitSignal(SignalName.UpdatedLoading, (int)loadProgress[0] * 100);
                break;
            }
            case ResourceLoader.ThreadLoadStatus.Loaded:
            {
                LoadingTimer.Stop();
                EmitSignal(SignalName.FinishedLoading, ((PackedScene)ResourceLoader.LoadThreadedGet(CurrentLoadingPath)).Instantiate());
                break;
            }
        }
    }

    private async Task SetupTimer()
    {
        LoadingTimer = new();
        LoadingTimer.Name = "LoadingTimer";
        LoadingTimer.WaitTime = LoadingTimerDelay;
        LoadingTimer.Timeout += CheckLoadingStatus;
        Root.CallDeferred(MethodName.AddChild, LoadingTimer);

        await ToSignal(LoadingTimer, Timer.SignalName.Ready);

        HasInitialized = true;
    }

    private void OnFinishedLoading(Node scene)
    {
        Root.CallDeferred(MethodName.AddChild, scene);
        CurrentScene = scene;

        CurrentLoadingPath = "";
        IsLoading = false;
    }

    private void OnInvalidScene(string path)
    {
        GD.PrintErr($"Invalid resource: {path}");
    }

    private void OnFailedLoading(string path)
    {
        GD.PrintErr($"Failed to load resource: {path}");
    }
}

