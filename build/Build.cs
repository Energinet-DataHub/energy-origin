using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
[GitHubActions(
    "ci",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["main"],
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(Format), nameof(Test), nameof(Publish), nameof(DockerBuild)],
    ImportSecrets = ["DOCKER_USERNAME", "DOCKER_PASSWORD"])]

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => d => d
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => d => d
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => d => d
        .DependsOn(Restore)
        .Executes(() =>
        {
            var projects = GetChangedProjects();

            foreach (var project in projects)
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            }
        });

    Target Format => d => d
        .Executes(() =>
        {
            DotNetFormat(s => s
                .SetProject(Solution)
                .EnableNoRestore());
        });

    Target Test => d => d
        .DependsOn(Compile)
        .Executes(() =>
        {
            var projects = GetChangedProjects();

            foreach (var project in projects)
            {
                DotNetTest(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .SetCoverletOutput(ArtifactsDirectory / "coverage" / $"{project.Name}.xml")
                    .SetCoverletOutputFormat(CoverletOutputFormat.cobertura));
            }
        });

    Target Publish => d => d
        .DependsOn(Test)
        .Executes(() =>
        {
            var projects = GetChangedProjects();

            foreach (var project in projects)
            {
                DotNetPublish(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutput(ArtifactsDirectory / project.Name)
                    .SetRuntime("linux-x64"));
            }
        });

    Target DockerBuild => d => d
        .DependsOn(Publish)
        .Executes(() =>
        {
            var globalJsonPath = RootDirectory / "global.json";
            var globalJsonContent = File.ReadAllText(globalJsonPath);
            var globalJson = JsonDocument.Parse(globalJsonContent);

            var sdkVersion = globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString();
            var runtimeVersion = globalJson.RootElement.GetProperty("runtime").GetProperty("version").GetString();

            if (string.IsNullOrEmpty(sdkVersion) || string.IsNullOrEmpty(runtimeVersion))
            {
                throw new Exception("SDK or runtime version not found in global.json");
            }

            var projects = GetChangedProjects();
            foreach (var project in projects)
            {
                var imageName = $"{project.Name.ToLower()}-{GitRepository.Commit?[..7]}";
                var dockerFilePath = RootDirectory / "Dockerfile";
                var buildContext = RootDirectory;

                DockerTasks.DockerBuild(s => s
                    .SetFile(dockerFilePath)
                    .SetTag(imageName)
                    .SetPath(buildContext)
                    .SetBuildArg("SDK_VERSION", "8.0.204")
                    .SetBuildArg("RUNTIME_VERSION", "8.0.4")
                    .SetBuildArg("SUBSYSTEM", project.Directory.Parent?.Name ?? string.Empty)
                    .SetBuildArg("PROJECT", project.Name));

                if (((IBuildServer)GitHubActions.Instance)?.Branch == "main" && !(GitHubActions.Instance?.IsPullRequest ?? false))
                {
                    DockerTasks.DockerPush(s => s.SetName(imageName));
                }
                else
                {
                    Log.Information("Skipping Docker push for non-main branches or pull requests");
                }
            }
        });

    Project[] GetChangedProjects()
    {
        var changedFiles = GitTasks.Git("diff --name-only HEAD HEAD~1")
            .Select(output => output.Text)
            .Select(Path.GetFullPath)
            .ToArray();

        var changedProjects = Solution.AllProjects
            .Where(p => changedFiles.Any(file => file.StartsWith(p.Directory)))
            .ToArray();

        return changedProjects.Length != 0 ? changedProjects : Solution.AllProjects.ToArray();
    }
}
