using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using CadastroContatos.Data;
using CadastroContatos.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;

var builder = WebApplication.CreateBuilder(args);

#region Configure Services

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("CadContatos")));

builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirContato",
        policy => policy.RequireClaim("ExcluirContato"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cadastro de Contatos",
        Description = "Developed by Cecília Nigro",
        Contact = new OpenApiContact { Name = "Cecília Nigro", Email = "ceci.nigro@outlook.com" }

    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "token JWT",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

#endregion

#region Configure Pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();

app.UseHttpsRedirection();

MapActions(app);

app.Run();

#endregion

#region Actions

void MapActions(WebApplication app)
{

    app.MapPost("/registro", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser registerUser) =>
    {
        if (registerUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(user.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("RegistroUsuario")
      .WithTags("Usuario");

    app.MapPost("/login", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        LoginUser loginUser) =>
    {
        if (loginUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(loginUser, out var errors))
            return Results.ValidationProblem(errors);

        var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

        if (result.IsLockedOut)
            return Results.BadRequest("Usuário bloqueado");

        if (!result.Succeeded)
            return Results.BadRequest("Usuário ou senha inválidos");

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(loginUser.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("LoginUsuario")
      .WithTags("Usuario");

    app.MapGet("/contato", [AllowAnonymous] async (
        MinimalContextDb context) =>
        await context.Contatos.ToListAsync())
        .WithName("GetContato")
        .WithTags("Contato");

    app.MapGet("/contato/{id}", [Authorize] async (
        Guid id,
        MinimalContextDb context) =>
        await context.Contatos.FindAsync(id)
              is Contato contato
                  ? Results.Ok(contato)
                  : Results.NotFound())
        .Produces<Contato>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetcontatoPorId")
        .WithTags("contato");

    app.MapPost("/contato", [Authorize] async (
        MinimalContextDb context,
        Contato contato) =>
    {
        if (!MiniValidator.TryValidate(contato, out var errors))
            return Results.ValidationProblem(errors);

        context.Contatos.Add(contato);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.CreatedAtRoute("GetcontatoPorId", new { id = contato.Id }, contato)
            : Results.BadRequest("Houve um problema ao salvar o registro");

    }).ProducesValidationProblem()
    .Produces<Contato>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PostContato")
    .WithTags("Contato");



    app.MapPut("/contato/{id}", [Authorize] async (
        Guid id,
        MinimalContextDb context,
        Contato contato) =>
    {
        var contatoBanco = await context.Contatos.FindAsync(id);
        if (contatoBanco == null) return Results.NotFound();

        if (!MiniValidator.TryValidate(contato, out var errors))
            return Results.ValidationProblem(errors);

        context.Contatos.Update(contato);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("Houve um problema ao salvar o registro");

    }).ProducesValidationProblem()
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PutContato")
    .WithTags("Contato");

    app.MapDelete("/Contato/{id}", [Authorize] async (
        Guid id,
        MinimalContextDb context) =>
    {
        var contato = await context.Contatos.FindAsync(id);
        if (contato == null) return Results.NotFound();

        context.Contatos.Remove(contato);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("Houve um problema ao salvar o registro");

    }).Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization("ExcluirContato")
    .WithName("DeleteContato")
    .WithTags("Contato");
}
#endregion.,~,ç- gyh