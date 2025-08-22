using System.Dynamic;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;

namespace Nexus.Library.Modules;

public class DynamicGrpcClient
{
    private static readonly Dictionary<System.Type, (Struct, Method<Struct, Struct>)> _cached = new();

    public async Task<TR> InvokeAsync<T, TR>(string address, string serviceName, string methodName, T request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var response = await InvokeMethodAsync(address, serviceName, methodName, request);
        return (TR)ConvertFromStruct(response);
    }

    private async Task<Struct> InvokeMethodAsync(string address, string serviceName, string methodName, object request)
    {
        using (var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
               {
                   Credentials = ChannelCredentials.Insecure
               }))
        {
            Method<Struct, Struct> method;
            Struct requestStruct;
            if (_cached.TryGetValue(request.GetType(), out var cached))
            {
                method = cached.Item2;
                requestStruct = cached.Item1;
                PopulateStruct(request, requestStruct);
            }
            else
            {
                method = new Method<Struct, Struct>(
                    MethodType.Unary,
                    serviceName,
                    methodName,
                    new StructMarshaller(),
                    new StructMarshaller()
                );

                requestStruct = ConvertToStruct(request);
                _cached.Add(request.GetType(), (requestStruct, method));
            }

            var callInvoker = channel.CreateCallInvoker();
            return await callInvoker.AsyncUnaryCall(method, null, default, requestStruct);
        }
    }

    private Struct ConvertToStruct(object? obj)
    {
        var structValue = new Struct();

        if (obj == null) return structValue;

        foreach (var prop in obj.GetType().GetProperties())
        {
            var value = prop.GetValue(obj);
            structValue.Fields[prop.Name] = CreateValue(value);
        }

        return structValue;
    }

    private void PopulateStruct(object? obj, Struct structObj)
    {
        if (obj == null) return;

        foreach (var prop in obj.GetType().GetProperties())
        {
            var value = prop.GetValue(obj);
            structObj.Fields[prop.Name] = CreateValue(value);
        }
    }

    private Value CreateValue(object? value)
    {
        if (value == null)
            return Value.ForNull();

        return value switch
        {
            string s => Value.ForString(s),
            int i => Value.ForNumber(i),
            long l => Value.ForNumber(l),
            double d => Value.ForNumber(d),
            float f => Value.ForNumber(f),
            bool b => Value.ForBool(b),
            DateTime dt => Value.ForString(dt.ToString("O")),
            IEnumerable<object> list => CreateListValue(list),
            IDictionary<string, object> dict => Value.ForStruct(CreateStructFromDictionary(dict)),
            _ => Value.ForString(value.ToString())
        };
    }

    private Value CreateListValue(IEnumerable<object> list)
    {
        return Value.ForList(list.Select(CreateValue).ToArray());
    }

    private Struct CreateStructFromDictionary(IDictionary<string, object> dict)
    {
        var structValue = new Struct();
        foreach (var kvp in dict)
        {
            structValue.Fields[kvp.Key] = CreateValue(kvp.Value);
        }

        return structValue;
    }

    private dynamic ConvertFromStruct(Struct response)
    {
        var result = new ExpandoObject();
        var resultDict = result as IDictionary<string, object?>;

        foreach (var field in response.Fields)
        {
            resultDict[field.Key] = ExtractValue(field.Value);
        }

        return result;
    }

    private object? ExtractValue(Value value)
    {
        return value.KindCase switch
        {
            Value.KindOneofCase.NullValue => null,
            Value.KindOneofCase.NumberValue => value.NumberValue,
            Value.KindOneofCase.StringValue => value.StringValue,
            Value.KindOneofCase.BoolValue => value.BoolValue,
            Value.KindOneofCase.StructValue => ConvertFromStruct(value.StructValue),
            Value.KindOneofCase.ListValue => value.ListValue.Values.Select(ExtractValue).ToList(),
            _ => null
        };
    }

    private class StructMarshaller() : Marshaller<Struct>(@struct => @struct.ToByteArray(), message =>
    {
        var result = new Struct();
        result.MergeFrom(message);
        return result;
    });
}