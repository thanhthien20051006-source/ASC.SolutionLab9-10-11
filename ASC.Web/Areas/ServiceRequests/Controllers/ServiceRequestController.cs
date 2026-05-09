using ASC.Business.Interfaces;
using ASC.Model.BaseTypes;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.ServiceRequests.Models;
using ASC.Web.Controllers;
using ASC.Web.Data;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ASC.Web.Areas.ServiceRequests.Controllers
{
    [Area("ServiceRequests")]
    public class ServiceRequestController : BaseController
    {
        private readonly IServiceRequestOperations _serviceRequestOperations;
        private readonly IMapper _mapper;
        private readonly IMasterDataCacheOperations _masterData;
        private readonly UserManager<IdentityUser> _userManager;

        private const string ServiceRequestViewPath =
            "~/Areas/ServiceRequests/Views/ServiceRequest/ServiceRequest.cshtml";

        public ServiceRequestController(
            IServiceRequestOperations operations,
            IMapper mapper,
            IMasterDataCacheOperations masterData,
            UserManager<IdentityUser> userManager)
        {
            _serviceRequestOperations = operations;
            _mapper = mapper;
            _masterData = masterData;
            _userManager = userManager;
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

        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(rowKey))
            {
                return RedirectToAction("Dashboard", "Dashboard", new { area = "ServiceRequests" });
            }

            var serviceRequest = await _serviceRequestOperations.GetServiceRequestByKeysAsync(partitionKey, rowKey);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            var currentUser = HttpContext.User.GetCurrentUserDetails();
            if (HttpContext.User.IsInRole(Roles.User.ToString()) && serviceRequest.PartitionKey != currentUser.Email)
            {
                return Forbid();
            }

            return View(await BuildDetailsViewModelAsync(serviceRequest));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEngineer(string partitionKey, string rowKey, string selectedServiceEngineer)
        {
            if (!string.IsNullOrWhiteSpace(selectedServiceEngineer))
            {
                await _serviceRequestOperations.AssignServiceEngineerAsync(partitionKey, rowKey, selectedServiceEngineer);
            }

            return RedirectToAction("Details", new { partitionKey, rowKey });
        }

        [Authorize(Roles = "Admin,Engineer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string partitionKey, string rowKey, string selectedStatus)
        {
            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                await _serviceRequestOperations.UpdateServiceRequestStatusAsync(rowKey, partitionKey, selectedStatus);
            }

            return RedirectToAction("Details", new { partitionKey, rowKey });
        }

        private async Task<ServiceRequestDetailsViewModel> BuildDetailsViewModelAsync(ServiceRequest serviceRequest)
        {
            var engineers = await _userManager.GetUsersInRoleAsync(Roles.Engineer.ToString());
            var statuses = Enum.GetNames(typeof(Status));

            return new ServiceRequestDetailsViewModel
            {
                ServiceRequest = serviceRequest,
                SelectedServiceEngineer = serviceRequest.ServiceEngineer,
                SelectedStatus = serviceRequest.Status,
                ServiceEngineers = engineers.Select(e => new SelectListItem
                {
                    Text = string.IsNullOrWhiteSpace(e.UserName) ? e.Email : $"{e.UserName} ({e.Email})",
                    Value = e.Email,
                    Selected = e.Email == serviceRequest.ServiceEngineer
                }).ToList(),
                Statuses = statuses.Select(s => new SelectListItem
                {
                    Text = s,
                    Value = s,
                    Selected = s == serviceRequest.Status
                }).ToList()
            };
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