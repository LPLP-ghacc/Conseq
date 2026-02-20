# ConseqConcatenation

**ConseqConcatenation** is a lightweight, reflection-based object serializer for .NET that converts objects into a compact, human-readable key-value text format and back.

It is designed for:

- Configuration-like structures  
- Lightweight data persistence  
- Debug-friendly serialization  
- Human-editable structured text  

This is **not JSON** and not XML. It is a minimal custom format optimized for clarity and simplicity.

---

## Features

- Attribute-based field/property naming  
- Inline and readable formatting modes  
- Support for:
  - Primitive types  
  - `string`  
  - `decimal`  
  - `bool`  
  - `Guid`  
  - `DateTime`  
  - `TimeSpan`  
  - `Enum`  
  - Arrays  
  - `List<T>`  
  - `HashSet<T>`  
  - `Dictionary<TKey, TValue>`  
- Case-insensitive deserialization  
- Nullable type support  
- Reflection-driven member mapping  

---

## Installation

Clone the repository:

```bash
git clone https://github.com/yourusername/ConseqConcatenation.git
```

Or add the project directly to your solution.

---

## Quick Start

### 1. Define a Data Model

All serializable types must implement:

```csharp
public interface IConseqData;
```

Example:

```csharp
using ConseqConcatenation;

[ElementName("Example")]
[Comment("Example configuration object")]
public class Example : IConseqData
{
    public int[] Numbers { get; set; }
    public List<string> Names { get; set; }
    public HashSet<Guid> Ids { get; set; }
    public Dictionary<string, int> Map { get; set; }
    public DateTime Created { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal Price { get; set; }
    public bool Enabled { get; set; }
}
```

---

### 2. Serialize

```csharp
var example = new Example
{
    Numbers = new[] { 1, 2, 3 },
    Names = new List<string> { "Alice", "Bob" },
    Enabled = true,
    Price = 19.99m,
    Created = DateTime.UtcNow
};

string text = example.Conqsequalize();
Console.WriteLine(text);
```

Example output:

```
[Example]
Numbers = 1;2;3
Names = Alice;Bob
Enabled = True
Price = 19.99
```

---

### 3. Deserialize

```csharp
var restored = Conseq.Deconqsequalize<Example>(text);
Console.WriteLine(restored.Names[1]);
```

---

## Formatting Modes

```csharp
ConseqFormat.None
ConseqFormat.Compact
ConseqFormat.Readable
```

---

## Attributes

### ElementNameAttribute

Renames class, field, or property in serialized output.

```csharp
[ElementName("User")]
public class User : IConseqData
{
    [ElementName("login")]
    public string Username { get; set; }
}
```

---

### CommentAttribute

Adds human-readable comments (ignored during deserialization).

```csharp
[Comment("Application configuration")]
public class Config : IConseqData
{
    [Comment("Enable logging")]
    public bool Logging { get; set; }
}
```

---

## Supported Types

Primitives:

- int  
- double  
- decimal  
- bool  
- Guid  
- DateTime  
- TimeSpan  
- Enum  

Collections:

- Arrays (`T[]`)  
- `List<T>`  
- `HashSet<T>`  
- `Dictionary<TKey, TValue>`  
- Any generic collection with an `Add` method  

---

## Limitations

- No escaping mechanism for `;` or `:` inside values  
- Reflection-based (not optimized for high-throughput scenarios)  
- Not intended as a replacement for `System.Text.Json`  

---

## License

[MIT License](https://github.com/LPLP-ghacc/Conseq/blob/master/LICENSE)
