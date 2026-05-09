using ASC.Model.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ASC.Web.Areas.ServiceRequests.Models
{
    public class ServiceRequestDetailsViewModel
    {
        public ServiceRequest ServiceRequest { get; set; } = new ServiceRequest();
        public List<SelectListItem> ServiceEngineers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public string? SelectedServiceEngineer { get; set; }
        public string? SelectedStatus { get; set; }
    }
}
