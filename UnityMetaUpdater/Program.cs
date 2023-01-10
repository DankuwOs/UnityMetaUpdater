using System.Diagnostics;

public class UnityMetaUpdater
{
    static string fromDirectory = "None";
    static string toDirectory = "None";

    /// <summary>
    /// Path to file | guid
    /// </summary>
    static Dictionary<string, string> metaAndScripts = new Dictionary<string, string>();

    /// <summary>
    /// Path to file | guid
    /// </summary>
    static Dictionary<string, string> metaAndScriptsToUpdate = new Dictionary<string, string>();
    
    /// <summary>
    /// Previous GUID | New GUID
    /// </summary>
    static Dictionary<string, string> guidDictionary = new Dictionary<string, string>();

    private static ProgressBar bar;

    private static async Task Main(string[] args)
    {
        if (args.Length >= 2)
        {
            fromDirectory = args[0];
            toDirectory = args[1];
        }

        if (fromDirectory == "None" || toDirectory == "None")
        {
            Console.WriteLine("Usage: UnityMetaUpdater.exe <from directory> <to directory>");
            return;
        }
        Console.WriteLine("Make sure you've backed up your unity projects, this program may just ruin your project. Press 'Y' to continue.");
        
        if (Console.ReadKey().Key != ConsoleKey.Y)
            return;

        bar = new ProgressBar();
        Console.Write("\nGrabbing meta files... ");
        
        var fromFiles = Directory.GetFiles(fromDirectory, "*.meta", SearchOption.AllDirectories);
        bar.Report(0.25);
        
        var toFiles = Directory.GetFiles(toDirectory, "*.meta", SearchOption.AllDirectories);
        bar.Report(0.5);
        
        var fromPrefabs = Directory.GetFiles(fromDirectory, "*.prefab", SearchOption.AllDirectories);
        bar.Report(0.75);
        
        var fromScenes = Directory.GetDirectories(fromDirectory, "*.unity", SearchOption.AllDirectories);
        bar.Report(1);
        
        Console.Write("\nFinding GUIDs in files... ");
        
        bar = new ProgressBar();

        FromFiles(fromFiles);
        ToFiles(toFiles);
        
        Console.Write("\nComparing GUIDs in files... ");
        
        bar = new ProgressBar();
        
        guidDictionary = Compare();
        
        Console.Write("\nUpdating files... ");
        
        bar = new ProgressBar();
        
        UpdateMeta();
        UpdatePrefabs(fromPrefabs);
        UpdateScenes(fromScenes);
        
        Console.WriteLine("\nFinished updating!");
    }
    
    
    

    private static void FromFiles(string[] fromFiles)
    {
        for (var index = 0; index < fromFiles.Length; index++)
        {
            bar?.Report(((double) index / fromFiles.Length) / 2);

            var file = fromFiles[index];
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (!line.Contains("guid: ")) continue;

                var start = line.IndexOf("guid: ");
                var guid = "";
                for (int i = start + 6; i < line.Length; i++)
                {
                    if (line[i].Equals(' '))
                        break;

                    guid += line[i];
                }

                if (!metaAndScriptsToUpdate.ContainsKey(file))
                    metaAndScriptsToUpdate.Add(file, guid);
            }
        }
    }

    private static void ToFiles(string[] toFiles)
    {
        for (var index = 0; index < toFiles.Length; index++)
        {
            bar?.Report(((double) index + 1 / toFiles.Length) / 2 + 0.5);

            var file = toFiles[index];
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (!line.Contains("guid: ")) continue;

                var start = line.IndexOf("guid: ");
                var guid = "";
                for (int i = start + 6; i < line.Length; i++)
                {
                    if (line[i].Equals(' '))
                        break;

                    guid += line[i];
                }

                if (!metaAndScripts.ContainsKey(file))
                    metaAndScripts.Add(file, guid);
            }
        }
    }

    private static Dictionary<string, string> Compare()
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        int i = 1;
        int length = metaAndScripts.Count;
        foreach (var meta in metaAndScripts)
        {
            bar?.Report(((double) i++ / length));
            
            foreach (var key in metaAndScriptsToUpdate.Where(key => Path.GetFileName(key.Key) == Path.GetFileName(meta.Key)))
            {
                if (!dictionary.ContainsKey(key.Value))
                    dictionary.Add(key.Value, meta.Value);
            }
        }

        return dictionary;
    }

    private static void UpdateMeta()
    {
        int index = 0;
        int length = metaAndScripts.Count;
        foreach (var meta in metaAndScripts)
        {
            bar?.Report(((double) index++ / length) / 3);
            foreach (var key in metaAndScriptsToUpdate)
            {
                
                if (!key.Key.Contains(Path.GetFileName(meta.Key))) continue;
                
                var metaToUpdate = metaAndScriptsToUpdate[key.Key];
                    
                if (meta.Key == metaToUpdate) continue;
                
                
                var lines = File.ReadAllLines(key.Key);
                var newLines = new List<string>();
                foreach (var line in lines)
                {
                    var newLine = line;
                    if (line.Contains("guid: "))
                    {
                        var start = line.IndexOf("guid: ");
                        var guid = "";
                        for (int i = start + 6; i < line.Length; i++)
                        {
                            if (line.Substring(i, 1).Equals(" "))
                                break;
                            guid += line[i];
                        }
                        var replace = line.Replace(guid, meta.Value);
                        newLine = replace;
                    }
                    newLines.Add(newLine);
                }
                File.WriteAllLines(key.Key, newLines);
            }
        }
    }
    
    private static void UpdatePrefabs(string[] prefabs)
    {
        int index = 0;
        int length = prefabs.Length;
        
        
        foreach (var prefab in prefabs)
        {
            bar?.Report(((double) index++ / length) / 3 + 0.33333);
            
            var lines = File.ReadAllLines(prefab);
            var newLines = new List<string>();
            foreach (var line in lines)
            {
                var newLine = line;
                if (line.Contains("guid: "))
                {
                    var start = line.IndexOf("guid: ");
                    var guid = "";
                    for (int i = start + 6; i < line.Length; i++)
                    {
                        if (line.Substring(i, 1).Equals(",") || line.Substring(i, 1).Equals(" "))
                            break;
                        guid += line[i];
                    }

                    if (guidDictionary.ContainsKey(guid))
                    {
                        var replace = line.Replace(guid, guidDictionary[guid]);
                        newLine = replace;
                    }
                    else
                    {
                        newLine = line;
                    }
                }
                newLines.Add(newLine);
            }
            File.WriteAllLines(prefab, newLines);
        }
    }
    
    private static void UpdateScenes(string[] scenes)
    {
        int index = 0;
        int length = scenes.Length;
        
        
        foreach (var scene in scenes)
        {
            bar?.Report(((double) index++ / length) / 3 + 0.66666);
            
            var lines = File.ReadAllLines(scene);
            var newLines = new List<string>();
            foreach (var line in lines)
            {
                var newLine = line;
                if (line.Contains("guid: "))
                {
                    var start = line.IndexOf("guid: ");
                    var guid = "";
                    for (int i = start + 6; i < line.Length; i++)
                    {
                        if (line.Substring(i, 1).Equals(",") || line.Substring(i, 1).Equals(" "))
                            break;
                        guid += line[i];
                    }

                    if (guidDictionary.ContainsKey(guid))
                    {
                        var replace = line.Replace(guid, guidDictionary[guid]);
                        newLine = replace;
                    }
                    else
                    {
                        newLine = line;
                    }
                }
                newLines.Add(newLine);
            }
            File.WriteAllLines(scene, newLines);
        }
    }
}