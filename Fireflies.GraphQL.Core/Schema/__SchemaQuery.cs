﻿using Fireflies.GraphQL.Abstractions;

namespace Fireflies.GraphQL.Core.Schema;

// ReSharper disable once InconsistentNaming
public class __SchemaQuery {
    private readonly __Schema _schema;

    public __SchemaQuery(__Schema schema) {
        _schema = schema;
    }

    [GraphQLQuery]
    public __Schema __Schema() {
        return _schema;
    }
}