using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using JW_Utils.JW_DataObjects;
using JW_Utils.JW_HostedServices;
using JW_Utils.JW_HostedServices.AccountManagement;
using Microsoft.AspNetCore.Authentication;

namespace FTP_UpLifter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices((hostContext, services) =>
                    {
                        var configuration = hostContext.Configuration;
                        var requiredGroups = configuration.GetSection("Authorization:RequiredGroups").Get<string[]>();

                        // Register IDataObjects as a singleton service
                        services.AddSingleton<DataObjects>(provider =>
                        {
                            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            var dataDirectory = Path.Combine(baseDirectory, "Data");
                            if (!Directory.Exists(dataDirectory))
                            {
                                Directory.CreateDirectory(dataDirectory);
                            }
                            var dataObject = new DataObjects(Path.Combine(dataDirectory, "Settings.db"));
                            return dataObject;
                        });

                        // Register hosted service for file watching
                        services.AddSingleton<IFileWatcherService, FileWatcherService>();
                        services.AddHostedService<FileWatcherService>(); services.AddSingleton<IFileWatcherService, FileWatcherService>();
 

                        // Add authentication and Razor Pages services for the web app
                        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                                .AddNegotiate();

                        var authorizationRequired = configuration.GetValue<bool>("Authorization:Required");
                        if (authorizationRequired)
                        {
                            services.AddAuthorization(options =>
                            {
                                options.FallbackPolicy = options.DefaultPolicy;
                                options.AddPolicy("AuthenticatedUser", policy =>
                                {
                                    policy.RequireAuthenticatedUser();
                                    policy.RequireAssertion(context =>
                                    {
                                        var userGroups = context.User.Claims
                                            .Where(c => c.Type == "groups")
                                            .Select(c => c.Value);

                                        // Log the claims for debugging
                                        var logger = context.Resource as ILogger<Program>;
                                        if (logger != null)
                                        {
                                            foreach (var claim in context.User.Claims)
                                            {
                                                logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
                                            }
                                        }

                                        return requiredGroups != null && requiredGroups.Any(group => userGroups.Contains(group));
                                    });
                                });
                            });

                            services.AddRazorPages(options =>
                            {
                                options.Conventions.AuthorizeFolder("/", "AuthenticatedUser");
                            });

                            // Register custom claims transformation
                            services.AddTransient<IClaimsTransformation, CustomClaimsTransformer>();
                        }
                        else
                        {
                            services.AddRazorPages();
                        }
                    });

                    webBuilder.Configure((context, app) =>
                    {
                        var env = context.HostingEnvironment;
                        if (env.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                        }
                        else
                        {
                            app.UseExceptionHandler("/Error");
                            app.UseHsts();
                        }

                        app.UseHttpsRedirection();
                        app.UseStaticFiles();

                        app.UseRouting();

                        var authorizationRequired = context.Configuration.GetValue<bool>("Authorization:Required");
                        if (authorizationRequired)
                        {
                            app.UseAuthentication();
                            app.UseAuthorization();
                        }

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapRazorPages();
                        });
                    });
                })
                .UseWindowsService(); // This line ensures the app can run as a Windows Service
    }
}