using System.Reflection;
using Fireflies.GraphQL.Core.Exceptions;
using Fireflies.GraphQL.Core.Extensions;

namespace Fireflies.GraphQL.Core;

internal class SchemaValidator {
    private readonly IEnumerable<OperationDescriptor> _operations;
    private readonly Dictionary<string, Type> _verifiedTyped = new();

    public SchemaValidator(IEnumerable<OperationDescriptor> operations) {
        _operations = operations;
    }

    public void Validate() {
        foreach(var operation in _operations) {
            var returnType = operation.Method.DiscardTaskFromReturnType();
            InspectType(returnType);
        }
    }

    private void InspectType(Type type) {
        if(type.IsCollection(out var elementType)) {
            type = elementType;
        }

        if(Type.GetTypeCode(type) != TypeCode.Object)
            return;

        if(type == typeof(void))
            return;

        var graphQLName = type.GraphQLName();
        if(_verifiedTyped.TryGetValue(graphQLName, out var existingType)) {
            if(existingType != type) {
                throw new DuplicateNameException($"{type.Name} is used for more than one type");
            }

            return;
        }

        _verifiedTyped.Add(graphQLName, type);

        foreach(var subType in type.GetAllGraphQLMemberInfo()) {
            Type typeToInspect;
            if(subType is PropertyInfo propertyInfo)
                typeToInspect = propertyInfo.PropertyType;
            else if(subType is MethodInfo methodInfo)
                typeToInspect = methodInfo.ReturnType;
            else
                throw new ArgumentOutOfRangeException(nameof(subType));

            typeToInspect = typeToInspect.DiscardTask();
            typeToInspect = Nullable.GetUnderlyingType(typeToInspect) ?? typeToInspect;

            InspectType(typeToInspect);
        }
    }
}