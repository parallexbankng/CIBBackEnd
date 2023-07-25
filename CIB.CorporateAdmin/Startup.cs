using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using CIB.Core.Configuration;
using CIB.Core.Modules.CorporateProfile.Validation;
using CIB.Core.Modules.Workflow.Validation;
using CIB.Core.Services._2FA;
using CIB.Core.Services.Api;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Services.Notification;
using CIB.Core.Services.OnlendingApi;
using CIB.Core.Services.OnlendingApi.Dto;
using CIB.Core.Utils;
using CIB.CorporateAdmin.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace CIB.CorporateAdmin
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }
    public IConfiguration Configuration { get; }
    readonly string Cors = "Cors";
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddControllers();
      services.AddCors(c => c.AddPolicy(Cors, cors => cors.WithOrigins("*").AllowAnyHeader().AllowAnyMethod()));
      //services.AddCors(c => c.AddPolicy(Cors, cors => cors.WithOrigins("*").AllowAnyHeader().AllowAnyMethod()));
      services.AddAdminServiceRegistration(Configuration);
      var tokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Encryption.DecryptStrings(Configuration["Jwt:Issuer"]),
        ValidAudience = Encryption.DecryptStrings(Configuration["Jwt:Issuer"]),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Encryption.DecryptStrings(Configuration["Jwt:Key"])))
      };
      services.AddSingleton(tokenValidationParameters);
      services.AddAuthentication(x =>
      {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      }).AddJwtBearer(x =>
      {
        x.SaveToken = true;
        x.TokenValidationParameters = tokenValidationParameters;
      });
      services.AddAuthorization();
      services.AddHttpContextAccessor();
      services.AddSwaggerGen(options =>
      {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Corporate Admin Portal", Version = "v1" });
        // add JWT Authentication
        var securityScheme = new OpenApiSecurityScheme
        {
          Name = "Authorization",
          Description = "Enter JWT Bearer token **_only_**",
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.ApiKey,
          Scheme = "bearer",
          Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
        };
        options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
        options.OperationFilter<FileUploadFilter>();
      });

      services.AddHttpClient<IEmailService, EmailService>();
      services.AddHttpClient("finnacleClient", c =>
      {
        c.Timeout = TimeSpan.FromMinutes(5);
        c.BaseAddress = new Uri(Configuration.GetValue<string>("TestApiUrl:baseUrl"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        c.DefaultRequestHeaders.Add("client-id", Encryption.DecryptStrings($"{Configuration["RequestKey:client-id"]}"));
        c.DefaultRequestHeaders.Add("client-key", Encryption.DecryptStrings($"{Configuration["RequestKey:client-key"]}"));
      });
      services.AddHttpClient("tokenClient", c =>
      {
        c.Timeout = TimeSpan.FromMinutes(5);
        c.BaseAddress = new Uri(Configuration.GetValue<string>("TestApiUrl:baseUrl"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      });
      services.AddHttpClient("testClient", c =>
      {
        c.Timeout = TimeSpan.FromMinutes(5);
        c.BaseAddress = new Uri(Configuration.GetValue<string>("TestClient:baseUrl"));
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      });

      services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

      services.AddTransient<IFileService, FileService>();
      services.AddTransient<IApiService, ApiService>();
      services.AddTransient<IOnlendingServiceApi, OnlendingServiceApi>();
      services.AddTransient<IToken2faService, Token2faService>();
      services.AddTransient<INotificationService, NotificationService>();
      services.AddTransient<IAuthenticationService, AuthenticationService>();
      services.AddAutoMapper(Assembly.GetExecutingAssembly());
      services.AddValidatorsFromAssemblyContaining<CreateCorporateProfileValidation>();
      services.AddValidatorsFromAssemblyContaining<UpdateCorporateProfileValidation>();
      services.AddValidatorsFromAssemblyContaining<CreateCorporateWorkFlowValidation>();
      services.AddValidatorsFromAssemblyContaining<UpdateWorkFlowValidation>();
    }
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment() || env.IsProduction())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CIB.CorporateAdmin v1"));
      }

      app.UseHttpsRedirection();
      app.UseRouting();
      app.UseCors(Cors);
      app.UseAuthentication();
      //app.UseAuthorization();
      app.UseApiKey();
      app.UseStaticFiles();
      app.UseStaticFiles(new StaticFileOptions()
      {
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot")),
        RequestPath = new PathString("/bulkupload")
      });
      app.Use(async (context, next) =>
      {
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self'; font-src 'self'; img-src 'self'; frame-src 'self'");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        await next();
      });
      app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
  }
}
