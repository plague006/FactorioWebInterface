using FactorioWebInterface.Data;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public interface IFactorioBanClientMethods
    {
        Task SendAddBan(Ban ban);
        Task SendRemoveBan(string username);
    }
}
