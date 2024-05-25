using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SignalRServer.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + 
        CookieAuthenticationDefaults.AuthenticationScheme, Policy ="BasicAuth")]
    public class LearningHub : Hub<ILearningHubClient>
    {
        public async Task BroadcastMessage(string message)
        {
            await Clients.All.ReceiveMessage(GetMessageToSend(message));
        }
        public async Task BroadcastStream(IAsyncEnumerable<string> stream)
        {// with IAsyncEnumerabe we can insert into the collection while reading from it 
         // we use await foreach because we wait for items to be added to the collection
            await foreach(string item in stream)
            {
                await Clients.Caller.ReceiveMessage($"Server received {item}");
            }
        }
        public async IAsyncEnumerable<string> TriggerStream(
            int jobsCount,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        )
        {
            for(int i=0; i < jobsCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return $"Job {i} executed successfully";
                await Task.Delay(1000, cancellationToken);
            }
        }
        public async Task SendToOthers(string message)
        {
            await Clients.Others.ReceiveMessage(GetMessageToSend(message));
        }
        public async Task SendToSelf(string message)
        {
            await Clients.Caller.ReceiveMessage(GetMessageToSend(message));
        }
        public async Task SendToIndividual(string connectionId, string message)
        {
            await Clients.Client(connectionId).ReceiveMessage(GetMessageToSend(message));
            //await Clients.Clients(connectionIds).ReceiveMessage(GetMessageToSend(message));
        }
        [Authorize(Roles = "user")]
        public async Task SendToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).ReceiveMessage(GetMessageToSend(message));
        }
        [Authorize("AdminOnly")] 
        public async Task AddUserToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.ReceiveMessage($"Current user added to {groupName} group!");
            await Clients.Others.ReceiveMessage($"{Context.ConnectionId} added to {groupName} group!");
        }
        public async Task RemoveUserFromGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Caller.ReceiveMessage($"Current user removed from {groupName} group!");
            await Clients.Others.ReceiveMessage($"User {Context.ConnectionId} removed from {groupName} group!");
        }
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
        private string GetMessageToSend(string originalMessage)
        {
            return $"User connection id: {Context.ConnectionId}. Message: {originalMessage}";
        }
    }
    

}