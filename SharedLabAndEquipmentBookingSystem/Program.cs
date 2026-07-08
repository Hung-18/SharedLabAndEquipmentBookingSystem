using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Console.WriteLine("Dang ket noi database: " + db.Database.GetConnectionString());

    Console.WriteLine("Dang chay migration...");
    await db.Database.MigrateAsync();

    Console.WriteLine("Dang tao trigger...");
    await db.EnsureDatabaseGuardsCreatedAsync();

    Console.WriteLine("Da tao trigger xong.");
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

