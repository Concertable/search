using Concertable.Contracts;

namespace Concertable.Search.Application.Params;

public interface IGenreParams
{
    IReadOnlyCollection<Genre> Genres { get; }
}
