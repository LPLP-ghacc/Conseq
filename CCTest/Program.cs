using ConseqConcatenation;

namespace CCTest;

public class Settings: IConseqData
{
    public string VoiceInput { get; init; } = null!;
    public int VoiceSpeed { get; init; }
    public int VoiceVolume { get; init; }
    public int StdDelay { get; init; }
    public string ReaderName { get; init; } = null!;

    public Settings(string voiceInput, int voiceSpeed, int voiceVolume, int stdDelay, string readerName) 
    {
        VoiceInput = voiceInput;
        VoiceSpeed = voiceSpeed;
        VoiceVolume = voiceVolume;
        StdDelay = stdDelay;
        ReaderName = readerName;
    }
    
    public static Settings Default { get; } = new("CABLE Input", 0, 100, 10, "Microsoft Irina");

    public async Task Save(string path, string fileName)
    {
        var conseq = this.Conqsequalize();
        
        await File.WriteAllTextAsync(Path.Combine(path, fileName), conseq);
    }

    public static async Task<Settings> Load(string path)
    {
        if (!Path.Exists(path))
        {
            var settings = Default;
            var conseqSave = settings.Conqsequalize();
            await File.WriteAllTextAsync(Path.Combine(Environment.CurrentDirectory, "settings.cc"), conseqSave);
            
            return settings;
        }
        
        var conseq = await File.ReadAllTextAsync(path);
        
        try
        {
            return Conseq.Deconqsequalize<Settings>(conseq);
        }
        catch
        {
            return Default;
        }
    }
}


[Comment("this class represents a test for Conqsequalize processing")]
[ElementName("example class")]
public class Example : IConseqData
{
    [ElementName("sample name")]
    public int[] Numbers { get; set; }
    public List<string> Names { get; set; }
    public HashSet<Guid> Ids { get; set; }
    public Dictionary<string, int> Map { get; set; }
    public DateTime Created { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal Price { get; set; }
    public bool Enabled { get; set; }
}

public static class Program
{
    public static void Main(string[] args)
    {
        var example = new Example
        {
            Numbers = [0, 0, 0, 1],
            Names = ["hello", "world", "!"],
            Ids = [Guid.NewGuid(), Guid.NewGuid()],
            Map = new Dictionary<string, int>
            {
                ["A"] = 1,
                ["B"] = 2
            },
            Created = DateTime.UtcNow.Date,
            Duration = TimeSpan.FromMinutes(90),
            Price = 123.45m,
            Enabled = true
        };

        var text = example.Conqsequalize();
        
        Console.WriteLine(text);
        /*
# this class represents a test for Conqsequalize processing
[example class]
sample name = 0;0;0;1
Names = hello;world;!
Ids = 355ee557-aed4-4bde-a2db-1f8b71a05220;ef556019-fe0f-46e8-84da-da6738ad4a90
Map = A:1;B:2
Created = 02/19/2026 00:00:00
Duration = 01:30:00
Price = 123.45
Enabled = True
Round-trip test PASSED
        */
        
        var result = Conseq.Deconqsequalize<Example>(text);

        Validate(example, result);

        Console.WriteLine("Round-trip test PASSED");
    }

    private static void Validate(Example expected, Example actual)
    {
        if (!expected.Enabled.Equals(actual.Enabled))
            Fail("Enabled mismatch");

        if (expected.Price != actual.Price)
            Fail("Price mismatch");

        if (expected.Created != actual.Created)
            Fail("Created mismatch");

        if (expected.Duration != actual.Duration)
            Fail("Duration mismatch");

        // Array check
        if (expected.Numbers == null || actual.Numbers == null)
            Fail("Numbers null mismatch");

        if (expected.Numbers.Length != actual.Numbers.Length)
            Fail("Numbers length mismatch");

        for (int i = 0; i < expected.Numbers.Length; i++)
        {
            if (expected.Numbers[i] != actual.Numbers[i])
                Fail($"Numbers[{i}] mismatch");
        }

        // List check
        if (expected.Names == null || actual.Names == null)
            Fail("Names null mismatch");

        if (expected.Names.Count != actual.Names.Count)
            Fail("Names count mismatch");

        for (int i = 0; i < expected.Names.Count; i++)
        {
            if (expected.Names[i] != actual.Names[i])
                Fail($"Names[{i}] mismatch");
        }

        // HashSet check
        if (!expected.Ids.SetEquals(actual.Ids))
            Fail("Ids set mismatch");

        // Dictionary check
        if (expected.Map.Count != actual.Map.Count)
            Fail("Map count mismatch");

        foreach (var kv in expected.Map)
        {
            if (!actual.Map.TryGetValue(kv.Key, out var val))
                Fail($"Map missing key {kv.Key}");

            if (val != kv.Value)
                Fail($"Map value mismatch for key {kv.Key}");
        }
    }

    private static void Fail(string message)
    {
        throw new InvalidOperationException("TEST FAILED: " + message);
    }
}