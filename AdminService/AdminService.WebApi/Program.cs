var builder = WebApplication.CreateBuilder(args);

// 1. Agregar soporte para Controladores (¡Muy importante!)
builder.Services.AddControllers();

// 2. Mantiene la configuración de OpenAPI que ya traía tu proyecto
builder.Services.AddOpenApi();

var app = builder.Build();

// 3. Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 4. Mapear las rutas a los controladores que crees
app.MapControllers();

app.Run();