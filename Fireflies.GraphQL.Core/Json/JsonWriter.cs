using System.Globalization;
using System.Text.Json;

namespace Fireflies.GraphQL.Core.Json;

public abstract class JsonWriter {
    private readonly MemoryStream _stream;
    protected readonly Utf8JsonWriter Writer;

    public byte[]? Result { get; set; }

    protected JsonWriter() {
        _stream = new MemoryStream();
        Writer = new Utf8JsonWriter(_stream);
        Writer.WriteStartObject(); // Root object
        Start();
    }

    protected abstract void Start();
    protected abstract void Stop();

    public async Task<byte[]> GetBuffer() {
        Stop();
        Writer.WriteEndObject();
        await Writer.FlushAsync();

        var result = _stream.ToArray();
        await Writer.DisposeAsync();
        await _stream.DisposeAsync();

        return result;
    }

    public void WriteStartArray() {
        Writer.WriteStartArray();
    }

    public void WriteStartArray(string fieldName) {
        Writer.WriteStartArray(fieldName);
    }

    public void WriteStartObject() {
        Writer.WriteStartObject();
    }

    public void WriteStartObject(string fieldName) {
        Writer.WriteStartObject(fieldName);
    }

    public void WriteEndObject() {
        Writer.WriteEndObject();
    }

    public void WriteEndArray() {
        Writer.WriteEndArray();
    }

    public void WriteNull(string fieldName) {
        Writer.WriteNull(fieldName);
    }

    public void WriteValue(object value, TypeCode typeCode, Type elementType) {
        if(elementType.IsEnum) {
            Writer.WriteStringValue(value.ToString());
            return;
        }

        switch(typeCode) {
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Byte:
            case TypeCode.SByte:
                Writer.WriteNumberValue((int)Convert.ChangeType(value, TypeCode.Int32));
                break;

            case TypeCode.Boolean:
                Writer.WriteBooleanValue((bool)value);
                break;

            case TypeCode.Char:
            case TypeCode.String:
                Writer.WriteStringValue((string)value);
                break;

            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                Writer.WriteNumberValue((decimal)Convert.ChangeType(value, TypeCode.Decimal));
                break;

            case TypeCode.DateTime:
                Writer.WriteStringValue(((DateTimeOffset)value).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo));
                break;

            default:
                if(value.GetType() == typeof(DateTimeOffset)) {
                    Writer.WriteStringValue(((DateTimeOffset)value).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo));
                    break;
                }

                throw new ArgumentOutOfRangeException(nameof(typeCode));
        }
    }

    public void WriteValue(string property, object value, TypeCode typeCode, Type elementType) {
        if(elementType.IsEnum) {
            Writer.WriteString(property, value.ToString());
            return;
        }

        switch(typeCode) {
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Byte:
            case TypeCode.SByte:
                Writer.WriteNumber(property, (int)Convert.ChangeType(value, TypeCode.Int32));
                break;

            case TypeCode.Boolean:
                Writer.WriteBoolean(property, (bool)value);
                break;

            case TypeCode.Char:
            case TypeCode.String:
                Writer.WriteString(property, (string)value);
                break;

            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                Writer.WriteNumber(property, (decimal)Convert.ChangeType(value, TypeCode.Decimal));
                break;

            case TypeCode.DateTime:
                Writer.WriteString(property, ((DateTimeOffset)value).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo));
                break;

            default:
                if(value.GetType() == typeof(DateTimeOffset)) {
                    Writer.WriteString(property, ((DateTimeOffset)value).ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo));
                    break;
                }

                throw new ArgumentOutOfRangeException(nameof(typeCode));
        }
    }
}