namespace ConseqConcatenation.TESTS;

public enum TestStatus
{
    None,
    Active,
    Disabled
}

public class TestItem : IConseqData
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public TestStatus Status { get; set; }
}

public static class RootCollectionTest
{
    public static void Run()
    {
        Test_List_RoundTrip();
        Test_Empty_List();

        Console.WriteLine("ALL TESTS PASSED");
    }

    private static void Test_List_RoundTrip()
    {
        var original = new List<TestItem>
        {
            new() { Name = "Alpha", Value = 10, Status = TestStatus.Active },
            new() { Name = "Beta",  Value = 20, Status = TestStatus.Disabled }
        };

        var text = Conseq.Conqsequalize(original, ConseqFormat.Readable);

        var restored = Conseq.Deconqsequalize<List<TestItem>>(text);

        if (restored == null)
            Fail("Restored list is null");

        if (restored.Count != original.Count)
            Fail("Count mismatch");

        for (int i = 0; i < original.Count; i++)
        {
            if (restored[i].Name != original[i].Name)
                Fail($"Name mismatch at {i}");

            if (restored[i].Value != original[i].Value)
                Fail($"Value mismatch at {i}");

            if (restored[i].Status != original[i].Status)
                Fail($"Enum mismatch at {i}");
        }
    }

    private static void Test_Empty_List()
    {
        var original = new List<TestItem>();

        var text = Conseq.Conqsequalize(original, ConseqFormat.Readable);

        var restored = Conseq.Deconqsequalize<List<TestItem>>(text);

        if (restored == null)
            Fail("Empty list restored as null");

        if (restored.Count != 0)
            Fail("Empty list count mismatch");
    }

    private static void Fail(string message)
    {
        throw new InvalidOperationException("TEST FAILED: " + message);
    }
}