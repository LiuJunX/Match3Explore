using System;
using System.Threading;
using System.Threading.Tasks;
using Match3.Core.Commands;
using Match3.Core.Models.Grid;
using Match3.Core.Simulation;
using Xunit;

namespace Match3.Core.Tests.Commands;

public class CommandHistoryTests
{
    #region Helper: Stub IGameCommand

    private sealed record StubCommand : IGameCommand
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public int IssuedAtTick { get; init; }
        public bool Execute(SimulationEngine engine) => true;
        public bool CanExecute(in GameState state) => true;
    }

    #endregion

    #region Record and Retrieve

    [Fact]
    public void Record_SingleCommand_CountIsOne()
    {
        var history = new CommandHistory();
        var cmd = new StubCommand();

        history.Record(cmd);

        Assert.Equal(1, history.Count);
    }

    [Fact]
    public void Record_MultipleCommands_CountMatchesRecorded()
    {
        var history = new CommandHistory();

        history.Record(new StubCommand { IssuedAtTick = 0 });
        history.Record(new StubCommand { IssuedAtTick = 1 });
        history.Record(new StubCommand { IssuedAtTick = 2 });

        Assert.Equal(3, history.Count);
    }

    [Fact]
    public void Record_NullCommand_IsIgnored()
    {
        var history = new CommandHistory();

        history.Record(null!);

        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void Record_PreservesOrder()
    {
        var history = new CommandHistory();
        var cmd0 = new StubCommand { IssuedAtTick = 10 };
        var cmd1 = new StubCommand { IssuedAtTick = 20 };
        var cmd2 = new StubCommand { IssuedAtTick = 30 };

        history.Record(cmd0);
        history.Record(cmd1);
        history.Record(cmd2);

        var commands = history.GetCommands();
        Assert.Equal(10, commands[0].IssuedAtTick);
        Assert.Equal(20, commands[1].IssuedAtTick);
        Assert.Equal(30, commands[2].IssuedAtTick);
    }

    #endregion

    #region GetCommands Returns Independent Copy

    [Fact]
    public void GetCommands_ReturnsAllRecordedCommands()
    {
        var history = new CommandHistory();
        var cmd1 = new StubCommand();
        var cmd2 = new StubCommand();

        history.Record(cmd1);
        history.Record(cmd2);

        var commands = history.GetCommands();

        Assert.Equal(2, commands.Count);
        Assert.Same(cmd1, commands[0]);
        Assert.Same(cmd2, commands[1]);
    }

    [Fact]
    public void GetCommands_ReturnsIndependentCopy()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());

        var snapshot1 = history.GetCommands();

        // Record another command after snapshot
        history.Record(new StubCommand());

        var snapshot2 = history.GetCommands();

        // First snapshot should not be affected
        Assert.Single(snapshot1);
        Assert.Equal(2, snapshot2.Count);
    }

    [Fact]
    public void GetCommands_EmptyHistory_ReturnsEmptyList()
    {
        var history = new CommandHistory();

        var commands = history.GetCommands();

        Assert.Empty(commands);
    }

    #endregion

    #region GetCommand with Valid and Invalid Index

    [Fact]
    public void GetCommand_ValidIndex_ReturnsCommand()
    {
        var history = new CommandHistory();
        var cmd = new StubCommand { IssuedAtTick = 42 };
        history.Record(cmd);

        var result = history.GetCommand(0);

        Assert.NotNull(result);
        Assert.Same(cmd, result);
    }

    [Fact]
    public void GetCommand_LastIndex_ReturnsLastCommand()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand { IssuedAtTick = 1 });
        history.Record(new StubCommand { IssuedAtTick = 2 });
        var lastCmd = new StubCommand { IssuedAtTick = 3 };
        history.Record(lastCmd);

        var result = history.GetCommand(2);

        Assert.NotNull(result);
        Assert.Same(lastCmd, result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetCommand_NegativeIndex_ReturnsNull(int index)
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());

        var result = history.GetCommand(index);

        Assert.Null(result);
    }

    [Fact]
    public void GetCommand_IndexEqualsCount_ReturnsNull()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());

        var result = history.GetCommand(1);

        Assert.Null(result);
    }

    [Fact]
    public void GetCommand_IndexBeyondCount_ReturnsNull()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());

        var result = history.GetCommand(999);

        Assert.Null(result);
    }

    [Fact]
    public void GetCommand_EmptyHistory_ReturnsNull()
    {
        var history = new CommandHistory();

        var result = history.GetCommand(0);

        Assert.Null(result);
    }

    #endregion

    #region Clear Resets State

    [Fact]
    public void Clear_ResetsCountToZero()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());
        history.Record(new StubCommand());

        history.Clear();

        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void Clear_GetCommandsReturnsEmpty()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());

        history.Clear();

        var commands = history.GetCommands();
        Assert.Empty(commands);
    }

    [Fact]
    public void Clear_GetCommandReturnsNull()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());

        history.Clear();

        Assert.Null(history.GetCommand(0));
    }

    [Fact]
    public void Clear_CanRecordAfterClear()
    {
        var history = new CommandHistory();
        history.Record(new StubCommand());
        history.Clear();

        var newCmd = new StubCommand { IssuedAtTick = 99 };
        history.Record(newCmd);

        Assert.Equal(1, history.Count);
        Assert.Same(newCmd, history.GetCommand(0));
    }

    #endregion

    #region IsRecording Property Behavior

    [Fact]
    public void IsRecording_DefaultsToTrue()
    {
        var history = new CommandHistory();

        Assert.True(history.IsRecording);
    }

    [Fact]
    public void IsRecording_WhenFalse_RecordIsIgnored()
    {
        var history = new CommandHistory();
        history.IsRecording = false;

        history.Record(new StubCommand());

        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void IsRecording_WhenReEnabled_RecordWorks()
    {
        var history = new CommandHistory();
        history.IsRecording = false;
        history.Record(new StubCommand()); // ignored

        history.IsRecording = true;
        history.Record(new StubCommand()); // recorded

        Assert.Equal(1, history.Count);
    }

    [Fact]
    public void IsRecording_CanBeToggledMultipleTimes()
    {
        var history = new CommandHistory();

        history.IsRecording = false;
        history.Record(new StubCommand()); // ignored
        Assert.Equal(0, history.Count);

        history.IsRecording = true;
        history.Record(new StubCommand()); // recorded
        Assert.Equal(1, history.Count);

        history.IsRecording = false;
        history.Record(new StubCommand()); // ignored
        Assert.Equal(1, history.Count);

        history.IsRecording = true;
        history.Record(new StubCommand()); // recorded
        Assert.Equal(2, history.Count);
    }

    #endregion

    #region Count Property

    [Fact]
    public void Count_InitiallyZero()
    {
        var history = new CommandHistory();

        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void Count_IncrementsOnRecord()
    {
        var history = new CommandHistory();

        for (int i = 0; i < 5; i++)
        {
            history.Record(new StubCommand());
            Assert.Equal(i + 1, history.Count);
        }
    }

    [Fact]
    public void Count_DoesNotIncrementWhenNotRecording()
    {
        var history = new CommandHistory();
        history.IsRecording = false;

        history.Record(new StubCommand());

        Assert.Equal(0, history.Count);
    }

    #endregion

    #region Thread Safety

    [Fact]
    public async Task ConcurrentRecording_DoesNotLoseCommands()
    {
        var history = new CommandHistory();
        const int threadCount = 4;
        const int commandsPerThread = 100;

        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < commandsPerThread; i++)
                {
                    history.Record(new StubCommand());
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(threadCount * commandsPerThread, history.Count);
    }

    #endregion
}
