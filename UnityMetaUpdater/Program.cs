using System.Security.Cryptography;
using System.Text;

public class UnityMetaUpdater
{
    static string fromDirectory = "None";
    static string toDirectory = "None";

    /// <summary>
    /// (File Path, Meta Path), GUID
    /// </summary>
    /// <returns></returns>
    private static Dictionary<Tuple<string, string>, string> fromFilesAndMeta =
        new Dictionary<Tuple<string, string>, string>();

    /// <summary>
    /// (File Path, Meta Path), GUID
    /// </summary>
    /// <returns></returns>
    private static Dictionary<Tuple<string, string>, string> toFilesAndMeta =
        new Dictionary<Tuple<string, string>, string>();

    /// <summary>
    /// From GUID, To GUID
    /// </summary>
    private static Dictionary<string, string> guidDictionary = new Dictionary<string, string>();

    /// <summary>
    /// (MD5 Hash, GUID)
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, string> hashDictionary = new Dictionary<string, string>();

    private static ProgressBar bar;

    private static readonly string[] UnityExtensions = new[]
    {
        ".mat",
        ".prefab",
        ".asset",
        ".unity",
        ".anim",
        ".controller",
        ".mixer",
        ".physicMaterial",
        ".renderTexture"
    };

    private static async Task Main(string[] args)
    {
        if (args.Length >= 2)
        {
            fromDirectory = args[0];
            toDirectory = args[1];
        }

        if (fromDirectory == "None" || toDirectory == "None")
        {
            Console.WriteLine(
                "Usage: UnityMetaUpdater.exe <from directory> <to directory>");
            return;
        }

        Console.WriteLine(
            "Make sure you've backed up your unity projects, this program may just ruin your project. Press 'Y' to continue.");


        if (Console.ReadKey().Key != ConsoleKey.Y)
            return;

        bar = new ProgressBar();
        Console.Write("\nGrabbing files... ");

        var fromFiles = Directory.GetFiles(fromDirectory, "*.*", SearchOption.AllDirectories)
            .Where(e => Path.GetExtension(e) != ".meta");
        bar.Report(0.25);

        var toFiles = Directory.GetFiles(toDirectory, "*.*", SearchOption.AllDirectories)
            .Where(e => Path.GetExtension(e) != ".meta");
        bar.Report(0.5);

        var fromFileMetaTuples = GetMetaFiles(fromFiles.ToArray());
        bar.Report(0.75);

        var toFileMetaTuples = GetMetaFiles(toFiles.ToArray());
        bar.Report(1);

        bar = new ProgressBar();
        Console.Write("\nFinding GUIDs in files... ");

        GetGUIDsFromList(fromFileMetaTuples, out fromFilesAndMeta);
        GetGUIDsFromList(toFileMetaTuples, out toFilesAndMeta, 1);


        bar = new ProgressBar();
        Console.Write("\nFinding hashes of all files...");
        FillHashDictionary(toFilesAndMeta, out hashDictionary);

        bar = new ProgressBar();
        Console.Write("\nComparing GUIDs in files... ");

        guidDictionary = Compare();


        bar = new ProgressBar();
        Console.Write("\nUpdating files... ");

        UpdateMeta();
        UpdateFiles();

        Console.WriteLine("\nFinished updating!");
    }

    private static List<Tuple<string, string>> GetMetaFiles(string[] files)
    {
        var fileMetaList = new List<Tuple<string, string>>();
        foreach (var file in files)
        {
            var metaFile = $"{file}.meta";
            if (File.Exists(metaFile))
                fileMetaList.Add(new Tuple<string, string>(file, metaFile));
        }

        return fileMetaList;
    }


