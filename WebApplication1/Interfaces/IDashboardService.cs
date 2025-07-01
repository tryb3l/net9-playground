using System.Threading.Tasks;
using WebApplication1.Areas.Admin.ViewModels.Dashboard;

namespace WebApplication1.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardViewModelAsync();
    }
}