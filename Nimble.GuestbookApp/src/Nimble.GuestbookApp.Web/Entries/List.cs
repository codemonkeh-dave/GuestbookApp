using FastEndpoints;
using MediatR;
using Nimble.GuestbookApp.UseCases.Entries;

namespace Nimble.GuestbookApp.Web.Entries;

public class List : EndpointWithoutRequest<EntryListResponse>
{
  private readonly IMediator _mediator;

  public List(IMediator mediator)
  {
    _mediator = mediator;
  }
  public override void Configure()
  {
    Get("/Entries");
    AllowAnonymous();
  }

  public override async Task HandleAsync(CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new ListEntriesQuery(null, null));

    if (result.IsSuccess)
    {
      Response = new EntryListResponse
      {
        Entries = result.Value.Select(e => new EntryRecord(e.Id, e.EmailAddress,
e.Message, e.DateTimeCreated)).ToList()
      };
    }
  }
}
