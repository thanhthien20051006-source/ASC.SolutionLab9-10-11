using ASC.Business;
using ASC.Business.Interfaces;
using ASC.DataAccess;
using ASC.Solution.Services;
using ASC.Web.Configuration;
using ASC.Web.Data;
using ASC.Web.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ASC.Web.Services
{
    public static class DependencyInjection
    {
        //Config services
        public static IServiceCollection AddCongfig(this IServiceCollection services, IConfiguration config)
        {

            // Add AddDbContext with connectionString to mirage database
            var connectionString = config.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            //Add Options  and get data from  appsettings.json with "AppSettings"
            services.AddOptions();// IOption
            services.Configure<ApplicationSettings>(config.GetSection("AppSettings"));

            //Using a Gmail Authentication Provider for Customer Authentication
            services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = config["AppSettings:Google:Identity:ClientId"];
        options.ClientSecret = config["AppSettings:Google:Identity:ClientSecret"];
    });
            //services.AddDistributedMemoryCache();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = config.GetSection("CacheSettings:CacheConnectionString").Value;
                options.InstanceName = config.GetSection("CacheSettings:CacheInstance").Value;
            });
            return services;
        }

        //Add service
        public static IServiceCollection AddMyDependencyGroup(this IServiceCollection services)
        {
            //Add ApplicationDbContext
            services.AddScoped<DbContext, ApplicationDbContext>();

            //Add IdentityUser IdentityUser
            services.AddIdentity<IdentityUser, IdentityRole>((options) =>
            {
                options.User.RequireUniqueEmail = true;

            }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            //Add services
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.AddSingleton<IIdentitySeed, IdentitySeed>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Add Cache, Session
            services.AddSession();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddDistributedMemoryCache();
            services.AddSingleton<INavigationCacheOperations, NavigationCacheOperations>();
            //
            services.AddScoped<IMasterDataCacheOperations, MasterDataCacheOperations>();
            services.AddScoped<IServiceRequestOperations, ServiceRequestOperations>();
            //Add RazorPages , MVC
            services.AddRazorPages();
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddScoped<UserActivityFilter>();
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<UserActivityFilter>();
            });
            //Add MasterDataOperations
            services.AddScoped<IMasterDataOperations, MasterDataOperations>();
            services.AddAutoMapper(typeof(ApplicationDbContext));
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<UserActivityFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
            return services;
        }
        

    }
}