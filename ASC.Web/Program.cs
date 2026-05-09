using ASC.Solution.Services;
using ASC.Web.Configuration;
using ASC.Web.Data;
using ASC.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ASC.Model.Models;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCongfig(builder.Configuration);
builder.Services.AddMyDependencyGroup();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var storageSeed = scope.ServiceProvider.GetRequiredService<IIdentitySeed>();

    await storageSeed.Seed(
        scope.ServiceProvider.GetService<UserManager<IdentityUser>>(),
        scope.ServiceProvider.GetService<RoleManager<IdentityRole>>(),
        scope.ServiceProvider.GetService<IOptions<ApplicationSettings>>());
}

using (var scope = app.Services.CreateScope())
{
    var navigationCacheOperations =
        scope.ServiceProvider.GetRequiredService<INavigationCacheOperations>();

    await navigationCacheOperations.CreateNavigationCacheAsync();
}
//CreateMasterDataCache
using (var scope = app.Services.CreateScope())
{
    var masterDataCacheOperations = scope.ServiceProvider.GetRequiredService<IMasterDataCacheOperations>();
    await masterDataCacheOperations.CreateMasterDataCacheAsync();
}
app.Run();
