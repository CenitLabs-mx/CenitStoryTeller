using System;
using CenitStoryTeller.Data;

namespace CenitStoryTeller.Tests.Fakes;

internal sealed class StubCurrentUser : ICurrentUser
{
    public Guid? Id { get; init; }
    public static StubCurrentUser Anonymous() => new();
    public static StubCurrentUser De(Guid userId) => new() { Id = userId };
}
