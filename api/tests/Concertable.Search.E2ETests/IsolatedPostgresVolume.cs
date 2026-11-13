using System.Diagnostics;

namespace Concertable.Search.E2ETests;

internal sealed class IsolatedPostgresVolume
{
    private const string OwnershipLabel = "com.concertable.search.e2e-run";
    private readonly string runId;

    public IsolatedPostgresVolume(string runId)
    {
        this.runId = runId;
        Name = $"concertable-search-e2e-{runId}";
    }

    public string Name { get; }

    public async Task CreateAsync(CancellationToken cancellationToken)
    {
        var create = await ExecuteAsync(
            ["volume", "create", "--label", $"{OwnershipLabel}={runId}", Name],
            cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(create, "create the isolated PostgreSQL volume");

        var owner = await ExecuteAsync(
            ["volume", "inspect", "--format", $"{{{{ index .Labels \"{OwnershipLabel}\" }}}}", Name],
            cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(owner, "verify isolated PostgreSQL volume ownership");
        if (!string.Equals(owner.Output.Trim(), runId, StringComparison.Ordinal))
            throw new InvalidOperationException("The isolated PostgreSQL volume ownership label did not match this test run.");
    }

    public async Task AssertContainerRemovedAsync(string containerId, CancellationToken cancellationToken)
    {
        var containers = await ExecuteAsync(
            ["ps", "--all", "--filter", $"id={containerId}", "--format", "{{.ID}}"],
            cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(containers, "verify removal of the first PostgreSQL container");
        if (!string.IsNullOrWhiteSpace(containers.Output))
            throw new InvalidOperationException("The first PostgreSQL container still exists after the AppHost was disposed.");
    }

    public async Task RemoveIfUnusedAsync(CancellationToken cancellationToken)
    {
        var owner = await ExecuteAsync(
            ["volume", "inspect", "--format", $"{{{{ index .Labels \"{OwnershipLabel}\" }}}}", Name],
            cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(owner, "verify isolated PostgreSQL volume ownership before cleanup");
        if (!string.Equals(owner.Output.Trim(), runId, StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to remove a volume not owned by this test run.");

        var references = await ExecuteAsync(
            ["ps", "--all", "--filter", $"volume={Name}", "--format", "{{.ID}}"],
            cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(references, "check isolated PostgreSQL volume references");
        if (!string.IsNullOrWhiteSpace(references.Output))
            throw new InvalidOperationException("Refusing to remove an isolated PostgreSQL volume that is still referenced by a container.");

        var remove = await ExecuteAsync(["volume", "rm", Name], cancellationToken).ConfigureAwait(false);
        EnsureSucceeded(remove, "remove the isolated PostgreSQL volume");
    }

    private static async Task<CommandResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo.FileName = "docker";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        Task<string> output;
        Task<string> error;
        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Docker did not start the requested command.");
            output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            error = process.StandardError.ReadToEndAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException("Docker is unavailable for the isolated PostgreSQL lifecycle test.", exception);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            if (!cancellationToken.IsCancellationRequested)
                throw new TimeoutException("Docker did not complete the isolated PostgreSQL lifecycle command within 20 seconds.");

            throw;
        }

        await Task.WhenAll(output, error).ConfigureAwait(false);
        return new CommandResult(process.ExitCode, output.Result, error.Result);
    }

    private static void EnsureSucceeded(CommandResult result, string operation)
    {
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Docker could not {operation} (exit code {result.ExitCode}).");
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
    }

    private sealed record CommandResult(int ExitCode, string Output, string Error);
}
