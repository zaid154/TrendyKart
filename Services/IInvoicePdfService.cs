using System.Threading.Tasks;
using TrendyKart.Models;

namespace TrendyKart.Services
{
    public interface IInvoicePdfService
    {
        string GenerateInvoiceHtml(Order order, SiteSetting siteSetting);
        Task SendInvoiceEmailAsync(Order order, SiteSetting siteSetting);
    }
}
