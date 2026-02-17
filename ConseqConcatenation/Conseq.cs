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

            var classCommentLine = isCompact || string.IsNullOrEmpty(classComment) ? "" : "# " + classComment + Environment.NewLine;
            var headerLine = string.IsNullOrEmpty(header) ? "" : $"[{header}]" + (isCompact ? " " : Environment.NewLine);

            var nameValueSep = isCompact ? ":" : " = ";

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f =>
                {
                    var name = f.GetCustomAttribute<ElementNameAttribute>()?.Title ?? f.Name;
                    var cAttr = f.GetCustomAttribute<CommentAttribute>();
                    var commentLine = isCompact || cAttr == null || string.IsNullOrEmpty(cAttr.Comment) ? "" : "# " + cAttr.Comment + Environment.NewLine;
                    return commentLine + $"{name}{nameValueSep}{f.GetValue(data)}";
                });

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p =>
                {
                    var name = p.GetCustomAttribute<ElementNameAttribute>()?.Title ?? p.Name;
                    var cAttr = p.GetCustomAttribute<CommentAttribute>();
                    var commentLine = isCompact || cAttr == null || string.IsNullOrEmpty(cAttr.Comment) ? "" : "# " + cAttr.Comment + Environment.NewLine;
                    return commentLine + $"{name}{nameValueSep}{p.GetValue(data)}";
                });

            var separator = isCompact ? " " : (format == ConseqFormat.Readable ? "," + Environment.NewLine : Environment.NewLine);
            var body = string.Join(separator, fields.Concat(props));
            return classCommentLine + headerLine + body;
        }
    }
    
    public static T Deconqsequalize<T>(string text) where T : IConseqData, new()
    {
        return (T)Deconqsequalize(text, typeof(T));
    }

    public static object Deconqsequalize(string text, Type targetType)
    {
        if (!typeof(IConseqData).IsAssignableFrom(targetType))
            throw new InvalidOperationException("Type must implement IConseqData.");

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
        return (from line in lines
            let sepIndex = line.Contains('=')
                ? line.IndexOf('=')
                : line.IndexOf(':')
                where sepIndex > 0
            let key = line[..sepIndex].Trim()
            let value = line[(sepIndex + 1)..].Trim()
            select (key, value)).ToList();
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        while (true)
        {
            if (targetType == typeof(string)) return value;

            if (string.IsNullOrEmpty(value)) return null;

            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                targetType = underlying;
                continue;
            }

            if (targetType.IsEnum) return Enum.Parse(targetType, value, ignoreCase: true);

            if (targetType == typeof(Guid)) return Guid.Parse(value);

            return targetType == typeof(DateTime) ? DateTime.Parse(value) : Convert.ChangeType(value, targetType);
        }
    }
}