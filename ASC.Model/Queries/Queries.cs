using ASC.Model.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ASC.Model.Queries
{
    public static class Queries
    {
        public static Expression<Func<ServiceRequest, bool>> GetDashboardQuery(
            DateTime? requestedDate,
            List<string>? status = null,
            string? email = "",
            string? serviceEngineerEmail = "")
        {
            var query = (Expression<Func<ServiceRequest, bool>>)(u => true);

            if (requestedDate.HasValue)
            {
                var requestedDateValue = requestedDate.Value.Date;

                var requestedDateFilter =
                    (Expression<Func<ServiceRequest, bool>>)
                    (u => u.RequestedDate >= requestedDateValue);

                query = query.And(requestedDateFilter);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailFilter =
                    (Expression<Func<ServiceRequest, bool>>)
                    (u => u.PartitionKey == email);

                query = query.And(emailFilter);
            }

            if (!string.IsNullOrWhiteSpace(serviceEngineerEmail))
            {
                var serviceEngineerFilter =
                    (Expression<Func<ServiceRequest, bool>>)
                    (u => u.ServiceEngineer == serviceEngineerEmail);

                query = query.And(serviceEngineerFilter);
            }

            var statusQueries =
                (Expression<Func<ServiceRequest, bool>>)(u => false);

            if (status != null && status.Count > 0)
            {
                foreach (var state in status)
                {
                    var statusFilter =
                        (Expression<Func<ServiceRequest, bool>>)
                        (u => u.Status == state);

                    statusQueries = statusQueries.Or(statusFilter);
                }

                query = query.And(statusQueries);
            }

            return query;
        }
    }
}