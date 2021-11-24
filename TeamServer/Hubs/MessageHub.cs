using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using TeamServer.Interfaces;

namespace TeamServer.Hubs
{
    [Authorize]
    public class MessageHub : Hub<IMessageHub>
    {
        // methods defined in IMessageHub
    }
}