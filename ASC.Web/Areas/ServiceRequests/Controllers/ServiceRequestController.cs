using ASC.Business.Interfaces;
using ASC.Model.BaseTypes;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.ServiceRequests.Models;
using ASC.Web.Controllers;
using ASC.Web.Data;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace ASC.Web.Areas.ServiceRequests.Controllers
{
    [Area("ServiceRequests")]
    public class ServiceRequestController : BaseController
    {
        private readonly IServiceRequestOperations _serviceRequestOperations;
        private readonly IMapper _mapper;
        private readonly IMasterDataCacheOperations _masterData;

        private const string ServiceRequestViewPath =
            "~/Areas/ServiceRequests/Views/ServiceRequest/ServiceRequest.cshtml";

        public ServiceRequestController(
            IServiceRequestOperations operations,
            IMapper mapper,
            IMasterDataCacheOperations masterData)
        {
            _serviceRequestOperations = operations;
            _mapper = mapper;
            _masterData = masterData;
        }

        [HttpGet]
        public async Task<IActionResult> ServiceRequest()
        {
            await LoadMasterDataDropdownsAsync();

            return View(ServiceRequestViewPath, new NewServiceRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceRequest(NewServiceRequestViewModel request)
        {
            if (!ModelState.IsValid)
            {
                await LoadMasterDataDropdownsAsync();

                return View(ServiceRequestViewPath, request);
            }

            var serviceRequest =
                _mapper.Map<NewServiceRequestViewModel, ServiceRequest>(request);

            serviceRequest.PartitionKey = HttpContext.User.GetCurrentUserDetails().Email;
            serviceRequest.RowKey = Guid.NewGuid().ToString();
            serviceRequest.RequestedDate = request.RequestedDate;
            serviceRequest.Status = Status.New.ToString();

            await _serviceRequestOperations.CreateServiceRequestAsync(serviceRequest);

            return RedirectToAction("Dashboard", "Dashboard", new { area = "ServiceRequests" });
        }

        private async Task LoadMasterDataDropdownsAsync()
        {
            var masterData = await _masterData.GetMasterDataCacheAsync();

            ViewBag.VehicleTypes = masterData.Values
                .Where(p => p.PartitionKey == MasterKeys.VehicleType.ToString() && p.IsActive)
                .ToList();

            ViewBag.VehicleNames = masterData.Values
                .Where(p => p.PartitionKey == MasterKeys.VehicleName.ToString() && p.IsActive)
                .ToList();
        }
    }
}