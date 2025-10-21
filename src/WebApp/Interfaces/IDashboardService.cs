using System.Threading.Tasks;
using WebApp.Areas.Admin.ViewModels.Dashboard;

namespace WebApp.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardViewModelAsync();
    }
}