    private static void GetGUIDsFromList(List<Tuple<string, string>> files,
        out Dictionary<Tuple<string, string>, string> dictionary, int num = 0)
    {
        dictionary = new Dictionary<Tuple<string, string>, string>();
        for (var index = 0; index < files.Count; index++)
        {
            // funny bar go number
            bar?.Report((((double)index + 1) / files.Count) / 2 + (num == 1 ? 0.5 : 0));

            var metaFile = files[index].Item2;
            var lines = File.ReadAllLines(metaFile);
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


                if (!dictionary.ContainsKey(files[index]))
                    dictionary.Add(files[index], guid);
            }
        }
    }

    private static Dictionary<string, string> Compare()
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        int i = 1;
        int length = fromFilesAndMeta.Count;

        foreach (var fromFile in fromFilesAndMeta)
        {
            bar?.Report((double)i++ / length / 2);
            if (!UnityExtensions.Contains(Path.GetExtension(fromFile.Key.Item1)))
            {
                continue;
            }

            // If the file is a .asset it might have a m_Name: line that is different based on the name of the file, compute it slightly differently.
            string hash = Path.GetExtension(fromFile.Key.Item1) == ".asset" ? ComputeHashAsset(fromFile.Key.Item1) : ComputeHash(fromFile.Key.Item1);

            if (!hashDictionary.ContainsKey(hash)) continue;
            if (dictionary.ContainsKey(fromFile.Value)) continue;

            dictionary.Add(fromFile.Value, hashDictionary[hash]);
        }
        
        
        i = 1;
        length = toFilesAndMeta.Count;
        foreach (var toFile in toFilesAndMeta)
        {
            bar?.Report((double)i++ / length / 2 + 0.5);

            foreach (var fromFile in fromFilesAndMeta.Where(key => Path.GetFileName(key.Key.Item1) == Path.GetFileName(toFile.Key.Item1)))
            {
                var toValue = toFile.Value;

                if (!dictionary.ContainsKey(fromFile.Value))
                    dictionary.Add(fromFile.Value, toValue);

            }
        }

        return dictionary;
    }


    private static void UpdateMeta()
    {
        int index = 1;
        int length = toFilesAndMeta.Count;
        foreach (var toFile in toFilesAndMeta)
        {
            bar?.Report(((double) index++ / length) / 2);
            foreach (var fromFile in fromFilesAndMeta)
            {
                
                if (!fromFile.Key.Item2.Contains(Path.GetFileName(toFile.Key.Item2))) continue;


                var lines = File.ReadAllLines(fromFile.Key.Item2);
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
                        var replace = line.Replace(guid, toFile.Value);
                        newLine = replace;
                    }
                    newLines.Add(newLine);
                }
                File.WriteAllLines(fromFile.Key.Item2, newLines);
            }
        }
    }

    private static void UpdateFiles()
    {
        int index = 1;
        int length = fromFilesAndMeta.Count;
        
        
        foreach (var file in fromFilesAndMeta)
        {
            bar?.Report(((double) index++ / length) / 2 + 0.5);
            if (!UnityExtensions.Contains(Path.GetExtension(file.Key.Item1)))
                continue;
            
            var lines = File.ReadAllLines(file.Key.Item1);
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
            File.WriteAllLines(file.Key.Item1, newLines);
        }
    }

    private static string ComputeHash(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(File.ReadAllBytes(filePath));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
    
    private static string ComputeHashAsset(string filePath)
    {
        var reader = File.OpenText(filePath);
        var builder = new StringBuilder();
        while (reader.ReadLine() is { } line)
        {
            if (!line.StartsWith("  m_Name:"))
                builder.AppendLine(line);
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());

        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    private static void FillHashDictionary(Dictionary<Tuple<string, string>, string> files, out Dictionary<string, string> outList)
    {
        int i = 1;
        int length = files.Count;
        outList = new Dictionary<string, string>();
        foreach (var pair in files)
        {
            bar?.Report((double)i++ / length);
            var file = pair.Key.Item1;
            var hash = Path.GetExtension(file) == ".asset" ? ComputeHashAsset(file) : ComputeHash(file);
            
            if (outList.ContainsKey(hash) || outList.ContainsValue(pair.Value))
                continue;
            
            outList.Add(hash, pair.Value);
        }
    }

    
}