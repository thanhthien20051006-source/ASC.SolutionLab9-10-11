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
            var masterKeys = await _unitOfWork
                .Repository<MasterDataKey>()
                .FindAllAsync();

            return masterKeys.ToList();
        }

        public async Task<List<MasterDataKey>> GetMaserKeyByNameAsync(string name)
        {
            var masterKeys = await _unitOfWork
                .Repository<MasterDataKey>()
                .FindAllByPartitionKeyAsync(name);

            return masterKeys.ToList();
        }

        public async Task<bool> InsertMasterKeyAsync(MasterDataKey key)
        {
            await _unitOfWork
                .Repository<MasterDataKey>()
                .AddAsync(key);

            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<bool> UpdateMasterKeyAsync(string originalPartitionKey, MasterDataKey key)
        {
            var masterKey = await _unitOfWork
                .Repository<MasterDataKey>()
                .FindAsync(originalPartitionKey, key.RowKey);

            if (masterKey == null)
            {
                return false;
            }

            masterKey.Name = key.Name;
            masterKey.IsActive = key.IsActive;
            masterKey.IsDeleted = key.IsDeleted;

            _unitOfWork
                .Repository<MasterDataKey>()
                .Update(masterKey);

            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesByKeyAsync(string key)
        {
            var masterValues = await _unitOfWork
                .Repository<MasterDataValue>()
                .FindAllByPartitionKeyAsync(key);

            return masterValues.ToList();
        }

        public async Task<List<MasterDataValue>> GetAllMasterValuesAsync()
        {
            var masterValues = await _unitOfWork
                .Repository<MasterDataValue>()
                .FindAllAsync();

            return masterValues.ToList();
        }

        public async Task<MasterDataValue> GetMasterValueByNameAsync(string key, string name)
        {
            var masterValue = await _unitOfWork
                .Repository<MasterDataValue>()
                .FindAsync(key, name);

            return masterValue;
        }

        public async Task<bool> InsertMasterValueAsync(MasterDataValue value)
        {
            await _unitOfWork
                .Repository<MasterDataValue>()
                .AddAsync(value);

            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<bool> UpdateMasterValueAsync(
            string originalPartitionKey,
            string originalRowKey,
            MasterDataValue value)
        {
            var masterValue = await _unitOfWork
                .Repository<MasterDataValue>()
                .FindAsync(originalPartitionKey, originalRowKey);

            if (masterValue == null)
            {
                return false;
            }

            masterValue.Name = value.Name;
            masterValue.IsActive = value.IsActive;
            masterValue.IsDeleted = value.IsDeleted;

            _unitOfWork
                .Repository<MasterDataValue>()
                .Update(masterValue);

            _unitOfWork.CommitTransaction();

            return true;
        }

        public async Task<bool> UploadBulkMasterData(List<MasterDataValue> values)
        {
            foreach (var value in values)
            {
                var masterKeys = await GetMaserKeyByNameAsync(value.PartitionKey);

                if (!masterKeys.Any())
                {
                    await _unitOfWork.Repository<MasterDataKey>().AddAsync(new MasterDataKey
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = value.PartitionKey,
                        Name = value.PartitionKey,
                        IsActive = true,
                        CreatedBy = value.CreatedBy ?? "System"
                    });
                }

                var masterValuesByKey =
                    await GetAllMasterValuesByKeyAsync(value.PartitionKey);

                var masterValue = masterValuesByKey
                    .FirstOrDefault(p => p.Name == value.Name);

                if (masterValue == null)
                {
                    await _unitOfWork
                        .Repository<MasterDataValue>()
                        .AddAsync(value);
                }
                else
                {
                    masterValue.IsActive = value.IsActive;
                    masterValue.IsDeleted = value.IsDeleted;
                    masterValue.Name = value.Name;

                    _unitOfWork
                        .Repository<MasterDataValue>()
                        .Update(masterValue);
                }
            }

            _unitOfWork.CommitTransaction();

            return true;
        }
    }
}