namespace Concertable.Search.Application.Interfaces;

internal interface IAutocompleteServiceFactory
{
    IAutocompleteService Create(HeaderType? type);
}
