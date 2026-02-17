using ConseqConcatenation;

namespace CCTest;

[ElementName("test"), Comment("class comment")]
public class Test : IConseqData
{
    [ElementName("hi"), Comment("comment")]
    public int a;
    public int b;
    public int c;
}

public static class Program
{
    public static void Main(string[] args)
    {
        var test = new Test
        {
            a = 1,
            b = 2,
            c = 3
        };
        
        Console.WriteLine(test.Conqsequalize());
    }
}