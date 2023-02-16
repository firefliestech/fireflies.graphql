﻿namespace Fireflies.GraphQL.Contract;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class)]
public class GraphQLDeprecatedAttribute : GraphQLAttribute {
    public string Reason { get; private set; }

    public GraphQLDeprecatedAttribute(string reason) {
        Reason = reason;
    }
}