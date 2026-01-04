using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Packages.Rider.Editor.ProjectGeneration
{
  internal class ProjectGenerationSLNX : ProjectGenerationBase
  {
    public ProjectGenerationSLNX(string projectDirectory, IAssemblyNameProvider assemblyNameProvider, IFileIO fileIoProvider, IGUIDGenerator guidGenerator)
      : base(projectDirectory, assemblyNameProvider, fileIoProvider, guidGenerator)
    {
    }

    public override string SolutionFile()
    {
      return Path.Combine(ProjectDirectory, $"{m_ProjectName}.slnx");
    }

    protected override void SyncSolution(StringBuilder stringBuilder, List<ProjectPart> islands, Type[] types)
    {
      SyncSolutionFileIfNotChanged(SolutionFile(), SolutionText(stringBuilder, islands), types);
    }

    private string SolutionText(StringBuilder stringBuilder, List<ProjectPart> islands)
    {
      stringBuilder.AppendLine("<Solution>");
      stringBuilder.AppendLine("  <Configurations>");
      stringBuilder.AppendLine("    <BuildType Name=\"Debug\" />");
      stringBuilder.AppendLine("  </Configurations>");

      var config = RiderProjectConfigStorage.Load();

      // Calculate folders and project locations
      var folderProjects = new Dictionary<string, List<string>>(); // folder path -> list of project paths
      var rootProjects = new List<string>();
      var allFolders = new HashSet<string>();

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
            folder = string.Join("/", folderParts);
          }
          else if (parts.Length > 1)
          {
             var length = parts.Length - 1;
             if (config.groupProjectsByNameDepth > 0)
               length = Math.Min(length, config.groupProjectsByNameDepth);

             folder = string.Join("/", parts.Take(length));
          }
        }

        var projectName = m_AssemblyNameProvider.GetProjectName(island.Name, island.Defines);
        var projectPath = $"{projectName}.csproj";

        if (!string.IsNullOrEmpty(folder))
        {
            folder = folder.Replace('.', '/');
            // Ensure folder starts and ends with slash for consistency with SLNX format
            if (!folder.StartsWith("/")) folder = "/" + folder;
            if (!folder.EndsWith("/")) folder = folder + "/";

            if (!folderProjects.ContainsKey(folder))
                folderProjects[folder] = new List<string>();
            folderProjects[folder].Add(projectPath);
            allFolders.Add(folder);

            // Add all parent folders to the set, but they might be empty of direct projects
            var currentFolder = folder;
            while (currentFolder.Length > 1) // > "/"
            {
               // Remove trailing slash
               var trimmed = currentFolder.Substring(0, currentFolder.Length - 1);
               var lastSlash = trimmed.LastIndexOf('/');
               if (lastSlash >= 0)
               {
                   var parent = trimmed.Substring(0, lastSlash + 1);
                   if (parent.Length > 0 && parent != "/")
                   {
                       allFolders.Add(parent);
                       currentFolder = parent;
                   }
                   else
                   {
                       break;
                   }
               }
               else
               {
                   break;
               }
            }
        }
        else
        {
            rootProjects.Add(projectPath);
        }
      }

      // Output folders sorted by name
      foreach (var folder in allFolders.OrderBy(x => x))
      {
          if (folderProjects.TryGetValue(folder, out var projects) && projects.Count > 0)
          {
              stringBuilder.Append("  <Folder Name=\"").Append(folder).AppendLine("\">");
              foreach (var project in projects.OrderBy(x => x))
              {
                  stringBuilder.Append("    <Project Path=\"").Append(project).AppendLine("\" />");
              }
              stringBuilder.AppendLine("  </Folder>");
          }
          else
          {
              stringBuilder.Append("  <Folder Name=\"").Append(folder).AppendLine("\" />");
          }
      }

      // Output root projects
      foreach (var project in rootProjects.OrderBy(x => x))
      {
          stringBuilder.Append("  <Project Path=\"").Append(project).AppendLine("\" />");
      }

      stringBuilder.AppendLine("</Solution>");
      return stringBuilder.ToString();
    }
  }
}
