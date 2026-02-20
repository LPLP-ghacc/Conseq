using System.Collections;
using System.Globalization;
using System.Reflection;

namespace ConseqConcatenation;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class ElementNameAttribute(string value) : Attribute
{
    public string Title { get; } = value;
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class CommentAttribute(string value) : Attribute
{
    public string Comment { get; } = value;
}

public enum ConseqFormat
{
    None,
    Compact,
    Readable
}

public interface IConseqData;

public static class Conseq
{
    extension(IConseqData data)
    {
        public string Conqsequalize() => Conqsequalize(data, ConseqFormat.None);

        public string Conqsequalize(ConseqFormat format)
        {
            var type = data.GetType();
            var classComment = type.GetCustomAttribute<CommentAttribute>()?.Comment;
            var header = type.GetCustomAttribute<ElementNameAttribute>()?.Title;
            var isCompact = format == ConseqFormat.Compact;

            var classCommentLine = isCompact || string.IsNullOrEmpty(classComment) ? string.Empty : "# " + classComment + Environment.NewLine;
            var headerLine = string.IsNullOrEmpty(header) ? string.Empty : $"[{header}]" + (isCompact ? " " : Environment.NewLine);

            var nameValueSep = isCompact ? ":" : " = ";

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f =>
                {
                    var name = f.GetCustomAttribute<ElementNameAttribute>()?.Title ?? f.Name;
                    var cAttr = f.GetCustomAttribute<CommentAttribute>();
                    var commentLine = isCompact || cAttr == null || string.IsNullOrEmpty(cAttr.Comment) ? string.Empty : "# " + cAttr.Comment + Environment.NewLine;
                    return commentLine + $"{name}{nameValueSep}{SerializeValue(f.GetValue(data))}";
                });

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p =>
                {
                    var name = p.GetCustomAttribute<ElementNameAttribute>()?.Title ?? p.Name;
                    var cAttr = p.GetCustomAttribute<CommentAttribute>();
                    var commentLine = isCompact || cAttr == null || string.IsNullOrEmpty(cAttr.Comment) ? string.Empty : "# " + cAttr.Comment + Environment.NewLine;
                    return commentLine + $"{name}{nameValueSep}{SerializeValue(p.GetValue(data))}";
                });

