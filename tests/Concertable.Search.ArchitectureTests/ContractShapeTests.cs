using System.Collections;
using System.Reflection;
using Concertable.Contracts;
using Concertable.Search.Application;
using Concertable.Search.Infrastructure.Repositories;

namespace Concertable.Search.ArchitectureTests;

public sealed class ContractShapeTests
{
    [Fact]
    public void SearchOperationMethods_CollectionPayloadsUseReadOnlyLists()
    {
        var violations = GetOperationMethods()
            .Where(method => !IsAllowedOperationReturnType(method.ReturnType))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}: {method.ReturnType}")
            .Order()
            .ToArray();

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData(typeof(Task<IReadOnlyList<string>>), true)]
    [InlineData(typeof(Task<IPagination<string>>), true)]
    [InlineData(typeof(Task<string>), true)]
    [InlineData(typeof(Task<IEnumerable<string>>), false)]
    [InlineData(typeof(Task<List<string>>), false)]
    [InlineData(typeof(Task<string[]>), false)]
    public void IsAllowedOperationReturnType_RepresentativeShape_ReturnsExpected(Type returnType, bool expected)
    {
        var result = IsAllowedOperationReturnType(returnType);

        Assert.Equal(expected, result);
    }

    private static IEnumerable<MethodInfo> GetOperationMethods()
    {
        var applicationAssembly = typeof(HeaderDispatcher).Assembly;
        var infrastructureAssembly = typeof(ConcertHeaderRepository).Assembly;

        return applicationAssembly
            .GetTypes()
            .Concat(infrastructureAssembly.GetTypes())
            .Where(IsOperationOwner)
            .SelectMany(type => type.GetMethods(
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => !method.IsSpecialName);
    }

    private static bool IsOperationOwner(Type type) =>
        type == typeof(HeaderDispatcher)
        || type.Namespace == "Concertable.Search.Application.Interfaces"
        || type.Namespace == "Concertable.Search.Application.Services"
        || type.Namespace == "Concertable.Search.Infrastructure.Repositories";

    private static bool IsAllowedOperationReturnType(Type returnType)
    {
        var payloadType = UnwrapTask(returnType);

        if (IsPagination(payloadType) || !IsCollection(payloadType))
            return true;

        return payloadType.IsGenericType
            && payloadType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
    }

    private static Type UnwrapTask(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)
            ? type.GetGenericArguments()[0]
            : type;

    private static bool IsPagination(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPagination<>);

    private static bool IsCollection(Type type) =>
        type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}
