using ASC.Business.Interfaces;
using ASC.DataAccess;
using ASC.Model.Models;
namespace ASC.Business
{
    public class MasterDataOperations : IMasterDataOperations
    {
        private readonly IUnitOfWork _unitOfWork;

        public MasterDataOperations(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<MasterDataKey>> GetAllMasterKeysAsync()
        {
            var masterKeys = await _unitOfWork.Repository<MasterDataKey>().FindAllAsync();
            return masterKeys.ToList();
        }

        public async Task<List<MasterDataKey>> GetMaserKeyByNameAsync(string name)
        {
            var masterKeys = await _unitOfWork.Repository<MasterDataKey>().FindAllByPartitionKeyAsync(name);
            return masterKeys.ToList();
        }
        public async Task<bool> InsertMasterKeyAsync(MasterDataKey key)
        {
            using (_unitOfWork)
            {
                await _unitOfWork.Repository<MasterDataKey>().AddAsync(key);
                _unitOfWork.CommitTransaction();
                return true;
            }
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesByKeyAsync(string key)
        {
            try
            {
                var masterKeys = await _unitOfWork.Repository<MasterDataValue>().FindAllByPartitionKeyAsync(key);
                return masterKeys.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public async Task<MasterDataValue> GetMasterValueByNameAsync(string key, string name)
        {
            var masterValues = await _unitOfWork.Repository<MasterDataValue>().
                FindAsync(key, name);
            return masterValues;
        }
        public async Task<bool> InsertMasterValueAsync(MasterDataValue value)
        {
            using (_unitOfWork)
            {
                await _unitOfWork.Repository<MasterDataValue>().AddAsync(value);
                _unitOfWork.CommitTransaction();
                return true;
            }
        }

        public async Task<bool> UpdateMasterKeyAsync(string orginalPartitionKey, MasterDataKey key)
        {
            using (_unitOfWork)
            {
                var masterKey = await _unitOfWork.Repository<MasterDataKey>().
                FindAsync(orginalPartitionKey, key.RowKey);
                masterKey.IsActive = key.IsActive;
                masterKey.IsDeleted = key.IsDeleted;
                masterKey.Name = key.Name;
                masterKey.UpdatedBy = string.IsNullOrWhiteSpace(key.UpdatedBy) ? masterKey.UpdatedBy : key.UpdatedBy;
                _unitOfWork.Repository<MasterDataKey>().Update(masterKey);
                _unitOfWork.CommitTransaction();
                return true;
            }
        }
        public async Task<bool> UpdateMasterValueAsync(string originalPartitionKey, string originalRowKey, MasterDataValue value)
        {
            using (_unitOfWork)
            {
                var masterValue = await _unitOfWork.Repository<MasterDataValue>().
                FindAsync(originalPartitionKey, originalRowKey);
                masterValue.IsActive = value.IsActive;
                masterValue.IsDeleted = value.IsDeleted;
                masterValue.Name = value.Name;
                masterValue.UpdatedBy = string.IsNullOrWhiteSpace(value.UpdatedBy) ? masterValue.UpdatedBy : value.UpdatedBy;
                _unitOfWork.Repository<MasterDataValue>().Update(masterValue);
                _unitOfWork.CommitTransaction();
                return true;
            }
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesAsync()
        {
            var masterValues = await _unitOfWork.Repository<MasterDataValue>().
            FindAllAsync();
            return masterValues.ToList();
        }

        public async Task<bool> UploadBulkMasterData(List<MasterDataValue> values)
        {
            using (_unitOfWork)
            {
                foreach (var value in values)
                {
                    value.CreatedBy = string.IsNullOrWhiteSpace(value.CreatedBy) ? "System" : value.CreatedBy;
                    value.UpdatedBy = string.IsNullOrWhiteSpace(value.UpdatedBy) ? value.CreatedBy : value.UpdatedBy;

                    // Find, if null insert MasterKey
                    var masterKey = await _unitOfWork.Repository<MasterDataKey>().FindAllByPartitionKeyAsync(value.PartitionKey);
                    if (!masterKey.Any())
                    {
                        await _unitOfWork.Repository<MasterDataKey>().AddAsync(new MasterDataKey()
                        {
                            Name = value.PartitionKey,
                            RowKey = Guid.NewGuid().ToString(),
                            PartitionKey = value.PartitionKey,
                            IsActive = true,
                            CreatedBy = value.CreatedBy,
                            UpdatedBy = value.UpdatedBy
                        });
                    }

                    // Find, if null Insert MasterValue
                    var masterValuesByKey = await _unitOfWork.Repository<MasterDataValue>().FindAllByPartitionKeyAsync(value.PartitionKey);
                    var masterValue = masterValuesByKey.FirstOrDefault(p => p.Name == value.Name);
                    if (masterValue == null)
                    {
                        await _unitOfWork.Repository<MasterDataValue>().AddAsync(value);
                    }
                    else
                    {
                        masterValue.IsActive = value.IsActive;
                        masterValue.IsDeleted = value.IsDeleted;
                        masterValue.Name = value.Name;
                        masterValue.UpdatedBy = value.UpdatedBy;
                        _unitOfWork.Repository<MasterDataValue>().Update(masterValue);
                    }
                }
                _unitOfWork.CommitTransaction();
                return true;
            }
        }
    }
}