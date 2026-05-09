using ASC.Business.Interfaces;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.Configuration.Models;
using ASC.Web.Controllers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;

namespace ASC.Web.Areas.Configuration.Controllers
{
    [Area("Configuration")]
    [Authorize(Roles = "Admin")]
    public class MasterDataController : BaseController
    {
        private readonly IMasterDataOperations _masterData;
        private readonly IMapper _mapper;

        public MasterDataController(IMasterDataOperations masterData, IMapper mapper)
        {
            _masterData = masterData;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();
            var masterKeysViewModel = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);

            // Hold all Master Keys in session
            HttpContext.Session.SetSession("MasterKeys", masterKeysViewModel);

            return View(new MasterKeysViewModel
            {
                MasterKeys = masterKeysViewModel == null ? null : masterKeysViewModel.ToList(),
                MasterKeyInContext = new MasterDataKeyViewModel(),
                IsEdit = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel model)
        {
            var input = model.MasterKeyInContext;
            var currentUser = HttpContext.User.Identity?.Name ?? "System";

            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                ModelState.AddModelError("MasterKeyInContext.Name", "Name is required.");

                var masterKeys = await _masterData.GetAllMasterKeysAsync();
                var masterKeysViewModel = _mapper.Map<List<MasterDataKey>, List<MasterDataKeyViewModel>>(masterKeys);
                model.MasterKeys = masterKeysViewModel?.ToList();
                model.MasterKeyInContext ??= new MasterDataKeyViewModel();
                return View(model);
            }

            var masterDataKey = new MasterDataKey
            {
                RowKey = input.RowKey,
                PartitionKey = input.PartitionKey,
                Name = input.Name.Trim(),
                IsActive = input.IsActive
            };

            if (model.IsEdit)
            {
                masterDataKey.UpdatedBy = currentUser;
                await _masterData.UpdateMasterKeyAsync(masterDataKey.PartitionKey, masterDataKey);
            }
            else
            {
                masterDataKey.RowKey = Guid.NewGuid().ToString();
                masterDataKey.PartitionKey = masterDataKey.Name;
                masterDataKey.CreatedBy = currentUser;
                masterDataKey.UpdatedBy = currentUser;
                await _masterData.InsertMasterKeyAsync(masterDataKey);
            }

            return RedirectToAction("MasterKeys", "MasterData", new { area = "Configuration" });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            // Get All Master Keys and hold them in ViewBag for Select tag
            var masterKeys = await _masterData.GetAllMasterKeysAsync() ?? new List<MasterDataKey>();

            ViewBag.MasterKeys = masterKeys
                .GroupBy(k => k.PartitionKey)
                .Select(g => g.First())
                .OrderBy(k => k.PartitionKey)
                .ToList();

            ViewBag.MasterKeyItems = ViewBag.MasterKeys;
            ViewBag.MasterKeysJson = ViewBag.MasterKeys;

            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false
            });
        }

        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            // Get Master values based on master key.
            if (string.IsNullOrWhiteSpace(key) || key == "--Select--")
            {
                return Json(new { data = new List<MasterDataValue>() });
            }

            var masterValues = await _masterData.GetAllMasterValuesByKeyAsync(key.Trim());
            return Json(new { data = masterValues ?? new List<MasterDataValue>() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            if (!ModelState.IsValid)
            {
                return Json("Error");
            }

            var masterDataValue = _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);
            var currentUser = HttpContext.User.GetCurrentUserDetails()?.Name
                              ?? HttpContext.User.Identity?.Name
                              ?? "System";

            if (isEdit)
            {
                // Update Master Value
                masterDataValue.UpdatedBy = currentUser;
                await _masterData.UpdateMasterValueAsync(masterDataValue.PartitionKey, masterDataValue.RowKey, masterDataValue);
            }
            else
            {
                // Insert Master Value
                masterDataValue.RowKey = Guid.NewGuid().ToString();
                masterDataValue.CreatedBy = currentUser;
                masterDataValue.UpdatedBy = currentUser;
                await _masterData.InsertMasterValueAsync(masterDataValue);
            }

            return Json(true);
        }

        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            var masterValueList = new List<MasterDataValue>();

            using (var memoryStream = new MemoryStream())
            {
                // Get MemoryStream from Excel file
                await excelFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Create a ExcelPackage object from MemoryStream
                ExcelPackage.License.SetNonCommercialPersonal("ASC Lab");
                using (ExcelPackage package = new ExcelPackage(memoryStream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        return masterValueList;
                    }

                    // Get the first Excel sheet from the Workbook
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    if (worksheet.Dimension == null)
                    {
                        return masterValueList;
                    }

                    int rowCount = worksheet.Dimension.Rows;
                    var currentUser = HttpContext.User.Identity?.Name ?? "System";

                    // Iterate all the rows and create the list of MasterDataValue
                    // Ignore first row as it is header
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var partitionKey = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var name = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var isActiveText = worksheet.Cells[row, 3].Value?.ToString()?.Trim();

                        if (string.IsNullOrWhiteSpace(partitionKey) || string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        var masterDataValue = new MasterDataValue();
                        masterDataValue.RowKey = Guid.NewGuid().ToString();
                        masterDataValue.PartitionKey = partitionKey;
                        masterDataValue.Name = name;
                        masterDataValue.IsActive = string.IsNullOrWhiteSpace(isActiveText)
                            || isActiveText.Equals("true", StringComparison.OrdinalIgnoreCase)
                            || isActiveText.Equals("1", StringComparison.OrdinalIgnoreCase)
                            || isActiveText.Equals("yes", StringComparison.OrdinalIgnoreCase)
                            || isActiveText.Equals("y", StringComparison.OrdinalIgnoreCase)
                            || isActiveText.Equals("active", StringComparison.OrdinalIgnoreCase);
                        masterDataValue.CreatedBy = currentUser;
                        masterDataValue.UpdatedBy = currentUser;
                        masterValueList.Add(masterDataValue);
                    }
                }
            }

            return masterValueList;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel()
        {
            try
            {
                var files = Request.Form.Files;

                // Validations
                if (!files.Any())
                {
                    return Json(new { Error = true, Text = "Upload a file" });
                }

                var excelFile = files.First();
                if (excelFile.Length <= 0)
                {
                    return Json(new { Error = true, Text = "Upload a file" });
                }

                // Parse Excel Data
                var masterData = await ParseMasterDataExcel(excelFile);
                var result = await _masterData.UploadBulkMasterData(masterData);
                return Json(new { Success = result });
            }
            catch (Exception ex)
            {
                return Json(new { Error = true, Text = ex.Message });
            }
        }
    }
}
