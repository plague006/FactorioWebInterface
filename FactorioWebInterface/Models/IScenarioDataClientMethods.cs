using FactorioWebInterface.Data;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IScenarioDataClientMethods
    {
        Task SendAllEntries(ScenarioDataEntry[] data);
        Task SendAllEntriesForDataSet(string dataSet, ScenarioDataEntry[] data);
        Task SendEntry(ScenarioDataEntry data);
    }
}
