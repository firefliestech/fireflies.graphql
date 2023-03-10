using Fireflies.GraphQL.Core.Json;
using Fireflies.IoC.Abstractions;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Exceptions;
using GraphQLParser.Visitors;

namespace Fireflies.GraphQL.Core;

public class GraphQLEngine : ASTVisitor<IGraphQLContext> {
    private readonly GraphQLOptions _options;
    private readonly IDependencyResolver _dependencyResolver;
    private readonly WrapperRegistry _wrapperRegistry;
    private FragmentAccessor _fragmentAccessor = null!;
    private ValueAccessor _valueAccessor = null!;

    public IGraphQLContext Context { get; }

    private DataJsonWriter? _writer;

    public GraphQLEngine(GraphQLOptions options, IDependencyResolver dependencyResolver, IGraphQLContext context, WrapperRegistry wrapperRegistry) {
        _options = options;
        _dependencyResolver = dependencyResolver;
        _wrapperRegistry = wrapperRegistry;
        Context = context;
    }

    public async Task Execute(GraphQLRequest? request) {
        var (graphQLDocument, result) = Parse(request);
        if(result != null) {
            Context.IncreaseExpectedOperations();
            Context.PublishResult(result);
            return;
        }

        _fragmentAccessor = new FragmentAccessor(graphQLDocument!, Context);
        _valueAccessor = new ValueAccessor(request!.Variables, Context);

        var errors = await new RequestValidator(request, _fragmentAccessor, _options, _dependencyResolver, Context, _wrapperRegistry).Validate(graphQLDocument!).ConfigureAwait(false);
        if(errors.Any()) {
            Context.IncreaseExpectedOperations();
            Context.PublishResult(GenerateValidationErrorResult(errors));
        } else {
            _writer = !Context.IsWebSocket ? new DataJsonWriter() : null;
            await VisitAsync(graphQLDocument, Context).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<byte[]> Results() {
        await foreach(var result in Context.WithCancellation(Context.CancellationToken).ConfigureAwait(false)) {
            yield return result;

            if(!Context.IsWebSocket)
                yield break;
        }
    }

    private (GraphQLDocument?, ErrorJsonWriter?) Parse(GraphQLRequest? request) {
        if(request?.Query == null) {
            return (null, GenerateErrorResult("Empty request", "GRAPHQL_SYNTAX_ERROR"));
        }

        try {
            return (Parser.Parse(request.Query, new ParserOptions { Ignore = IgnoreOptions.All }), null);
        } catch(GraphQLSyntaxErrorException sex) {
            return (null, GenerateErrorResult(sex.Description, "GRAPHQL_SYNTAX_ERROR"));
        }
    }

    private ErrorJsonWriter GenerateValidationErrorResult(List<string> errors) {
        var errorWriter = new ErrorJsonWriter();
        foreach(var error in errors)
            GenerateMessage(error, "GRAPHQL_VALIDATION_FAILED", errorWriter);

        return errorWriter;
    }

    private ErrorJsonWriter GenerateErrorResult(string exceptionMessage, string code) {
        var errorWriter = new ErrorJsonWriter();
        GenerateMessage(exceptionMessage, code, errorWriter);
        return errorWriter;
    }

    private static void GenerateMessage(string exceptionMessage, string code, ErrorJsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteValue("message", exceptionMessage, TypeCode.String, typeof(string));

        writer.WriteStartObject("extensions");
        writer.WriteValue("code", code, TypeCode.String, typeof(string));
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    protected override async ValueTask VisitOperationDefinitionAsync(GraphQLOperationDefinition operationDefinition, IGraphQLContext context) {
        if(operationDefinition.Operation is OperationType.Query or OperationType.Mutation)
            context.IncreaseExpectedOperations(operationDefinition.SelectionSet.Selections.Count);

        var visitor = new OperationVisitor(_options, _dependencyResolver, _fragmentAccessor, _valueAccessor, _wrapperRegistry, operationDefinition.Operation, context, _writer);
        await visitor.VisitAsync(operationDefinition, context).ConfigureAwait(false);
    }
}