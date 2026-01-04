using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Packages.Rider.Editor.ProjectGeneration
{
  internal class ProjectGenerationSLN : ProjectGenerationBase
  {
    public ProjectGenerationSLN(string projectDirectory, IAssemblyNameProvider assemblyNameProvider, IFileIO fileIoProvider, IGUIDGenerator guidGenerator)
      : base(projectDirectory, assemblyNameProvider, fileIoProvider, guidGenerator)
    {
    }

    public override string SolutionFile()
    {
      return Path.Combine(ProjectDirectory, $"{m_ProjectName}.sln");
    }

    protected override void SyncSolution(StringBuilder stringBuilder, List<ProjectPart> islands, Type[] types)
    {
      SyncSolutionFileIfNotChanged(SolutionFile(), SolutionText(stringBuilder, islands), types);
    }

    private string SolutionText(StringBuilder stringBuilder, List<ProjectPart> islands)
    {
      stringBuilder
        .AppendLine()
        .AppendLine("Microsoft Visual Studio Solution File, Format Version 11.00")
        .AppendLine("# Visual Studio 2010");

      var config = RiderProjectConfigStorage.Load();
      var solutionFolders = new HashSet<string>();
      var solutionFolderProjectsCount = new Dictionary<string, int>();
      var islandFolders = new Dictionary<string, string>(); // island name -> folder path

      foreach (var island in islands)
      {
        var folder = island.SolutionFolder;
        if (config.groupProjectsByName)
        {
          var parts = island.Name.Split('.');
          if (parts.Length > 0 && (parts[0].Equals("com", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("net", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("org", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("pl", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("de", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("uk", StringComparison.OrdinalIgnoreCase)))
          {
            parts = parts.Skip(1).ToArray();
          }

          if (parts.Length > config.groupProjectsByNameDepth)
          {
            var folderParts = new string[config.groupProjectsByNameDepth];
            Array.Copy(parts, folderParts, config.groupProjectsByNameDepth);
            folder = string.Join(".", folderParts);
          }
          else if (parts.Length > 1)
          {
             var length = parts.Length - 1;
             if (config.groupProjectsByNameDepth > 0)
               length = Math.Min(length, config.groupProjectsByNameDepth);

             folder = string.Join(".", parts.Take(length));
          }
        }

        if (!string.IsNullOrEmpty(folder))
        {
          islandFolders[island.Name] = folder;

          // Add all parent folders
          var currentFolder = folder;
          while (!string.IsNullOrEmpty(currentFolder))
          {
            solutionFolders.Add(currentFolder);
            if (!solutionFolderProjectsCount.ContainsKey(currentFolder))
              solutionFolderProjectsCount[currentFolder] = 0;
            solutionFolderProjectsCount[currentFolder]++;

            var lastDot = currentFolder.LastIndexOf('.');
            if (lastDot > 0)
              currentFolder = currentFolder.Substring(0, lastDot);
            else
              break;
          }
        }
      }

      foreach (var folder in solutionFolders)
      {
        if (!config.groupProjectsByName && solutionFolderProjectsCount[folder] < 2) continue; // Keep old behavior for variant grouping

        stringBuilder
          .Append("Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"")
          .Append(Path.GetFileName(folder.Replace('.', '/'))) // Visual name
          .Append("\", \"")
          .Append(Path.GetFileName(folder.Replace('.', '/'))) // Folder path
          .Append("\", \"{")
          .Append(SolutionFolderGuid(folder))
          .AppendLine("}\"")
          .AppendLine("EndProject");
      }

      foreach (var island in islands)
      {
        var projectName = m_AssemblyNameProvider.GetProjectName(island.Name, island.Defines);

        // GUID is for C# class libraries
        stringBuilder
          .Append("Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"")
          .Append(projectName)
          .Append("\", \"")
          .Append(projectName)
          .Append(".csproj\", \"{")
          .Append(ProjectGuid(projectName))
          .AppendLine("}\"")
          .AppendLine("EndProject");
      }

      stringBuilder.AppendLine("Global")
        .AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution")
        .AppendLine("\t\tDebug|Any CPU = Debug|Any CPU")
        .AppendLine("\tEndGlobalSection")
        .AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

      foreach (var island in islands)
      {
        var projectGuid = ProjectGuid(m_AssemblyNameProvider.GetProjectName(island.Name, island.Defines));

        stringBuilder
          .Append("\t\t{").Append(projectGuid).AppendLine("}.Debug|Any CPU.ActiveCfg = Debug|Any CPU")
          .Append("\t\t{").Append(projectGuid).AppendLine("}.Debug|Any CPU.Build.0 = Debug|Any CPU");
      }

      stringBuilder.AppendLine("\tEndGlobalSection")
        .AppendLine("\tGlobalSection(SolutionProperties) = preSolution")
        .AppendLine("\t\tHideSolutionNode = FALSE")
        .AppendLine("\tEndGlobalSection");

      if (solutionFolders.Count > 0)
      {
        stringBuilder.AppendLine("\tGlobalSection(NestedProjects) = preSolution");

        // Map projects to folders
        foreach (var island in islands)
        {
          if (islandFolders.TryGetValue(island.Name, out var folder))
          {
             if (!config.groupProjectsByName && solutionFolderProjectsCount[folder] < 2) continue;

             var projectName = m_AssemblyNameProvider.GetProjectName(island.Name, island.Defines);
             stringBuilder
              .Append("\t\t{")
              .Append(ProjectGuid(projectName))
              .Append("} = {")
              .Append(SolutionFolderGuid(folder))
              .AppendLine("}");
          }
        }

        // Map folders to parents
        foreach (var folder in solutionFolders)
        {
           var lastDot = folder.LastIndexOf('.');
           if (lastDot > 0)
           {
             var parent = folder.Substring(0, lastDot);
             if (solutionFolders.Contains(parent))
             {
               if (!config.groupProjectsByName && solutionFolderProjectsCount[parent] < 2) continue; // Heuristic check

               stringBuilder
                .Append("\t\t{")
                .Append(SolutionFolderGuid(folder))
                .Append("} = {")
                .Append(SolutionFolderGuid(parent))
                .AppendLine("}");
             }
           }
        }

        stringBuilder.AppendLine("\tEndGlobalSection");
      }

      stringBuilder.AppendLine("EndGlobal");

      return stringBuilder.ToString();
    }
  }
}
