using APIAggreration.Extensions;
using APIAggreration.Interfaces;
using APIAggreration.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsAReallyLongSecureJwtKey1234567890";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://mydomain.com";


// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//extra
// Register memory cache
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IDataProviderService, DataProviderService>();
// Add authentication services
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

//Policy evaluation (decides if a user is allowed to access something).
//The[Authorize] attribute support.
//Without AddAuthorization(), the app won’t know how to handle [Authorize].
builder.Services.AddAuthorization();
builder.Services.AddExternalApiClients(builder.Configuration);

//build the app
var app = builder.Build();

// Configure the middleware HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Request comes in (example: GET /api/aggregated-data)
//app.UseAuthentication() figures out who you are (checks token).
//app.UseAuthorization() checks what you’re allowed to do.
//If you have [Authorize] on the endpoint:
//The middleware checks user claims/roles/policies.
app.UseAuthorization();

//Routing controller based apis
app.MapControllers();

app.Run();

