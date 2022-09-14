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



    private static void Main(string[] args)
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

        Console.WriteLine($"Grabbing shit from {fromDirectory} & {toDirectory}");
        var fromFiles = Directory.GetFiles(fromDirectory, "*.meta", SearchOption.AllDirectories);
        var toFiles = Directory.GetFiles(toDirectory, "*.meta", SearchOption.AllDirectories);
        
        var fromPrefabs = Directory.GetFiles(fromDirectory, "*.prefab", SearchOption.AllDirectories);
        
        
        
        
        FromFiles(fromFiles);
        ToFiles(toFiles);

        Console.WriteLine("Grabbed shit, comparing now...");
        
        guidDictionary = Compare();
        
        Console.WriteLine("Compared.. Updating..");
        
        UpdateMeta();
        UpdatePrefabs(fromPrefabs);
        
        Console.WriteLine("Finished updating.");
    }
    
    
    

    private static void FromFiles(string[] fromFiles)
    {
        foreach (var file in fromFiles)
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (line.Contains("guid: "))
                {
                    var start = line.IndexOf("guid: ");
                    var guid = "";
                    for (int i = start + 6; i < line.Length; i++)
                    {
                        if (line[i].Equals(" "))
                            break;
                        else
                        {
                            guid += line[i];
                        }
                    }

                    if (!metaAndScriptsToUpdate.ContainsKey(file))
                        metaAndScriptsToUpdate.Add(file, guid);
                    
                }
            }
        }
    }

    private static void ToFiles(string[] toFiles)
    {
        foreach (var file in toFiles)
        {

            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (line.Contains("guid: "))
                {
                    var start = line.IndexOf("guid: ");
                    var guid = "";
                    for (int i = start + 6; i < line.Length; i++)
                    {
                        if (line[i].Equals(" "))
                            break;
                        else
                        {
                            guid += line[i]; 
                        }
                    }
                    
                    if (!metaAndScripts.ContainsKey(file))
                        metaAndScripts.Add(file, guid);
                }
            }
        }
    }

    private static Dictionary<string, string> Compare()
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        foreach (var meta in metaAndScripts)
        {
            foreach (var key in metaAndScriptsToUpdate)
            {
                if (Path.GetFileName(key.Key) == Path.GetFileName(meta.Key))
                { 
                    Console.WriteLine($"{Path.GetFileName(key.Key)} == {Path.GetFileName(meta.Key)}");
                    if (!dictionary.ContainsKey(key.Value))
                        dictionary.Add(key.Value, meta.Value);
                }
            }
        }

        return dictionary;
    }

    private static void UpdateMeta()
    {
        foreach (var meta in metaAndScripts)
        {
            foreach (var key in metaAndScriptsToUpdate)
            {
                if (key.Key.Contains(Path.GetFileName(meta.Key)))
                {
                    var metaToUpdate = metaAndScriptsToUpdate[key.Key];
                    if (meta.Key != metaToUpdate)
                    {
                        Console.WriteLine("Updating " + meta.Key + " from " + metaToUpdate  + " to " + meta.Value);
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
                                    else
                                    {
                                        guid += line[i];
                                    }
                                }
                                var replace = line.Replace(guid, meta.Value);
                                newLine = replace;
                            }
                            newLines.Add(newLine);
                        }
                        File.WriteAllLines(key.Key, newLines);
                        Console.WriteLine("attempting to write to: " +  key.Key);
                    }
                }
            }
        }
    }
    
    private static void UpdatePrefabs(string[] prefabs)
    {
        
        foreach (var prefab in prefabs)
        {
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
                        else
                        {
                            guid += line[i];
                        }
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
            Console.WriteLine("attempting to write to: " +  prefab);
        }
    }
}