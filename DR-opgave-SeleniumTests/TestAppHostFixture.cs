namespace DR_opgave_SeleniumTests;

using System.Diagnostics;
using System.Net.Sockets;

public sealed class TestAppHostFixture : IAsyncLifetime
{
    private const string FrontendUrl = "http://127.0.0.1:5173";
    private const string BackendUrl = "http://127.0.0.1:5249";

    private Process? _frontendProcess;
    private Process? _backendProcess;
    private bool _startedFrontend;
    private bool _startedBackend;

    public async Task InitializeAsync()
    {
        var workspaceRoot = ResolveWorkspaceRoot();
        var backendDir = Path.Combine(workspaceRoot, "DR-opgave-Backend", "DR-Repo");
        var frontendDir = Path.Combine(workspaceRoot, "DR-opgave-Frontend", "DR-Repo-Frontend");

        if (!Directory.Exists(backendDir) || !Directory.Exists(frontendDir))
        {
            throw new DirectoryNotFoundException("Could not locate backend/frontend folders from the Selenium test project.");
        }

        if (!IsPortOpen("127.0.0.1", 5249))
        {
            _backendProcess = StartShellCommand(
                "dotnet run --urls http://127.0.0.1:5249",
                backendDir);
            _startedBackend = true;
        }

        await WaitForHttpAsync($"{BackendUrl}/api/v2/records", timeout: TimeSpan.FromSeconds(60));

        if (!IsPortOpen("127.0.0.1", 5173))
        {
            _frontendProcess = StartShellCommand(
                "npm run dev -- --host 127.0.0.1 --port 5173 --strictPort",
                frontendDir);
            _startedFrontend = true;
        }

        await WaitForHttpAsync(FrontendUrl, timeout: TimeSpan.FromSeconds(60));
    }

    public Task DisposeAsync()
    {
        if (_startedFrontend)
        {
            TryKill(_frontendProcess);
        }

        if (_startedBackend)
        {
            TryKill(_backendProcess);
        }

        return Task.CompletedTask;
    }

    private static Process StartShellCommand(string command, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/zsh",
            Arguments = $"-lc \"{command}\"",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
        };

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process: {command}");

        return process;
    }

    private static void TryKill(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
        finally
        {
            process.Dispose();
        }
    }

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var completed = connectTask.Wait(TimeSpan.FromMilliseconds(500));
            return completed && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForHttpAsync(string url, TimeSpan timeout)
    {
        using var http = new HttpClient();
        var startedAt = DateTime.UtcNow;

        while (DateTime.UtcNow - startedAt < timeout)
        {
            try
            {
                using var response = await http.GetAsync(url);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch
            {
                // Keep retrying until timeout.
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Timed out waiting for service at {url}");
    }

    private static string ResolveWorkspaceRoot()
    {
        var startPoints = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
        };

        foreach (var start in startPoints)
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                var backend = Path.Combine(dir.FullName, "DR-opgave-Backend", "DR-Repo");
                var frontend = Path.Combine(dir.FullName, "DR-opgave-Frontend", "DR-Repo-Frontend");
                if (Directory.Exists(backend) && Directory.Exists(frontend))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }
        }

        throw new DirectoryNotFoundException("Unable to resolve DR-opgave workspace root.");
    }
}
