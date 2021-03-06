﻿using Autofac.Multitenant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hangfire.AspNetCore.Multitenant.Request.IdentificationStrategies
{
    public sealed class DynamicTenantIdentificationService : ITenantIdentificationStrategy
    {
        private readonly Func<IHostingEnvironment, HttpContext, HangfireTenant> _currentTenant;
        private readonly Func<IHostingEnvironment, IEnumerable<HangfireTenant>> _allTenants;
        private readonly IHostingEnvironment _hostingEnvironment;

        private readonly ILogger<ITenantIdentificationStrategy> _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public DynamicTenantIdentificationService(IHttpContextAccessor contextAccessor, IHostingEnvironment hostingEnvironment, ILogger<ITenantIdentificationStrategy> logger, Func<IHostingEnvironment, HttpContext, HangfireTenant> currentTenant, Func<IHostingEnvironment, IEnumerable<HangfireTenant>> allTenants)
        {
            if (currentTenant == null)
            {
                throw new ArgumentNullException(nameof(currentTenant));
            }

            if (allTenants == null)
            {
                throw new ArgumentNullException(nameof(allTenants));
            }
            _contextAccessor = contextAccessor;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;

            this._currentTenant = currentTenant;
            this._allTenants = allTenants;
        }

        public IEnumerable<HangfireTenant> GetAllTenants()
        {
            return this._allTenants(_hostingEnvironment);
        }

        public Task<HangfireTenant> GetTenantAsync(HttpContext httpContext)
        {
            var tenant = this._currentTenant(_hostingEnvironment, httpContext);

            return Task.FromResult(tenant);
        }

        public bool TryIdentifyTenant(out object tenantId)
        {
            var httpContext = _contextAccessor.HttpContext;

            var tenant = GetTenantAsync(httpContext).GetAwaiter().GetResult();
            if (tenant != null)
            {
                tenantId = tenant.Id;
                return true;
            }

            tenantId = null;
            return false;
        }
    }
}
