using ASC.Business.Interfaces;
using ASC.Model.Models;
using ASC.Utilities;
using ASC.Web.Areas.Configuration.Models;
using ASC.Web.Controllers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // GET: /Configuration/MasterData/MasterKeys
        [HttpGet]
        public async Task<IActionResult> MasterKeys()
        {
            var masterKeys = await _masterData.GetAllMasterKeysAsync();

            var masterKeysViewModel =
                _mapper.Map<List<MasterDataKeyViewModel>>(masterKeys);

            return View(new MasterKeysViewModel
            {
                MasterKeys = masterKeysViewModel,
                MasterKeyInContext = new MasterDataKeyViewModel(),
                IsEdit = false
            });
        }

        // POST: /Configuration/MasterData/MasterKeys
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterKeys(MasterKeysViewModel model)
        {
            if (model.MasterKeyInContext == null)
            {
                return Json("Error");
            }

            var masterDataKey =
                _mapper.Map<MasterDataKeyViewModel, MasterDataKey>(model.MasterKeyInContext);

            if (model.IsEdit)
            {
                await _masterData.UpdateMasterKeyAsync(
                    masterDataKey.PartitionKey,
                    masterDataKey);
            }
            else
            {
                masterDataKey.PartitionKey = masterDataKey.Name;
                masterDataKey.RowKey = Guid.NewGuid().ToString();
                masterDataKey.CreatedBy =
                    HttpContext.User.Identity?.Name ?? "System";
                masterDataKey.IsActive = true;

                await _masterData.InsertMasterKeyAsync(masterDataKey);
            }

            return RedirectToAction("MasterKeys");
        }

        // GET: /Configuration/MasterData/MasterValues
        [HttpGet]
        public async Task<IActionResult> MasterValues()
        {
            ViewBag.MasterKeys = await _masterData.GetAllMasterKeysAsync();

            return View(new MasterValuesViewModel
            {
                MasterValues = new List<MasterDataValueViewModel>(),
                IsEdit = false
            });
        }

        // GET: /Configuration/MasterData/MasterValuesByKey?key=...
        [HttpGet]
        public async Task<IActionResult> MasterValuesByKey(string key)
        {
            var masterValues = await _masterData.GetAllMasterValuesByKeyAsync(key);

            return Json(new
            {
                data = masterValues
            });
        }

        // POST: /Configuration/MasterData/MasterValues
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MasterValues(bool isEdit, MasterDataValueViewModel masterValue)
        {
            if (!ModelState.IsValid)
            {
                return Json("Error");
            }

            var masterDataValue =
                _mapper.Map<MasterDataValueViewModel, MasterDataValue>(masterValue);

            if (isEdit)
            {
                await _masterData.UpdateMasterValueAsync(
                    masterDataValue.PartitionKey,
                    masterDataValue.RowKey,
                    masterDataValue);
            }
            else
            {
                masterDataValue.RowKey = Guid.NewGuid().ToString();

                masterDataValue.CreatedBy =
                    HttpContext.User.GetCurrentUserDetails()?.Name
                    ?? HttpContext.User.Identity?.Name
                    ?? "System";

                await _masterData.InsertMasterValueAsync(masterDataValue);
            }

            return Json(true);
        }

        private async Task<List<MasterDataValue>> ParseMasterDataExcel(IFormFile excelFile)
        {
            var masterValueList = new List<MasterDataValue>();

            using (var memoryStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(memoryStream);

                memoryStream.Position = 0;

                ExcelPackage.License.SetNonCommercialPersonal("ASC Lab");

                using (var package = new ExcelPackage(memoryStream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    if (worksheet.Dimension == null)
                    {
                        return masterValueList;
                    }

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var partitionKey = worksheet.Cells[row, 1].Text;
                        var name = worksheet.Cells[row, 2].Text;
                        var isActiveText = worksheet.Cells[row, 3].Text;

                        if (string.IsNullOrWhiteSpace(partitionKey) ||
                            string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        bool isActive = true;

                        if (!string.IsNullOrWhiteSpace(isActiveText))
                        {
                            bool.TryParse(isActiveText, out isActive);
                        }

                        var masterDataValue = new MasterDataValue
                        {
                            RowKey = Guid.NewGuid().ToString(),
                            PartitionKey = partitionKey,
                            Name = name,
                            IsActive = isActive,
                            CreatedBy = HttpContext.User.Identity?.Name ?? "System"
                        };

                        masterValueList.Add(masterDataValue);
                    }
                }
            }

            return masterValueList;
        }

        // POST: /Configuration/MasterData/UploadExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel()
        {
            var files = Request.Form.Files;

            if (!files.Any())
            {
                return Json(new { Error = true, Text = "Upload a file" });
            }

            var excelFile = files.First();

            if (excelFile.Length <= 0)
            {
                return Json(new { Error = true, Text = "Upload a file" });
            }

            var masterData = await ParseMasterDataExcel(excelFile);

            var result = await _masterData.UploadBulkMasterData(masterData);

            return Json(new { Success = result });
        }
    }
}