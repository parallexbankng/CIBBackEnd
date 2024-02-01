using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CIB.Core.Configuration
{
  public static class ServiceRegistration
  {
    public static IServiceCollection AddAdminServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
      var ConnectionString = Encryption.DecryptStrings(configuration.GetConnectionString("ParallexCIBCon"));
      services.AddDbContext<ParallexCIBContext>(options => options.UseSqlServer(ConnectionString));
      services.AddTransient<IUnitOfWork, UnitOfWork>();
      services.AddAutoMapper(Assembly.GetExecutingAssembly());
      return services;
    }
    public static IServiceCollection GatWayServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ParallexCIBContext>(options => options.UseSqlServer(configuration.GetConnectionString("ParallexCIBCon")));
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        return services;
    }
  }
  public class FileUploadFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var formParameters = context.ApiDescription.ParameterDescriptions.Where(paramDesc => paramDesc.IsFromForm());
    if(formParameters.Any())
    {
      // already taken care by swashbuckle. no need to add explicitly.
      return;
    }
    if(operation.RequestBody != null)
    {
      // NOT required for form type
      return;
    }
    if (context.ApiDescription.HttpMethod == HttpMethod.Post.Method)
    {
      var uploadFileMediaType = new OpenApiMediaType()
      {
        Schema = new OpenApiSchema()
        {
          Type = "object",
          Properties =
          {
            ["files"] = new OpenApiSchema()
            {
              Type = "array",
              Items = new OpenApiSchema()
              {
                Type = "string",
                Format = "binary"
              }
            }
          },
          Required = new HashSet<string>() { "files" }
        }
      };

      operation.RequestBody = new OpenApiRequestBody
      {
        Content = { ["multipart/form-data"] = uploadFileMediaType }
      };
    }
  }
}
  public static class Helper
  {
    internal static bool IsFromForm(this ApiParameterDescription apiParameter)
    {
      var source = apiParameter.Source;
      var elementType = apiParameter.ModelMetadata?.ElementType;
      return (source == BindingSource.Form || source == BindingSource.FormFile) || (elementType != null && typeof(IFormFile).IsAssignableFrom(elementType));
    }
  }
}
