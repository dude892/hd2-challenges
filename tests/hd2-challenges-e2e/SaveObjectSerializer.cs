using System.Diagnostics.CodeAnalysis;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(typeof(Hd2Challenges.EndToEnd.Tests.SaveObjectSerializer),
    typeof(Hd2Challenges.EndToEnd.Tests.SaveObject))]

namespace Hd2Challenges.EndToEnd.Tests;

public sealed class SaveObjectSerializer : IXunitSerializer
{
    public bool IsSerializable(Type type, object? value, [NotNullWhen(false)] out string? failureReason)
    {
        if (type == typeof(SaveObject) && value is SaveObject)
        {
            failureReason = null;
            return true;
        }

        failureReason = "Only save fixture objects are supported.";
        return false;
    }

    public string Serialize(object value) => Path.GetFileName(((SaveObject)value).FilePath);

    public object Deserialize(Type type, string serializedValue) => new SaveObject(serializedValue);
}
