using FactorioWebInterface.Data;
using FactorioWebInterface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace FactorioWebInterface.Hubs
{
    [Authorize]
    public class ScenarioDataHub : Hub<IScenarioDataClientMethods>
    {
        private IFactorioServerManager _factorioServerManager;

        public ScenarioDataHub(IFactorioServerManager factorioServerManager)
        {
            _factorioServerManager = factorioServerManager;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue(connectionId, out object oldDataSet))
            {
                Groups.RemoveFromGroupAsync(connectionId, (string)oldDataSet);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task TrackDataSet(string dataSet)
        {
            string connectionId = Context.ConnectionId;

            if (Context.Items.TryGetValue(connectionId, out object oldDataSet))
            {
                await Groups.RemoveFromGroupAsync(connectionId, (string)oldDataSet);
            }

            Context.Items[connectionId] = dataSet;
            await Groups.AddToGroupAsync(connectionId, dataSet);
        }

        public Task<string[]> GetAllDataSets()
        {
            return _factorioServerManager.GetAllScenarioDataSets();
        }

        public Task RequestAllData()
        {
            var client = Clients.Client(Context.ConnectionId);

            _ = Task.Run(async () =>
            {
                var data = await _factorioServerManager.GetAllScenarioData();
                _ = client.SendAllEntries(data);
            });

            return Task.FromResult(0);
        }

        public Task RequestAllDataForDataSet(string dataSet)
        {
            var client = Clients.Client(Context.ConnectionId);

            _ = Task.Run(async () =>
            {
                var data = await _factorioServerManager.GetScenarioData(dataSet);
                _ = client.SendAllEntriesForDataSet(dataSet, data);
            });

            return Task.FromResult(0);
        }

        public Task UpdateData(ScenarioDataEntry data)
        {
            return _factorioServerManager.UpdateScenarioDataFromWeb(data);
        }
    }
}
