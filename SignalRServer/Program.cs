using SignalRServer.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using System.Text.Json.Serialization;
using MessagePack;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using SignalRServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddCors(options => 
    {
        options.AddPolicy("AllowAnyGet",
                           builder => builder.AllowAnyOrigin()
                            .WithMethods("GET")
                            .AllowAnyHeader());
        options.AddPolicy("AllowExampleDomain",
                           builder => builder.WithOrigins("https://example.com")
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials());
    });
builder.Services.AddSignalR(hubOptions => 
    {
        hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(30);
        hubOptions.MaximumReceiveMessageSize = 65_536;//Bytes - biigest message a client can send
        hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(20);// wait before consider lost connection
        hubOptions.MaximumParallelInvocationsPerClient = 2;// by default each client can invoke 1 method of the hub and the next on is queued (here is 2 methods)
        hubOptions.EnableDetailedErrors = true;// if an unhandled exception is thrown client will see it, by default this setting is disabled. only for DEVELOPEMENT ENVIRONMENT must be true
        hubOptions.StreamBufferCapacity = 15;//maximum number of items that can be uploaded into a client-server stream

        if(hubOptions?.SupportedProtocols is not null)
        {
            foreach (var protocol in hubOptions.SupportedProtocols)
            {
                Console.WriteLine($"SignalR supports {protocol} protocol");
            }
        }
    })
    .AddMessagePackProtocol(options => 
    {
        options.SerializerOptions = MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData)
            .WithCompression(MessagePackCompression.Lz4Block)
            .WithAllowAssemblyVersionMismatch(true)
            .WithOldSpec()
            .WithOmitAssemblyVersion(true);
    })
    .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            options.PayloadSerializerOptions.Encoder = null;
            options.PayloadSerializerOptions.IncludeFields = false;
            options.PayloadSerializerOptions.IgnoreReadOnlyFields = false;
            options.PayloadSerializerOptions.IgnoreReadOnlyProperties = false;
            options.PayloadSerializerOptions.MaxDepth = 0;
            options.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
            options.PayloadSerializerOptions.DictionaryKeyPolicy = null;
            options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.PayloadSerializerOptions.PropertyNameCaseInsensitive = false;
            options.PayloadSerializerOptions.DefaultBufferSize = 32_768;
            options.PayloadSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
            options.PayloadSerializerOptions.ReferenceHandler = null;
            options.PayloadSerializerOptions.UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement;
            options.PayloadSerializerOptions.WriteIndented = true;
            Console.WriteLine($"Number of default JSON converters: {options.PayloadSerializerOptions.Converters.Count}");
        });

builder.Services.AddAuthentication(options => 
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddOpenIdConnect("oidc", options => 
    {
        options.Authority = "https://localhost:5001" ;
        options.ClientId = "webAppClient" ;
        options.ClientSecret = "webAppClientSecret" ;
        options.ResponseType = "code" ;
        options.CallbackPath = "/signin-oidc" ;
        options.SaveTokens = true ;
        options.RequireHttpsMetadata = false;
    })
    .AddJwtBearer(options => 
    {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        options.Authority = "https://localhost:5001";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ValidateIssuerSigningKey = true,
            // IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8
            //     .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value)),
            ValidateIssuer = false,
            ValidateAudience = false  
        };
        options.RequireHttpsMetadata = false;
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if(path.StartsWithSegments("/learninghub"))
                {
                    // attempt to get a token from a query string used by websocket
                    var accessToken = context.Request.Query["access_token"];

                    //if not present, extract the token from Authorization header
                    if(string.IsNullOrWhiteSpace(accessToken))
                    {
                        accessToken = context.Request.Headers["Authorization"]
                            .ToString()
                            .Replace("Bearer", "");//extracting the actual token
                    }
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };

    });
builder.Services.AddAuthorization(options =>
{ 
    options.AddPolicy( "BasicAuth" , policy =>
    { 
        policy.RequireAuthenticatedUser();
    });
    options.AddPolicy( "AdminClaim" , policy =>
    { 
        policy.RequireClaim( "admin" );
    });
    options.AddPolicy( "AdminOnly" , policy =>
    { 
        policy.Requirements.Add( new RoleRequirement( "admin" ));
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAnyGet")
    .UseCors("AllowExampleDomain");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<LearningHub>("/learningHub", options =>
    {
        options.Transports = 
                        HttpTransportType.WebSockets |
                        HttpTransportType.LongPolling;
        options.CloseOnAuthenticationExpiration = true;
        options.ApplicationMaxBufferSize = 65_536;
        options.TransportMaxBufferSize = 65_536;
        options.MinimumProtocolVersion = 0;
        options.TransportSendTimeout = TimeSpan.FromSeconds(10);
        options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(3);
        options.LongPolling.PollTimeout = TimeSpan.FromSeconds(10);
        Console.WriteLine($"Authorization data items: {options.AuthorizationData.Count}");
    });
app.UseBlazorFrameworkFiles();

app.Run();