            var separator = isCompact ? " " : (format == ConseqFormat.Readable ? ',' + Environment.NewLine : Environment.NewLine);
            var body = string.Join(separator, fields.Concat(props));
            return classCommentLine + headerLine + body;
        }
    }
    
    public static string Conqsequalize<T>(T data, ConseqFormat format = ConseqFormat.None)
    {
        if (data is IEnumerable enumerable and not string)
        {
            var blocks = new List<string>();

            foreach (var item in enumerable)
            {
                if (item is IConseqData cd)
                    blocks.Add(cd.Conqsequalize(format));
            }

            return string.Join(Environment.NewLine + Environment.NewLine, blocks);
        }

        if (data is IConseqData conseq)
            return conseq.Conqsequalize(format);

        throw new InvalidOperationException("Unsupported root type.");
    }
    
    private static object DeserializeRootCollection(string text, Type targetType)
    {
        var elementType = targetType.IsArray
            ? targetType.GetElementType()!
            : targetType.GetGenericArguments()[0];

        var itemsText = text
            .Split(["\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries);

        var list = (IList)Activator.CreateInstance(
            typeof(List<>).MakeGenericType(elementType))!;

        foreach (var itemText in itemsText)
        {
            var item = Deconqsequalize(itemText.Trim(), elementType);
            list.Add(item);
        }

        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        if (targetType.IsInterface)
            return list;

        var concrete = Activator.CreateInstance(targetType)!;
        var addMethod = targetType.GetMethod("Add");

        foreach (var item in list)
            addMethod?.Invoke(concrete, [item]);

        return concrete;
    }
    
    private static bool IsRootCollection(Type type)
    {
        if (type == typeof(string)) return false;
        if (type.IsArray) return true;

        return type.IsGenericType &&
               typeof(IEnumerable).IsAssignableFrom(type);
    }
    
    public static T Deconqsequalize<T>(string text) => (T)Deconqsequalize(text, typeof(T));

    private static object Deconqsequalize(string text, Type targetType)
    {
        if (IsRootCollection(targetType))
            return DeserializeRootCollection(text, targetType);
        
        var instance = Activator.CreateInstance(targetType)!;

        var lines = text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !l.StartsWith('#'))
            .ToList();

        if (lines.Count > 0 && lines[0].StartsWith('[') && lines[0].EndsWith(']'))
            lines.RemoveAt(0);

        var keyValuePairs = ParseKeyValue(lines);

        var members = targetType
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.MemberType is MemberTypes.Field or MemberTypes.Property)
            .ToList();

        foreach (var (key, value) in keyValuePairs)
        {
            var member = members.FirstOrDefault(m =>
            {
                var attrName = m.GetCustomAttribute<ElementNameAttribute>()?.Title;
                return string.Equals(attrName ?? m.Name, key, StringComparison.OrdinalIgnoreCase);
            });

            if (member == null)
                continue;

            var targetTypeOfMember = member switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => null
            };

            if (targetTypeOfMember == null)
                continue;

            var converted = ConvertValue(value, targetTypeOfMember);

            switch (member)
            {
                case FieldInfo f:
                    f.SetValue(instance, converted);
                    break;
                case PropertyInfo { CanWrite: true } p:
                    p.SetValue(instance, converted);
                    break;
            }
        }

        return instance;
    }

    private static List<(string key, string value)> ParseKeyValue(List<string> lines)
    {
        var result = new List<(string, string)>();

        foreach (var segments in lines.Select(line =>
                     line.Contains(' ') && !line.Contains('=')
                         ? line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         : [line]))
        {
            result.AddRange(from segment in segments
                let sepIndex = segment.Contains('=')
                    ? segment.IndexOf('=')
                    : segment.IndexOf(':')
                where sepIndex > 0
                let key = segment[..sepIndex].Trim()
                let value = segment[(sepIndex + 1)..]
                    .Trim()
                    .TrimEnd(',')
                select (key, value));
        }

        return result;
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        while (true)
        {
            if (targetType == typeof(string)) return value;

            if (string.IsNullOrWhiteSpace(value)) return null;

            // Nullable<T>
            var nullable = Nullable.GetUnderlyingType(targetType);
            if (nullable != null)
            {
                targetType = nullable;
                continue;
            }

            // IConseqData
            if (typeof(IConseqData).IsAssignableFrom(targetType))
            {
                return Deconqsequalize(value, targetType);
            }

            // array
            if (targetType.IsArray)
            {
                var elementType = targetType.GetElementType()!;
                var parts = SplitCollection(value);

                var array = Array.CreateInstance(elementType, parts.Length);

                for (int i = 0; i < parts.Length; i++) array.SetValue(ConvertValue(parts[i], elementType), i);

                return array;
            }

            // Generic collections
            if (targetType.IsGenericType)
            {
                var genericDef = targetType.GetGenericTypeDefinition();
                var args = targetType.GetGenericArguments();

                // Dictionary<TKey, TValue>
                if (genericDef == typeof(Dictionary<,>))
                {
                    var dict = (IDictionary)Activator.CreateInstance(targetType)!;

                    foreach (var pair in SplitCollection(value))
                    {
                        var kv = pair.Split(':', 2);
                        if (kv.Length != 2) throw new FormatException($"Invalid dictionary pair: {pair}");

                        var key = ConvertValue(kv[0].Trim(), args[0]);
                        var val = ConvertValue(kv[1].Trim(), args[1]);

                        dict.Add(key!, val);
                    }

                    return dict;
                }

                // List<T>, ICollection<T>, IEnumerable<T>, HashSet<T>
                if (typeof(IEnumerable).IsAssignableFrom(targetType))
                {
                    var elementType = args[0];

                    var concreteType = targetType.IsInterface
                        ? typeof(List<>).MakeGenericType(elementType)
                        : targetType;

                    var collection = Activator.CreateInstance(concreteType)!;

                    var addMethod = concreteType.GetMethod("Add");
                    if (addMethod == null)
                        throw new InvalidOperationException($"Type {concreteType} does not have Add method.");

                    foreach (var part in SplitCollection(value))
                    {
                        var converted = ConvertValue(part, elementType);
                        addMethod.Invoke(collection, [converted]);
                    }

                    return collection;
                }
            }

            // Enum
            if (targetType.IsEnum) return Enum.Parse(targetType, value, true);

            // Primitive special cases
            if (targetType == typeof(Guid)) return Guid.Parse(value);

            if (targetType == typeof(DateTime)) return DateTime.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(TimeSpan)) return TimeSpan.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(bool)) return bool.Parse(value);

            return targetType == typeof(decimal) ? decimal.Parse(value, CultureInfo.InvariantCulture) :
                // other primitive
                Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
    
    private static string SerializeValue(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string s:
                return s;
        }

        var type = value.GetType();

        // Dictionary<TKey, TValue>
        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var pairs = (from object? item in (IEnumerable)value let keyProp = item?.GetType().GetProperty("Key")! let valueProp = item.GetType().GetProperty("Value")! let key = keyProp.GetValue(item) let val = valueProp.GetValue(item) select $"{SerializeValue(key)}:{SerializeValue(val)}").ToList();

            return string.Join(";", pairs);
        }

        switch (value)
        {
            // IEnumerable (not string)
            case IEnumerable enumerable and not string:
            {
                var items = (from object? item in enumerable select SerializeValue(item)).ToList();

                return string.Join(";", items);
            }
            case DateTime dt:
                return dt.ToString(CultureInfo.InvariantCulture);
            case TimeSpan ts:
                return ts.ToString();
            case IFormattable f:
                return f.ToString(null, CultureInfo.InvariantCulture);
            default:
                return value.ToString() ?? string.Empty;
        }
    }
    
    private static string[] SplitCollection(string value)
    {
        return value
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .ToArray();
    }
}